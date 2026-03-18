using RegistryParser.Interfaces;
using RegistryParser.Loaders.Classes;
using RegistryParser.Models;
using RegistryParser.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryParser.Loaders
{
    /// <summary>
    /// Загрузчик для сложных форматов с несколькими файлами
    /// Поддерживает последовательную загрузку: сначала абоненты, затем услуги/счётчики
    /// </summary>
    public class MultiFileLoader
    {
        private readonly IFileLoader _fileLoader;
        private readonly IDictionary<string, string> _filePatterns;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="fileLoader">Базовый загрузчик файлов</param>
        /// <param name="filePatterns">Паттерны имен файлов (payers, counters, services)</param>
        public MultiFileLoader(
            IFileLoader fileLoader,
            IDictionary<string, string> filePatterns = null)
        {
            _fileLoader = fileLoader ?? new FileLoader();
            _filePatterns = filePatterns ?? new Dictionary<string, string>
            {
                { "payers", "*payers*" },
                { "counters", "*counters*" },
                { "services", "*services*" }
            };
        }

        /// <summary>
        /// Загрузка файлов в правильном порядке
        /// Сначала файлы с абонентами, затем услуги/счётчики
        /// </summary>
        public async Task<MultiFileLoadResult> LoadInOrderAsync(
            string[] filePaths,
            CancellationToken cancellationToken = default)
        {
            var result = new MultiFileLoadResult();

            // Проверка наличия всех файлов
            var allExist = await _fileLoader.AllFilesExistAsync(filePaths)
                .ConfigureAwait(false);

            if (!allExist)
            {
                result.Errors.Add(new ParseError
                {
                    ErrorType = ErrorType.FileError,
                    Message = "Не все требуемые файлы существуют",
                    Timestamp = DateTime.UtcNow
                });
                return result;
            }

            // Сортируем файлы по приоритету
            var orderedFiles = OrderFilesByPriority(filePaths);

            // Последовательная загрузка
            foreach (var fileGroup in orderedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var contents = await _fileLoader.LoadMultipleAsync(
                    fileGroup.Value.ToArray(),
                    cancellationToken)
                    .ConfigureAwait(false);

                result.FileContents[fileGroup.Key] = contents;
            }

            result.IsSuccess = true;
            return result;
        }

        /// <summary>
        /// Сортировка файлов по приоритету загрузки
        /// </summary>
        private IDictionary<string, List<string>> OrderFilesByPriority(string[] filePaths)
        {
            var ordered = new Dictionary<string, List<string>>();
            var priorities = new[] { "payers", "counters", "services" };

            foreach (var priority in priorities)
            {
                var matchingFiles = filePaths
                    .Where(f => f.IndexOf(priority, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                if (matchingFiles.Count > 0)
                {
                    ordered[priority] = matchingFiles;
                }
            }

            // Файлы без приоритета
            var remainingFiles = filePaths
                .Where(f => !ordered.Values.SelectMany(v => v).Contains(f))
                .ToList();

            if (remainingFiles.Count > 0)
            {
                ordered["other"] = remainingFiles;
            }

            return ordered;
        }
    }
}
