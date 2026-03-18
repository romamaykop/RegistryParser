using RegistryParser.Extensions.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Configuration
{
    /// <summary>
    /// Специфический маппинг одного поля
    /// </summary>
    public class FieldMapping
    {
        /// <summary>
        /// Индекс поля (0-based)
        /// </summary>
        public int FieldIndex { get; set; }

        /// <summary>
        /// Имя свойства в DTO
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Тип данных
        /// </summary>
        public DataType DataType { get; set; } = DataType.String;

        /// <summary>
        /// Формат даты (если DataType = DateTime)
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// Игнорировать это поле
        /// </summary>
        public bool Ignore { get; set; }
    }
}
