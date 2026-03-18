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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryParser.Parsers
{
    /// <summary>
    /// Универсальный парсер с использованием регулярных выражений
    /// Позволяет конфигурировать парсинг через внешние файлы
    /// Применим для CSV/TXT, DBF, XLS/XLSX форматов
    /// </summary>
    public class UniversalRegexParser : IRegistryParser
    {
        private readonly ParserConfiguration _configuration;
        private readonly IFileLoader _fileLoader;
        private readonly Dictionary<string, Regex> _compiledRegexes;

        /// <summary>
        /// Тип парсера
        /// </summary>
        public ParserType ParserType => ParserType.UniversalRegex;

        /// <summary>
        /// Конструктор
        /// </summary>
        public UniversalRegexParser(ParserConfiguration configuration = null, IFileLoader fileLoader = null)
        {
            _configuration = configuration ?? new ParserConfiguration();
            _fileLoader = fileLoader ?? new FileLoader();
            _compiledRegexes = new Dictionary<string, Regex>();

            // Компилируем регулярные выражения при инициализации
            CompileRegexPatterns();
        }

        /// <summary>
        /// Компиляция регулярных выражений для производительности
        /// </summary>
        private void CompileRegexPatterns()
        {
            if (_configuration.TagMappings == null) return;

            foreach (var mapping in _configuration.TagMappings)
            {
                if (!string.IsNullOrWhiteSpace(mapping.RegexPattern))
                {
                    try
                    {
                        _compiledRegexes[mapping.PropertyName] = new Regex(
                            mapping.RegexPattern,
                            RegexOptions.Compiled | RegexOptions.CultureInvariant);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new ConfigurationException(
                            $"Некорректный regex паттерн для {mapping.PropertyName}: {mapping.RegexPattern}",
                            ex);
                    }
                }
            }
        }

        /// <summary>
        /// Парсинг файла с использованием регулярных выражений
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

                return await ParseContentAsync(content, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.FileError,
                    FileName = filePath,
                    Message = $"Ошибка при парсинге: {ex.Message}",
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
        /// Парсинг строкового содержимого с использованием regex
        /// </summary>
        public async Task<ParseResult> ParseContentAsync(string content, CancellationToken cancellationToken = default)
        {
            var result = new ParseResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                int lineNumber = 0;

                foreach (var line in lines)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    lineNumber++;

                    if (_configuration.SkipEmptyLines && string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    result.TotalLinesProcessed++;

                    try
                    {
                        var payer = ParseLineWithRegex(line, lineNumber);
                        if (payer != null)
                        {
                            result.Payers.Add(payer);
                            result.SuccessLinesCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleLineError(result, ex, line, lineNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.FileError,
                    Message = $"Критическая ошибка парсинга: {ex.Message}",
                    Exception = ex
                });
            }

            stopwatch.Stop();
            result.ProcessingTime = stopwatch;

            return await Task.FromResult(result)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Парсинг строки с использованием регулярных выражений
        /// </summary>
        private INREG_PAYER ParseLineWithRegex(string line, int lineNumber)
        {
            var payer = new INREG_PAYER();

            if (_configuration.TagMappings == null || _configuration.TagMappings.Count == 0)
            {
                // Если нет конфигурации, пробуем базовый парсинг
                throw new ConfigurationException("В конфигурации не заполнен список маппингов тегов на поля DTO.");
            }

            foreach (var mapping in _configuration.TagMappings)
            {
                if (mapping.Skip) continue;

                string value = null;

                Regex regex;
                // Извлечение значения через regex
                if (_compiledRegexes.TryGetValue(mapping.PropertyName, out regex))
                {
                    var match = regex.Match(line);
                    if (match.Success && match.Groups.Count > mapping.RegexGroupIndex)
                    {
                        value = match.Groups[mapping.RegexGroupIndex].Value;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(mapping.RegexPattern))
                {
                    // Компиляция на лету если не было скомпиллировано
                    regex = new Regex(mapping.RegexPattern);
                    var match = regex.Match(line);
                    if (match.Success && match.Groups.Count > mapping.RegexGroupIndex)
                    {
                        value = match.Groups[mapping.RegexGroupIndex].Value;
                    }
                }

                // Применяем трансформации
                value = ApplyTransformations(value, mapping);

                // Устанавливаем значение
                if (!string.IsNullOrWhiteSpace(mapping.PropertyName))
                {
                    SetPropertyValue(payer, mapping.PropertyName, value, mapping);
                }
            }

            return payer;
        }

        /// <summary>
        /// Применение трансформаций к значению
        /// </summary>
        private string ApplyTransformations(string value, TagMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            // Удаление префикса
            if (!string.IsNullOrWhiteSpace(mapping.Prefix) && value.StartsWith(mapping.Prefix))
            {
                value = value.Substring(mapping.Prefix.Length);
            }

            // Удаление постфикса
            if (!string.IsNullOrWhiteSpace(mapping.Postfix) && value.EndsWith(mapping.Postfix))
            {
                value = value.Substring(0, value.Length - mapping.Postfix.Length);
            }

            // Ограничение длины
            if (mapping.Length.HasValue && value.Length > mapping.Length.Value)
            {
                value = value.Substring(0, mapping.Length.Value);
            }

            return value;
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
            catch (Exception ex)
            {
                throw new MappingException(
                    $"Ошибка установки свойства {propertyName}: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Конвертация значения в целевой тип
        /// </summary>
        private object ConvertValue(string value, Type targetType, TagMapping mapping)
        {
            if (string.IsNullOrWhiteSpace(value))
                return mapping.DefaultValue;

            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                switch (underlyingType.Name)
                {
                    case "String":
                        return value;

                    case "Int32":
                        return int.TryParse(value, out int i) ? i : mapping.DefaultValue;

                    case "Int64":
                        return long.TryParse(value, out long l) ? l : mapping.DefaultValue;

                    case "Decimal":
                        string normalizedValue = value.Replace(
                            mapping.DecimalSeparator ?? ".",
                            System.Globalization.CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator);
                        return decimal.TryParse(normalizedValue, out decimal d) ? d : mapping.DefaultValue;

                    case "DateTime":
                        if (!string.IsNullOrWhiteSpace(mapping.Format))
                        {
                            return DateTime.TryParseExact(value, mapping.Format,
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out DateTime dtFormat)
                                ? dtFormat : mapping.DefaultValue;
                        }
                        return DateTime.TryParse(value, out DateTime dtParse) ? dtParse : mapping.DefaultValue;

                    case "Boolean":
                        return bool.TryParse(value, out bool b) ? b : mapping.DefaultValue;

                    default:
                        return value;
                }
            }
            catch
            {
                return mapping.DefaultValue;
            }
        }

        /// <summary>
        /// Обработка ошибки строки
        /// </summary>
        private void HandleLineError(ParseResult result, Exception ex, string line, int lineNumber)
        {
            result.SkippedLinesCount++;

            if (_configuration.SkipErrorLines)
            {
                result.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.LineError,
                    LineNumber = lineNumber,
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
