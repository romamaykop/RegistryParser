using RegistryParser.Models.DTOs;
using RegistryParser.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Models
{
    /// <summary>
    /// Основной DTO объект реестра
    /// Содержит итоговый результат парсинга со списком плательщиков и ошибок
    /// </summary>
    public class Registry
    {
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public Registry()
        {
            Payers = new List<INREG_PAYER>();
            Counters = new List<INREG_COUNTER>();
            Services = new List<INREG_SERVICE>();
            Errors = new List<ParseError>();
            Metadata = new Dictionary<string, string>();
            ProcessedFiles = new List<string>();
        }

        /// <summary>
        /// Уникальный идентификатор реестра
        /// </summary>
        public Guid RegistryId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Список плательщиков из реестра
        /// </summary>
        public List<INREG_PAYER> Payers { get; set; }

        /// <summary>
        /// Список счётчиков из реестра
        /// </summary>
        public List<INREG_COUNTER> Counters { get; set; }

        /// <summary>
        /// Список услуг из реестра
        /// </summary>
        public List<INREG_SERVICE> Services { get; set; }

        /// <summary>
        /// Список ошибок, возникших при обработке реестра
        /// </summary>
        public List<ParseError> Errors { get; set; }

        /// <summary>
        /// Метаданные реестра (имя файла, дата загрузки, тип формата и т.д.)
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// Дата и время создания реестра
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Статус обработки реестра
        /// </summary>
        public RegistryStatus Status { get; set; } = RegistryStatus.Pending;

        /// <summary>
        /// Тип формата исходных файлов
        /// </summary>
        public string SourceFormat { get; set; }

        /// <summary>
        /// Имена обработанных файлов
        /// </summary>
        public List<string> ProcessedFiles { get; set; }

        /// <summary>
        /// Общее количество плательщиков
        /// </summary>
        public int PayerCount => Payers?.Count ?? 0;

        /// <summary>
        /// Общее количество счётчиков
        /// </summary>
        public int CounterCount => Counters?.Count ?? 0;

        /// <summary>
        /// Общее количество услуг
        /// </summary>
        public int ServiceCount => Services?.Count ?? 0;

        /// <summary>
        /// Общее количество ошибок
        /// </summary>
        public int ErrorCount => Errors?.Count ?? 0;

        /// <summary>
        /// Флаг наличия критических ошибок
        /// </summary>
        public bool HasCriticalErrors => Errors.Exists(e => e.ErrorType == ErrorType.FileError);
    }
}
