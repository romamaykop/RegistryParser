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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryParser.Parsers
{
    /// <summary>
    /// Парсер CSV/TXT файлов
    /// Читает файл построчно (потоково, без загрузки всего файла в память)
    /// Применяет маппинг из конфигурации (ColumnIndex → PropertyName)
    /// Создаёт объекты DTO и добавляет в ParseResult
    /// Поддерживает различные разделители и кодировки
    /// </summary>
    public class CsvParser : IRegistryParser
    {
        private readonly ParserConfiguration _configuration; // Конфигурация парсера
        private readonly IFileLoader _fileLoader; // Загрузчик файлов
        private readonly char _delimiter; // Разделитель полей (по умолчанию ';')
        private readonly CultureInfo _culture; // Культура для парсинга чисел и дат (ru-RU)

        public ParserType ParserType => ParserType.Csv; // Тип парсера

        public CsvParser() : this(new ParserConfiguration(), new FileLoader()) // Конструктор с конфигурацией по умолчанию
        {
        }

        /// <summary>
        /// Конструктор с кастомной конфигурацией
        /// </summary>
        /// <param name="configuration">Правила маппинга и настройки парсера</param>
        /// <param name="fileLoader">Загрузчик файлов (опционально)</param>
        public CsvParser(ParserConfiguration configuration, IFileLoader fileLoader = null)
        {
            _configuration = configuration ?? new ParserConfiguration();
            _fileLoader = fileLoader ?? new FileLoader(_configuration.Encoding, _configuration.BufferSize);
            _delimiter = _configuration.Delimiter;
            _culture = new CultureInfo("ru-RU");
        }

        /// <summary>
        /// Парсинг одного файла
        /// Использует потоковое чтение для минимизации использования памяти
        /// </summary>
        public async Task<ParseResult> ParseAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var result = new ParseResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Проверка существования файла
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

                // Потоковое чтение файла
                using (var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    _configuration.BufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan))
                using (var reader = new StreamReader(stream, _configuration.Encoding))
                {
                    int lineNumber = 0;
                    string[] headers = null;

                    // Построчное чтение
                    while (!reader.EndOfStream)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var line = await reader.ReadLineAsync().ConfigureAwait(false);
                        lineNumber++;

                        // Пропускаем пустые строки (если настроено)
                        if (_configuration.SkipEmptyLines && string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        // Проверка лимита строк (если настроено)
                        if (_configuration.MaxLines > 0 && lineNumber > _configuration.MaxLines)
                        {
                            break;
                        }

                        // Обработка заголовка (если есть)
                        if (_configuration.HasHeader && headers == null)
                        {
                            headers = SplitLine(line);
                            MapHeadersToConfiguration(headers);
                            continue;
                        }

                        result.TotalLinesProcessed++;

                        try
                        {
                            // Сначала разбиваем строку на поля
                            var fields = SplitLine(line);

                            // Передаём fields в ParseLineToPayer
                            var payer = ParseLineToPayer(fields, lineNumber, headers);
                            if (payer != null)
                            {
                                result.Payers.Add(payer);
                                result.SuccessLinesCount++;

                                // Парсим и добавляем счётчики
                                if (_configuration.TagMappings != null &&
                                    _configuration.TagMappings.Any(m => m.PropertyName?.StartsWith("IRC_") == true))
                                {
                                    var counter = ParseCounterFromLine(fields, lineNumber);
                                    if (counter != null)
                                    {
                                        result.Counters.Add(counter);
                                    }
                                }

                                // Парсим и добавляем услуги
                                if (_configuration.TagMappings != null &&
                                    _configuration.TagMappings.Any(m => m.PropertyName?.StartsWith("IRS_") == true))
                                {
                                    var service = ParseServiceFromLine(fields, lineNumber);
                                    if (service != null)
                                    {
                                        result.Services.Add(service);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Обработка ошибки строки (логирование и продолжение)
                            HandleLineError(result, ex, line, lineNumber, filePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Критическая ошибка файла (прерываем обработку)
                result.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.FileError,
                    FileName = filePath,
                    Message = $"Критическая ошибка при чтении файла: {ex.Message}",
                    Exception = ex
                });
            }

            stopwatch.Stop();
            result.ProcessingTime = stopwatch;

            return result;
        }

        /// <summary>
        /// Парсинг из строкового содержимого
        /// Используется для тестов или когда данные уже в памяти
        /// </summary>
        public async Task<ParseResult> ParseContentAsync(string content, CancellationToken cancellationToken = default)
        {
            var result = new ParseResult();
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            int lineNumber = 0;
            string[] headers = null;

            foreach (var line in lines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lineNumber++;

                if (_configuration.SkipEmptyLines && string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (_configuration.HasHeader && headers == null)
                {
                    headers = SplitLine(line);
                    MapHeadersToConfiguration(headers);
                    continue;
                }

                result.TotalLinesProcessed++;

                try
                {
                    // Сначала разбиваем строку на поля
                    var fields = SplitLine(line);

                    var payer = ParseLineToPayer(fields, lineNumber, headers);
                    if (payer != null)
                    {
                        result.Payers.Add(payer);
                        result.SuccessLinesCount++;

                        // Парсим и добавляем счётчики
                        if (_configuration.TagMappings != null &&
                            _configuration.TagMappings.Any(m => m.PropertyName?.StartsWith("IRC_") == true))
                        {
                            var counter = ParseCounterFromLine(fields, lineNumber);
                            if (counter != null)
                            {
                                result.Counters.Add(counter);
                            }
                        }

                        // Парсим и добавляем услуги
                        if (_configuration.TagMappings != null &&
                            _configuration.TagMappings.Any(m => m.PropertyName?.StartsWith("IRS_") == true))
                        {
                            var service = ParseServiceFromLine(fields, lineNumber);
                            if (service != null)
                            {
                                result.Services.Add(service);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    HandleLineError(result, ex, line, lineNumber, null);
                }
            }

            return await Task.FromResult(result).ConfigureAwait(false);
        }

        /// <summary>
        /// Разделение строки на поля с учетом ковычек
        /// 
        /// Разделяет по разделителю (например, ';')
        /// внутри кавычек разделитель игнорируется
        /// </summary>
        private string[] SplitLine(string line)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (ch == _delimiter && !inQuotes)
                {
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(ch);
                }
            }

            fields.Add(currentField.ToString().Trim());
            return fields.ToArray();
        }

        /// <summary>
        /// Парсинг нескольких файлов
        /// 
        /// Используется для сложных форматов, где данные распределены по файлам
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
        /// Маппинг заголовков на конфигурацию
        /// 
        /// Если в файле есть заголовки,этот метод находит соответствия
        /// между именами колонок и TagMappings.ColumnName
        /// </summary>
        private void MapHeadersToConfiguration(string[] headers)
        {
            if (_configuration.TagMappings == null || _configuration.TagMappings.Count == 0)
            {
                return;
            }

            for (int i = 0; i < headers.Length; i++)
            {
                var mapping = _configuration.TagMappings
                    .FirstOrDefault(m => m.ColumnName?.Equals(headers[i], StringComparison.OrdinalIgnoreCase) == true);

                if (mapping != null)
                {
                    mapping.ColumnIndex = i;
                }
            }
        }

        /// <summary>
        /// Это основной метод парсинга одной строки в объект INREG_PAYER
        /// 
        /// Счётчики и услуги добавляются в результат через событие/коллбек
        /// Связь осуществляется по ID плательщика (IRC_PAYER_ID → PAYER.ID)
        /// </summary>
        private INREG_PAYER ParseLineToPayer(string[] fields, int lineNumber, string[] headers)
        {
            var payer = new INREG_PAYER();

            // Если есть конфигурация маппинга - используем её
            if (_configuration.TagMappings != null && _configuration.TagMappings.Count > 0)
            {
                foreach (var mapping in _configuration.TagMappings)
                {
                    if (mapping.Skip) continue;

                    var value = GetValueByMapping(fields, mapping, headers);
                    SetPropertyValue(payer, mapping.PropertyName, value, mapping);
                }
            }

            return payer;
        }

        /// <summary>
        /// Получение значения по маппингу
        /// </summary>
        private string GetValueByMapping(string[] fields, TagMapping mapping, string[] headers)
        {
            string value = null;

            // Получаем значение по индексу колонки
            if (mapping.ColumnIndex.HasValue && mapping.ColumnIndex.Value >= 0 && mapping.ColumnIndex.Value < fields.Length)
            {
                value = fields[mapping.ColumnIndex.Value];
            }
            // Или по имени колонки (если есть заголовки)
            else if (!string.IsNullOrWhiteSpace(mapping.ColumnName) && headers != null)
            {
                var index = Array.IndexOf(headers, mapping.ColumnName);
                if (index >= 0 && index < fields.Length)
                {
                    value = fields[index];
                }
            }

            // Применяем префикс/постфикс
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (!string.IsNullOrWhiteSpace(mapping.Prefix) && value.StartsWith(mapping.Prefix))
                {
                    value = value.Substring(mapping.Prefix.Length);
                }
                if (!string.IsNullOrWhiteSpace(mapping.Postfix) && value.EndsWith(mapping.Postfix))
                {
                    value = value.Substring(0, value.Length - mapping.Postfix.Length);
                }
            }

            return value;
        }

        /// <summary>
        /// Установка значения свойства через рефлексию
        /// </summary>
        private void SetPropertyValue(INREG_PAYER payer, string propertyName, string value, TagMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            var propertyInfo = typeof(INREG_PAYER).GetProperty(propertyName);
            if (propertyInfo == null) return;

            try
            {
                object convertedValue = null;

                // Пользовательская трансформация (если задана)
                if (mapping.TransformFunc != null && !string.IsNullOrWhiteSpace(value))
                {
                    convertedValue = mapping.TransformFunc(value);
                }
                else
                {
                    convertedValue = ConvertValue(value, propertyInfo.PropertyType, mapping);
                }

                // Устанавливаем значение всегда (даже null для nullable типов)
                propertyInfo.SetValue(payer, convertedValue);
            }
            catch (Exception ex)
            {
                throw new MappingException(
                    $"Ошибка маппинга свойства {propertyName}: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Конвертация значения в целевой тип
        /// </summary>
        private object ConvertValue(string value, Type targetType, TagMapping mapping)
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Строки обрабатываем отдельно - возвращаем даже пустые значения
            if (underlyingType == typeof(string))
            {
                return ApplyLengthLimit(value, mapping.Length);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return mapping.DefaultValue;
            }

            try
            {
                switch (underlyingType.Name)
                {
                    case "Int32":
                        if (int.TryParse(value, out int intValue))
                            return intValue;
                        break;

                    case "Int64":
                        if (long.TryParse(value, out long longValue))
                            return longValue;
                        break;

                    case "Decimal":
                        return ParseDecimal(value, mapping.DecimalSeparator);

                    case "DateTime":
                        if (!string.IsNullOrWhiteSpace(mapping.Format) &&
                            DateTime.TryParseExact(value, mapping.Format, _culture,
                                DateTimeStyles.None, out DateTime dtValue))
                            return dtValue;
                        if (DateTime.TryParse(value, _culture, DateTimeStyles.None, out dtValue))
                            return dtValue;
                        break;

                    case "Boolean":
                        if (bool.TryParse(value, out bool boolValue))
                            return boolValue;
                        break;
                }
            }
            catch
            {
                // Возвращаем default для nullable типов
            }

            return mapping.DefaultValue;
        }

        /// <summary>
        /// Парсинг десятичного числа с учётом разделителя из конфигурации (по умолчанию ",")
        /// </summary>
        private decimal ParseDecimal(string value, string decimalSeparator = null)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0m;

            //Правильный порядок проверки разделителя
            decimalSeparator = !string.IsNullOrWhiteSpace(decimalSeparator) ? decimalSeparator : (!string.IsNullOrWhiteSpace(_configuration.DecimalSeparator)
            ? _configuration.DecimalSeparator
            : ",");

            // Нормализуем разделитель для CultureInfo.InvariantCulture
            var normalizedValue = value.Replace(decimalSeparator,
                CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator);
            // Пробуем парсить с InvariantCulture
            if (decimal.TryParse(normalizedValue, NumberStyles.Number,
                CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
            // Пробуем с русской культурой
            if (decimal.TryParse(value, NumberStyles.Number, _culture, out result))
            {
                return result;
            }

            return 0m;
        }

        /// <summary>
        /// Обрезает строку до максимальной длины
        /// </summary>
        private string ApplyLengthLimit(string value, int? maxLength)
        {
            if (!maxLength.HasValue || string.IsNullOrWhiteSpace(value))
                return value;

            return value.Length > maxLength.Value
                ? value.Substring(0, maxLength.Value)
                : value;
        }

        /// <summary>
        /// Обработка ошибки строки
        /// 
        /// Если настроено в конфигурации SkipErrorLines, то продолжает обработку
        /// </summary>
        private void HandleLineError(ParseResult result, Exception ex, string line, int lineNumber, string fileName)
        {
            result.SkippedLinesCount++;

            if (_configuration.SkipErrorLines)
            {
                result.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.LineError,
                    LineNumber = lineNumber,
                    FileName = fileName,
                    Message = ex.Message,
                    Exception = ex,
                    LineContent = line?.Length > 100 ? line.Substring(0, 100) + "..." : line
                });
            }
            else
            {
                throw ex;
            }
        }

        /// <summary>
        /// Объединение результатов при парсинге нескольких файлов
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
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

        /// <summary>
        /// Парсинг счётчика из строки
        /// 
        /// - В TagMappings должны быть поля с префиксом "IRC_", чтобы парсить счётчик
        /// </summary>
        private INREG_COUNTER ParseCounterFromLine(string[] fields, int lineNumber)
        {
            // Проверяем есть ли маппинги для полей счётчика (префикс "IRC_")
            var counterMappings = _configuration.TagMappings
                .Where(m => m.PropertyName?.StartsWith("IRC_") == true)
                .ToList();

            if (counterMappings.Count == 0)
                return null;

            var counter = new INREG_COUNTER();

            foreach (var mapping in counterMappings)
            {
                var value = GetValueByMapping(fields, mapping, null);
                SetCounterPropertyValue(counter, mapping.PropertyName, value, mapping);
            }

            return counter;
        }

        /// <summary>
        /// Установка значения свойства INREG_COUNTER
        /// </summary>
        private void SetCounterPropertyValue(INREG_COUNTER counter, string propertyName, string value, TagMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            var propertyInfo = typeof(INREG_COUNTER).GetProperty(propertyName);
            if (propertyInfo == null) return;

            try
            {
                var convertedValue = ConvertValue(value, propertyInfo.PropertyType, mapping);
                propertyInfo.SetValue(counter, convertedValue);
            }
            catch { }
        }

        /// <summary>
        /// Парсинг услуги из строки
        /// 
        /// - В TagMappings должны быть поля с префиксом "IRS_", чтобы парсить услуги
        /// </summary>
        private INREG_SERVICE ParseServiceFromLine(string[] fields, int lineNumber)
        {
            // Проверяем есть ли маппинги для полей услуги (префикс "IRS_")
            var serviceMappings = _configuration.TagMappings
                .Where(m => m.PropertyName?.StartsWith("IRS_") == true)
                .ToList();

            if (serviceMappings.Count == 0)
                return null;

            var service = new INREG_SERVICE();

            foreach (var mapping in serviceMappings)
            {
                var value = GetValueByMapping(fields, mapping, null);
                SetServicePropertyValue(service, mapping.PropertyName, value, mapping);
            }

            return service;
        }

        /// <summary>
        /// Установка значения свойства INREG_SERVICE
        /// </summary>
        private void SetServicePropertyValue(INREG_SERVICE service, string propertyName, string value, TagMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            var propertyInfo = typeof(INREG_SERVICE).GetProperty(propertyName);
            if (propertyInfo == null) return;

            try
            {
                var convertedValue = ConvertValue(value, propertyInfo.PropertyType, mapping);
                propertyInfo.SetValue(service, convertedValue);
            }
            catch { }
        }
    }
}
