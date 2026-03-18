using RegistryParser.Extensions.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Configuration
{
    /// <summary>
    /// Конфигурация маппинга тега на поле DTO
    /// Позволяет конфигурировать парсинг
    /// </summary>
    public class TagMapping
    {
        /// <summary>
        /// Индекс колонки в файле (подсчет от 0)
        /// Пример: 0 = первая колонка, 4 = пятая колонка
        /// </summary>
        public int? ColumnIndex { get; set; }

        /// <summary>
        /// Имя колонки (если есть заголовки)
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Имя свойства в DTO
        /// Пример: "IRP_PAYER_FULLNAME", "IRP_PAYER_FCNUMBER"
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Тип данных (по умолчанию String)
        /// </summary>
        public DataType DataType { get; set; } = DataType.String;

        /// <summary>
        /// Максимальная длина строки
        /// </summary>
        public int? Length { get; set; }

        /// <summary>
        /// Префикс для удаления
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Постфикс для удаления
        /// </summary>
        public string Postfix { get; set; }

        /// <summary>
        /// Разделитель десятичных знаков
        /// </summary>
        public string DecimalSeparator { get; set; }

        /// <summary>
        /// Формат даты
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Поле обязательное
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// Пропустить поле
        /// </summary>
        public bool Skip { get; set; }

        /// <summary>
        /// Значение по умолчанию
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Пользовательская функция трансформации значения
        /// Пример: value => value.ToUpper()
        /// </summary>
        public Func<string, object> TransformFunc { get; set; }

        /// <summary>
        /// Регулярное выражение для извлечения значения
        /// Используется только в UniversalRegexParser
        /// Пример: @"^([^;]+);" для извлечения первого поля
        /// </summary>
        public string RegexPattern { get; set; }

        /// <summary>
        /// Индекс группы в регулярном выражении (1-based)
        /// Используется только в UniversalRegexParser
        /// Пример: 1 = первая группа захвата
        /// </summary>
        public int RegexGroupIndex { get; set; } = 1;
    }
}
