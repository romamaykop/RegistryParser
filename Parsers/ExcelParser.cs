using RegistryParser.Configuration;
using RegistryParser.Extensions.Enums;
using RegistryParser.Interfaces;
using RegistryParser.Loaders;
using RegistryParser.Models;
using RegistryParser.Models.DTOs;
using RegistryParser.Models.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryParser.Parsers
{
    /// <summary>
    /// Парсер Excel файлов (XLS/XLSX)
    /// Использует библиотеку MiniExcel для эффективного чтения
    /// </summary>
    public class ExcelParser : IRegistryParser
    {
        private readonly ParserConfiguration _configuration;
        private readonly IFileLoader _fileLoader;

        /// <summary>
        /// Тип парсера
        /// </summary>
        public ParserType ParserType => ParserType.Excel;

        /// <summary>
        /// Конструктор
        /// </summary>
        public ExcelParser(ParserConfiguration configuration = null, IFileLoader fileLoader = null)
        {
            _configuration = configuration ?? new ParserConfiguration();
            _fileLoader = fileLoader ?? new FileLoader();
        }

        /// <summary>
        /// Парсинг Excel файла
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

                // Валидация файла перед загрузкой
                await ValidateExcelFileAsync(filePath, result)
                    .ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    return result;
                }

                // Читаем файл как байты
                var bytes = await _fileLoader.LoadAsBytesAsync(filePath, cancellationToken)
                    .ConfigureAwait(false);

                using (var stream = new MemoryStream(bytes))
                {
                    // Используем MiniExcel для чтения

                    //var rows = MiniExcelLibs.MiniExcel.Query(stream);

                    //int lineNumber = 0;
                    //foreach (var row in rows)
                    //{
                    //    cancellationToken.ThrowIfCancellationRequested();
                    //    lineNumber++;

                    //    if (_configuration.HasHeader && lineNumber == 1)
                    //    {
                    //        continue; // Пропускаем заголовок
                    //    }

                    //    var payer = MapExcelRowToPayer(row);
                    //    result.Payers.Add(payer);
                    //    result.SuccessLinesCount++;
                    //}
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.FileError,
                    FileName = filePath,
                    Message = $"Ошибка при парсинге Excel: {ex.Message}",
                    Exception = ex
                });
            }

            stopwatch.Stop();
            result.ProcessingTime = stopwatch;

            return result;
        }

        /// <summary>
        /// Парсинг нескольких файлов
        /// </summary>
        public async Task<ParseResult> ParseMultipleAsync(string[] filePaths, CancellationToken cancellationToken = default)
        {
            var combinedResult = new ParseResult();

            foreach (var filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileResult = await ParseAsync(filePath, cancellationToken)
                    .ConfigureAwait(false);
                MergeResults(combinedResult, fileResult);
            }

            return combinedResult;
        }

        /// <summary>
        /// Парсинг из содержимого
        /// </summary>
        public async Task<ParseResult> ParseContentAsync(string content, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Excel формат не поддерживает парсинг из строкового содержимого");
        }

        /// <summary>
        /// Валидация Excel файла
        /// </summary>
        private async Task ValidateExcelFileAsync(string filePath, ParseResult result)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();

                if (extension != ".xls" && extension != ".xlsx")
                {
                    result.Errors.Add(new ParseError
                    {
                        ErrorType = ErrorType.ValidationError,
                        FileName = filePath,
                        Message = $"Неподдерживаемое расширение файла: {extension}"
                    });
                    return;
                }

                var fileInfo = new FileInfo(filePath);

                // Проверка размера файла (максимум 600 МБ)
                const long MaxFileSize = 600 * 1024 * 1024;
                if (fileInfo.Length > MaxFileSize)
                {
                    result.Errors.Add(new ParseError
                    {
                        ErrorType = ErrorType.ValidationError,
                        FileName = filePath,
                        Message = $"Файл превышает максимальный размер 600 МБ (текущий: {fileInfo.Length / 1024 / 1024} МБ)"
                    });
                    return;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.ValidationError,
                    FileName = filePath,
                    Message = $"Ошибка валидации файла: {ex.Message}",
                    Exception = ex
                });
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Маппинг строки Excel в объект Payer
        /// </summary>
        private INREG_PAYER MapExcelRowToPayer(IDictionary<string, object> row)
        {
            var payer = new INREG_PAYER();

            if (_configuration.TagMappings != null)
            {
                foreach (var mapping in _configuration.TagMappings)
                {
                    if (mapping.Skip) continue;

                    if (row.TryGetValue(mapping.ColumnName, out var value))
                    {
                        SetPropertyValue(payer, mapping.PropertyName, value?.ToString(), mapping);
                    }
                }
            }

            return payer;
        }

        /// <summary>
        /// Установка значения свойства
        /// </summary>
        private void SetPropertyValue(INREG_PAYER payer, string propertyName, string value, TagMapping mapping)
        {
            var propertyInfo = typeof(INREG_PAYER).GetProperty(propertyName);
            if (propertyInfo == null) return;

            try
            {
                var convertedValue = ConvertValue(value, propertyInfo.PropertyType, mapping);
                propertyInfo.SetValue(payer, convertedValue);
            }
            catch
            {
                // Игнорируем ошибки маппинга для отдельных полей
            }
        }

        /// <summary>
        /// Конвертация значения
        /// </summary>
        private object ConvertValue(string value, Type targetType, TagMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(value))
                return mapping.DefaultValue;

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (underlyingType == typeof(string))
                    return value;

                if (underlyingType == typeof(int) || underlyingType == typeof(int?))
                    return int.TryParse(value, out var i) ? i : mapping.DefaultValue;

                if (underlyingType == typeof(decimal) || underlyingType == typeof(decimal?))
                    return decimal.TryParse(value, out var d) ? d : mapping.DefaultValue;

                if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTime?))
                    return DateTime.TryParse(value, out var dt) ? dt : mapping.DefaultValue;
            }
            catch
            {
                return mapping.DefaultValue;
            }

            return value;
        }

        /// <summary>
        /// Объединение результатов
        /// </summary>
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
