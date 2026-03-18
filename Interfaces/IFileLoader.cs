using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryParser.Interfaces
{
    /// <summary>
    /// Интерфейс загрузчика файлов
    /// Отвечает за чтение файлов различных форматов
    /// </summary>
    public interface IFileLoader
    {
        /// <summary>
        /// Загрузка содержимого файла как строки
        /// </summary>
        Task<string> LoadAsStringAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Загрузка содержимого файла как байтового массива
        /// </summary>
        Task<byte[]> LoadAsBytesAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Загрузка нескольких файлов
        /// </summary>
        Task<System.Collections.Generic.Dictionary<string, string>> LoadMultipleAsync(
            string[] filePaths,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверка существования файла
        /// </summary>
        Task<bool> FileExistsAsync(string filePath);

        /// <summary>
        /// Проверка наличия всех требуемых файлов
        /// </summary>
        Task<bool> AllFilesExistAsync(string[] filePaths);
    }
}
