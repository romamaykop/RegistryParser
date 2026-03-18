using RegistryParser.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryParser.Loaders
{
    /// <summary>
    /// Реализация загрузчика файлов
    /// Поддерживает асинхронное чтение файлов с прогрессом
    /// </summary>
    public class FileLoader : IFileLoader
    {
        private readonly Encoding _defaultEncoding;
        private readonly int _bufferSize;

        /// <summary>
        /// Конструктор с настройками по умолчанию
        /// </summary>
        public FileLoader() : this(Encoding.UTF8, 81920)
        {
        }

        /// <summary>
        /// Конструктор с кастомными настройками
        /// </summary>
        /// <param name="encoding">Кодировка файлов</param>
        /// <param name="bufferSize">Размер буфера</param>
        public FileLoader(Encoding encoding, int bufferSize)
        {
            _defaultEncoding = encoding;
            _bufferSize = bufferSize;
        }

        /// <summary>
        /// Загрузка содержимого файла как строки
        /// Оптимизировано для больших файлов через StreamReader
        /// </summary>
        public async Task<string> LoadAsStringAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл не найден: {filePath}", filePath);
            }

            // Используем StreamReader для эффективного чтения больших файлов
            using (var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                _bufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var reader = new StreamReader(stream, _defaultEncoding))
            {
                return await reader.ReadToEndAsync()
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Загрузка содержимого файла как байтового массива
        /// Используется для бинарных форматов (DBF, XLS)
        /// </summary>
        public async Task<byte[]> LoadAsBytesAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл не найден: {filePath}", filePath);
            }

            using (var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                _bufferSize,
                FileOptions.Asynchronous))
            {
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, _bufferSize, cancellationToken)
                    .ConfigureAwait(false);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Загрузка нескольких файлов
        /// Возвращает словарь: путь -> содержимое
        /// </summary>
        public async Task<Dictionary<string, string>> LoadMultipleAsync(
            string[] filePaths,
            CancellationToken cancellationToken = default)
        {
            if (filePaths == null || filePaths.Length == 0)
            {
                throw new ArgumentException("Список файлов не может быть пустым", nameof(filePaths));
            }

            var results = new Dictionary<string, string>();

            // Загружаем файлы параллельно для производительности
            var tasks = new List<Task<KeyValuePair<string, string>>>();

            foreach (var filePath in filePaths)
            {
                tasks.Add(LoadFileWithKeyAsync(filePath, cancellationToken));
            }

            var loadedFiles = await Task.WhenAll(tasks)
                .ConfigureAwait(false);

            foreach (var kvp in loadedFiles)
            {
                results[kvp.Key] = kvp.Value;
            }

            return results;
        }

        /// <summary>
        /// Вспомогательный метод для загрузки файла с ключом
        /// </summary>
        private async Task<KeyValuePair<string, string>> LoadFileWithKeyAsync(
            string filePath,
            CancellationToken cancellationToken)
        {
            var content = await LoadAsStringAsync(filePath, cancellationToken)
                .ConfigureAwait(false);
            return new KeyValuePair<string, string>(filePath, content);
        }

        /// <summary>
        /// Проверка существования файла
        /// </summary>
        public Task<bool> FileExistsAsync(string filePath)
        {
            return Task.FromResult(File.Exists(filePath));
        }

        /// <summary>
        /// Проверка наличия всех требуемых файлов
        /// </summary>
        public async Task<bool> AllFilesExistAsync(string[] filePaths)
        {
            if (filePaths == null || filePaths.Length == 0)
            {
                return false;
            }

            var tasks = new List<Task<bool>>();

            foreach (var filePath in filePaths)
            {
                tasks.Add(FileExistsAsync(filePath));
            }

            var results = await Task.WhenAll(tasks)
                .ConfigureAwait(false);

            // Все файлы должны существовать
            return Array.TrueForAll(results, exists => exists);
        }
    }
}
