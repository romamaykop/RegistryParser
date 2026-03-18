using RegistryParser.Configuration;
using RegistryParser.Extensions.Enums;
using RegistryParser.Interfaces;
using RegistryParser.Loaders;
using RegistryParser.Models;
using RegistryParser.Models.DTOs;
using RegistryParser.Models.Enums;
using RegistryParser.Services.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryParser.Services
{
    /// <summary>
    /// Основной сервис парсинга реестров
    /// Получает конфигурацию как объект ParserConfiguration
    /// </summary>
    public class RegistryParserService
    {
        private readonly IParserFactory _parserFactory;
        private readonly IFileLoader _fileLoader;
        private readonly ILogger _logger;

        /// <summary>
        /// Конструктор
        /// </summary>
        public RegistryParserService(
            IParserFactory parserFactory = null,
            IFileLoader fileLoader = null,
            ILogger logger = null)
        {
            _parserFactory = parserFactory ?? new ParserFactory();
            _fileLoader = fileLoader ?? new FileLoader();
            _logger = logger ?? new NullLogger();
        }

        /// <summary>
        /// Парсинг одного файла с конфигурацией
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="configuration">Конфигурация парсинга (маппинг полей)</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Объект Registry с результатами</returns>
        public async Task<Registry> ParseAsync(
            string filePath,
            ParserConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration),
                    "Конфигурация не может быть null. Создайте ParserConfiguration и передайте в метод.");
            }

            var registry = new Registry
            {
                SourceFormat = configuration.ParserType.ToString(),
                Status = RegistryStatus.Processing
            };

            try
            {
                _logger.LogInformation(
                    $"Начало парсинга: {filePath}, конфигурация: {configuration.Name ?? "без имени"}");

                // Передаём конфигурацию в фабрику
                var parser = _parserFactory.CreateParser(configuration);
                var parseResult = await parser.ParseAsync(filePath, cancellationToken)
                    .ConfigureAwait(false);

                registry.Payers = parseResult.Payers;
                registry.Counters = parseResult.Counters;
                registry.Services = parseResult.Services;
                registry.Errors = parseResult.Errors;
                registry.ProcessedFiles.Add(filePath);

                registry.Metadata["TotalLines"] = parseResult.TotalLinesProcessed.ToString();
                registry.Metadata["SuccessLines"] = parseResult.SuccessLinesCount.ToString();
                registry.Metadata["SkippedLines"] = parseResult.SkippedLinesCount.ToString();
                registry.Metadata["ConfigurationName"] = configuration.Name ?? "Unknown";

                registry.Status = parseResult.IsSuccess
                    ? (parseResult.Errors.Count > 0
                        ? RegistryStatus.CompletedWithErrors
                        : RegistryStatus.Completed)
                    : RegistryStatus.Failed;

                _logger.LogInformation(
                    $"Парсинг завершён. Плательщиков: {registry.PayerCount}, Ошибок: {registry.ErrorCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Критическая ошибка парсинга: {ex.Message}", ex);

                registry.Status = RegistryStatus.Failed;
                registry.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.FileError,
                    FileName = filePath,
                    Message = ex.Message,
                    Exception = ex
                });
            }

            return registry;
        }

        /// <summary>
        /// Парсинг нескольких файлов с конфигурацией
        /// </summary>
        public async Task<Registry> ParseMultipleFilesAsync(
            MultiFileParseRequest request,
            CancellationToken cancellationToken = default)
        {
            var registry = new Registry
            {
                SourceFormat = "MultiFile",
                Status = RegistryStatus.Processing
            };

            try
            {
                _logger.LogInformation($"Начало парсинга нескольких файлов");

                // 1. Парсим плательщиков
                if (request.PayersFileConfig != null)
                {
                    var payersResult = await ParseAsync(
                        request.PayersFileConfig.FilePath,
                        request.PayersFileConfig.Configuration,
                        cancellationToken).ConfigureAwait(false);

                    registry.Payers = payersResult.Payers;
                    registry.Errors.AddRange(payersResult.Errors.Where(e => e.ErrorType == ErrorType.FileError));
                }

                // 2. Парсим счётчики и привязываем к плательщикам
                if (request.CountersFileConfig != null && registry.Payers.Count > 0)
                {
                    var countersResult = await ParseAsync(
                        request.CountersFileConfig.FilePath,
                        request.CountersFileConfig.Configuration,
                        cancellationToken).ConfigureAwait(false);

                    registry.Counters = countersResult.Counters;

                    // Привязываем счётчики к плательщикам
                    LinkCountersToPayers(registry.Payers, registry.Counters);
                }

                // 3. Парсим услуги и привязываем к плательщикам
                if (request.ServicesFileConfig != null && registry.Payers.Count > 0)
                {
                    var servicesResult = await ParseAsync(
                        request.ServicesFileConfig.FilePath,
                        request.ServicesFileConfig.Configuration,
                        cancellationToken).ConfigureAwait(false);

                    registry.Services = servicesResult.Services;

                    // Привязываем услуги к плательщикам
                    LinkServicesToPayers(registry.Payers, registry.Services);
                }

                registry.ProcessedFiles.Add(request.PayersFileConfig?.FilePath ?? "");
                if (request.CountersFileConfig != null)
                    registry.ProcessedFiles.Add(request.CountersFileConfig.FilePath);
                if (request.ServicesFileConfig != null)
                    registry.ProcessedFiles.Add(request.ServicesFileConfig.FilePath);

                registry.Status = registry.HasCriticalErrors
                    ? RegistryStatus.Failed
                    : (registry.Errors.Count > 0
                        ? RegistryStatus.CompletedWithErrors
                        : RegistryStatus.Completed);

                _logger.LogInformation(
                    $"Парсинг завершён. Плательщиков: {registry.PayerCount}, " +
                    $"Счётчиков: {registry.CounterCount}, Услуг: {registry.ServiceCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Критическая ошибка парсинга: {ex.Message}", ex);

                registry.Status = RegistryStatus.Failed;
                registry.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.FileError,
                    Message = ex.Message,
                    Exception = ex
                });
            }

            return registry;
        }

        /// <summary>
        /// Привязка счётчиков к плательщикам по ID
        /// </summary>
        private void LinkCountersToPayers(
            System.Collections.Generic.List<INREG_PAYER> payers,
            System.Collections.Generic.List<INREG_COUNTER> counters)
        {
            var payerLookup = payers.ToDictionary(p => p.ID, p => p);

            foreach (var counter in counters)
            {
                if (payerLookup.TryGetValue(counter.IRC_PAYER_ID, out var payer))
                {
                    counter.INREG_PAYER = payer;
                    payer.INREG_COUNTER.Add(counter);
                }
            }
        }

        /// <summary>
        /// Привязка услуг к плательщикам по ID
        /// </summary>
        private void LinkServicesToPayers(
            System.Collections.Generic.List<INREG_PAYER> payers,
            System.Collections.Generic.List<INREG_SERVICE> services)
        {
            var payerLookup = payers.ToDictionary(p => p.ID, p => p);

            foreach (var service in services)
            {
                if (payerLookup.TryGetValue(service.IRS_PAYER_ID, out var payer))
                {
                    service.INREG_PAYER = payer;
                    payer.INREG_SERVICE.Add(service);
                }
            }
        }

        /// <summary>
        /// Парсинг из строкового содержимого с конфигурацией
        /// </summary>
        public async Task<Registry> ParseContentAsync(
            string content,
            ParserConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration),
                    "Конфигурация не может быть null.");
            }

            var registry = new Registry
            {
                SourceFormat = configuration.ParserType.ToString(),
                Status = RegistryStatus.Processing
            };

            try
            {
                var parser = _parserFactory.CreateParser(configuration);
                var parseResult = await parser.ParseContentAsync(content, cancellationToken)
                    .ConfigureAwait(false);

                registry.Payers = parseResult.Payers;
                registry.Counters = parseResult.Counters;
                registry.Services = parseResult.Services;
                registry.Errors = parseResult.Errors;

                registry.Status = parseResult.IsSuccess
                    ? (parseResult.Errors.Count > 0
                        ? RegistryStatus.CompletedWithErrors
                        : RegistryStatus.Completed)
                    : RegistryStatus.Failed;
            }
            catch (Exception ex)
            {
                registry.Status = RegistryStatus.Failed;
                registry.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.FileError,
                    Message = ex.Message,
                    Exception = ex
                });
            }

            return registry;
        }
    }
}
