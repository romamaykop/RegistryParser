using DotNetDBF;
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
    /// Парсер DBF файлов
    /// Использует библиотеку dotnetdbf для чтения бинарных DBF файлов
    /// </summary>
    public class DbfParser : IRegistryParser
    {
        private readonly ParserConfiguration _configuration;
        private readonly IFileLoader _fileLoader;

        /// <summary>
        /// Тип парсера
        /// </summary>
        public ParserType ParserType => ParserType.Dbf;

        /// <summary>
        /// Конструктор
        /// </summary>
        public DbfParser(ParserConfiguration configuration = null, IFileLoader fileLoader = null)
        {
            _configuration = configuration ?? new ParserConfiguration();
            _fileLoader = fileLoader ?? new FileLoader();
        }

        /// <summary>
        /// Парсинг DBF файла
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

                // Читаем файл как байты
                var bytes = await _fileLoader.LoadAsBytesAsync(filePath, cancellationToken)
                    .ConfigureAwait(false);

                // Используем dotnetdbf для парсинга
                using (var stream = new MemoryStream(bytes))
                {
                    using (var dbf = new DBFReader(stream))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var payer = MapDbfRecordToPayer(dbf);
                        result.Payers.Add(payer);
                        result.SuccessLinesCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.FileError,
                    FileName = filePath,
                    Message = $"Ошибка при парсинге DBF: {ex.Message}",
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
            throw new NotSupportedException("DBF формат не поддерживает парсинг из строкового содержимого");
        }

        /// <summary>
        /// Маппинг записи DBF в объект Payer
        /// </summary>
        private INREG_PAYER MapDbfRecordToPayer(object record)
        {
            // Реализация зависит от структуры DBF файла
            // Использует конфигурацию TagMappings для маппинга полей
            var payer = new INREG_PAYER();

            if (_configuration.TagMappings != null)
            {
                foreach (var mapping in _configuration.TagMappings)
                {
                    // Получение значения из записи DBF по имени колонки
                    // var value = GetDbfFieldValue(record, mapping.ColumnName);
                    // SetPropertyValue(payer, mapping.PropertyName, value, mapping);
                }
            }

            return payer;
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
