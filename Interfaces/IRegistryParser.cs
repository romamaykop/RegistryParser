using RegistryParser.Extensions.Enums;
using RegistryParser.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryParser.Interfaces
{
    /// <summary>
    /// Интерфейс парсера реестров
    /// Определяет контракт для всех реализаций парсеров
    /// </summary>
    public interface IRegistryParser
    {
        /// <summary>
        /// Тип парсера
        /// </summary>
        ParserType ParserType { get; }

        /// <summary>
        /// Парсинг файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат парсинга</returns>
        Task<ParseResult> ParseAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Парсинг нескольких файлов (для сложных форматов)
        /// </summary>
        /// <param name="filePaths">Пути к файлам</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат парсинга</returns>
        Task<ParseResult> ParseMultipleAsync(string[] filePaths, CancellationToken cancellationToken = default);

        /// <summary>
        /// Парсинг из строкового содержимого
        /// </summary>
        /// <param name="content">Содержимое файла</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат парсинга</returns>
        Task<ParseResult> ParseContentAsync(string content, CancellationToken cancellationToken = default);
    }
}
