using RegistryParser.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Models
{
    /// <summary>
    /// Модель ошибки парсинга
    /// Содержит информацию об ошибке, номере строки и типе ошибки
    /// </summary>
    public class ParseError
    {
        /// <summary>
        /// Тип ошибки (строка/файл)
        /// </summary>
        public ErrorType ErrorType { get; set; }

        /// <summary>
        /// Номер строки, где произошла ошибка (для ошибок строк)
        /// </summary>
        public int? LineNumber { get; set; }

        /// <summary>
        /// Имя файла, где произошла ошибка
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Исключение, вызвавшее ошибку
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Содержимое строки, где произошла ошибка
        /// </summary>
        public string LineContent { get; set; }

        /// <summary>
        /// Время возникновения ошибки
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
