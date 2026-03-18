using RegistryParser.Configuration;
using RegistryParser.Extensions.Enums;
using RegistryParser.Interfaces;
using RegistryParser.Loaders;
using RegistryParser.Models;
using RegistryParser.Models.DTOs;
using RegistryParser.Models.Enums;
using RegistryParser.Parsers.Classes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryParser.Parsers
{
    /// <summary>
    /// Парсер для сложных иерархических форматов
    /// 
    /// Поддерживает:
    /// - Несколько разделителей (;, :, [!])
    /// - Повторяющиеся блоки (ПУ, услуги)
    /// - Иерархическую структуру
    /// </summary>
    public class ComplexFormatParser : IRegistryParser
    {
        private readonly ComplexFormatConfiguration _config;
        private readonly IFileLoader _fileLoader;
        private readonly CultureInfo _culture;

        public ParserType ParserType => ParserType.Csv;

        public ComplexFormatParser(ComplexFormatConfiguration config, IFileLoader fileLoader = null)
        {
            _config = config ?? new ComplexFormatConfiguration();
            _fileLoader = fileLoader ?? new FileLoader();
            _culture = new CultureInfo("ru-RU");
        }

        /// <summary>
        /// ПАРСИНГ ФАЙЛА СО СЛОЖНЫМ ФОРМАТОМ
        /// </summary>
        public async Task<ParseResult> ParseAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var result = new ParseResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (!await _fileLoader.FileExistsAsync(filePath))
                {
                    result.Errors.Add(new ParseError
                    {
                        ErrorType = ErrorType.FileError,
                        FileName = filePath,
                        Message = $"Файл не найден: {filePath}"
                    });
                    return result;
                }

                var content = await _fileLoader.LoadAsStringAsync(filePath, cancellationToken)
                    .ConfigureAwait(false);

                return await ParseContentAsync(content, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.FileError,
                    FileName = filePath,
                    Message = $"Критическая ошибка: {ex.Message}",
                    Exception = ex
                });
            }

            stopwatch.Stop();
            result.ProcessingTime = stopwatch;

            return result;
        }

        public async Task<ParseResult> ParseMultipleAsync(string[] filePaths, CancellationToken cancellationToken = default)
        {
            var combinedResult = new ParseResult();

            foreach (var filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileResult = await ParseAsync(filePath, cancellationToken).ConfigureAwait(false);
                MergeResults(combinedResult, fileResult);
            }

            return combinedResult;
        }

        /// <summary>
        /// ПАРСИНГ СТРОКОВОГО СОДЕРЖИМОГО
        /// </summary>
        public async Task<ParseResult> ParseContentAsync(string content, CancellationToken cancellationToken = default)
        {
            var result = new ParseResult();
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            int lineNumber = 0;

            foreach (var line in lines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var parseResult = ParseLine(line, lineNumber);

                    if (parseResult.Payer != null)
                    {
                        result.Payers.Add(parseResult.Payer);

                        // Добавляем счётчики
                        if (parseResult.Counters != null)
                        {
                            result.Counters.AddRange(parseResult.Counters);
                        }

                        // Добавляем услуги
                        if (parseResult.Services != null)
                        {
                            result.Services.AddRange(parseResult.Services);
                        }

                        result.SuccessLinesCount++;
                    }

                    result.TotalLinesProcessed++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ParseError
                    {
                        ErrorType = ErrorType.LineError,
                        LineNumber = lineNumber,
                        Message = ex.Message,
                        LineContent = line.Length > 100 ? line.Substring(0, 100) + "..." : line
                    });
                    result.SkippedLinesCount++;
                }
            }

            return await Task.FromResult(result).ConfigureAwait(false);
        }

        /// <summary>
        /// ПАРСИНГ ОДНОЙ СТРОКИ
        /// Возвращает плательщика + список счётчиков + список услуг
        /// </summary>
        private LineParseResult ParseLine(string line, int lineNumber)
        {
            var result = new LineParseResult();

            // ============================================
            // ШАГ 1: Разделяем на основную часть и блок услуг по маркеру [!]
            // ============================================
            var parts = line.Split(new[] { _config.ServicesBlockMarker }, StringSplitOptions.None);

            var mainPart = parts[0]; // Данные плательщика + счётчики
            var servicesPart = parts.Length > 1 ? parts[1] : null; // Данные услуг

            // ============================================
            // ШАГ 2: Парсим основную часть (плательщик + счётчики)
            // ============================================
            var mainFields = mainPart.Split(_config.MainDelimiter);

            // Парсим плательщика (первые N полей по конфигурации)
            result.Payer = ParsePayer(mainFields);

            // Парсим счётчики (остальные поля до маркера [!])
            result.Counters = ParseCounters(mainFields, result.Payer);

            // ============================================
            // ШАГ 3: Парсим услуги (после маркера [!])
            // ============================================
            if (!string.IsNullOrWhiteSpace(servicesPart))
            {
                result.Services = ParseServices(servicesPart, result.Payer);
            }

            return result;
        }

        /// <summary>
        /// ПАРСИНГ ПЛАТЕЛЬЩИКА
        /// </summary>
        private INREG_PAYER ParsePayer(string[] fields)
        {
            var payer = new INREG_PAYER();

            foreach (var mapping in _config.PayerMappings)
            {
                if (mapping.FieldIndex >= fields.Length)
                    continue;

                var value = fields[mapping.FieldIndex];
                SetPayerProperty(payer, mapping.PropertyName, value, mapping);
            }

            return payer;
        }

        /// <summary>
        /// ПАРСИНГ СЧЁТЧИКОВ (повторяющиеся блоки)
        /// </summary>
        private List<INREG_COUNTER> ParseCounters(string[] fields, INREG_PAYER payer)
        {
            var counters = new List<INREG_COUNTER>();

            // Находим где начинаются счётчики (после полей плательщика)
            int payerFieldCount = _config.PayerMappings.Max(m => m.FieldIndex) + 1;

            if (payerFieldCount >= fields.Length)
                return counters;

            // Оставшиеся поля - это счётчики
            var counterFields = fields.Skip(payerFieldCount).ToArray();

            // Разбиваем на блоки по CounterBlockSize
            int blockCount = counterFields.Length / _config.CounterBlockSize;

            for (int i = 0; i < blockCount; i++)
            {
                var counter = new INREG_COUNTER();
                counter.IRC_PAYER_ID = payer.ID; // СВЯЗЬ С ПЛАТЕЛЬЩИКОМ

                for (int j = 0; j < _config.CounterBlockSize; j++)
                {
                    if (i * _config.CounterBlockSize + j >= counterFields.Length)
                        break;

                    var mapping = _config.CounterMappings.FirstOrDefault(m => m.FieldIndex == j);
                    if (mapping == null || mapping.Ignore)
                        continue;

                    var value = counterFields[i * _config.CounterBlockSize + j];
                    SetCounterProperty(counter, mapping.PropertyName, value, mapping);
                }

                counters.Add(counter);
            }

            return counters;
        }

        /// <summary>
        /// ПАРСИНГ УСЛУГ (повторяющиеся блоки после [!])
        /// </summary>
        private List<INREG_SERVICE> ParseServices(string servicesPart, INREG_PAYER payer)
        {
            var services = new List<INREG_SERVICE>();

            // Разделяем по под-разделителю (:)
            var serviceFields = servicesPart.Split(_config.SubDelimiter);

            // Разбиваем на блоки по ServiceBlockSize
            int blockCount = serviceFields.Length / _config.ServiceBlockSize;

            for (int i = 0; i < blockCount; i++)
            {
                var service = new INREG_SERVICE();
                service.IRS_PAYER_ID = payer.ID; // СВЯЗЬ С ПЛАТЕЛЬЩИКОМ

                for (int j = 0; j < _config.ServiceBlockSize; j++)
                {
                    if (i * _config.ServiceBlockSize + j >= serviceFields.Length)
                        break;

                    var mapping = _config.ServiceMappings.FirstOrDefault(m => m.FieldIndex == j);
                    if (mapping == null || mapping.Ignore)
                        continue;

                    var value = serviceFields[i * _config.ServiceBlockSize + j];
                    SetServiceProperty(service, mapping.PropertyName, value, mapping);
                }

                services.Add(service);
            }

            return services;
        }

        /// <summary>
        /// Установка свойства INREG_PAYER
        /// </summary>
        private void SetPayerProperty(INREG_PAYER payer, string propertyName, string value, FieldMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            var propertyInfo = typeof(INREG_PAYER).GetProperty(propertyName);
            if (propertyInfo == null) return;

            var convertedValue = ConvertValue(value, propertyInfo.PropertyType, mapping);
            propertyInfo.SetValue(payer, convertedValue);
        }

        /// <summary>
        /// Установка свойства INREG_COUNTER
        /// </summary>
        private void SetCounterProperty(INREG_COUNTER counter, string propertyName, string value, FieldMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            var propertyInfo = typeof(INREG_COUNTER).GetProperty(propertyName);
            if (propertyInfo == null) return;

            var convertedValue = ConvertValue(value, propertyInfo.PropertyType, mapping);
            propertyInfo.SetValue(counter, convertedValue);
        }

        /// <summary>
        /// Установка свойства INREG_SERVICE
        /// </summary>
        private void SetServiceProperty(INREG_SERVICE service, string propertyName, string value, FieldMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            var propertyInfo = typeof(INREG_SERVICE).GetProperty(propertyName);
            if (propertyInfo == null) return;

            var convertedValue = ConvertValue(value, propertyInfo.PropertyType, mapping);
            propertyInfo.SetValue(service, convertedValue);
        }

        /// <summary>
        /// Конвертация значения
        /// </summary>
        private object ConvertValue(string value, Type targetType, FieldMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (underlyingType == typeof(string))
                    return value;

                if (underlyingType == typeof(int) || underlyingType == typeof(int?))
                    return int.TryParse(value, out var i) ? i : 0;

                if (underlyingType == typeof(long) || underlyingType == typeof(long?))
                    return long.TryParse(value, out var l) ? l : 0;

                if (underlyingType == typeof(decimal) || underlyingType == typeof(decimal?))
                {
                    var normalizedValue = value.Replace(
                        _config.DecimalSeparator,
                        CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator);
                    return decimal.TryParse(normalizedValue, out var d) ? d : 0m;
                }

                if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTime?))
                {
                    if (!string.IsNullOrWhiteSpace(mapping.DateFormat))
                    {
                        return DateTime.TryParseExact(value, mapping.DateFormat, _culture,
                            DateTimeStyles.None, out var dt) ? dt : (DateTime?)null;
                    }
                    return DateTime.TryParse(value, out tryPdt) ? tryPdt : (DateTime?)null;
                }
            }
            catch
            {
                return null;
            }

            return value;
        }

        private void MergeResults(ParseResult target, ParseResult source)
        {
            target.Payers.AddRange(source.Payers);
            target.Counters.AddRange(source.Counters);
            target.Services.AddRange(source.Services);
            target.Errors.AddRange(source.Errors);
            target.TotalLinesProcessed += source.TotalLinesProcessed;
            target.SuccessLinesCount += source.SuccessLinesCount;
            target.SkippedLinesCount += source.SkippedLinesCount;
        }
    }
}
