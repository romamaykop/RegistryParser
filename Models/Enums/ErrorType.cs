using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Models.Enums
{
    /// <summary>
    /// Типы ошибок парсинга
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Ошибка в отдельной строке (продолжаем обработку)
        /// </summary>
        LineError,

        /// <summary>
        /// Критическая ошибка файла (прерываем обработку)
        /// </summary>
        FileError,

        /// <summary>
        /// Ошибка валидации данных
        /// </summary>
        ValidationError,

        /// <summary>
        /// Ошибка маппинга полей
        /// </summary>
        MappingError
    }
}
