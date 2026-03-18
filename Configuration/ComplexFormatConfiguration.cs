using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Configuration
{
    /// <summary>
    /// Конфигурация для сложных форматов
    /// Поддерживает несколько разделителей и иерархическую структуру
    /// </summary>
    public class ComplexFormatConfiguration
    {
        public ComplexFormatConfiguration()
        {
            PayerMappings = new List<FieldMapping>();
            CounterMappings = new List<FieldMapping>();
            ServiceMappings = new List<FieldMapping>();
        }

        /// <summary>
        /// Имя конфигурации
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Основной разделитель (между полями плательщика) (по умолчанию ";")
        /// </summary>
        public char MainDelimiter { get; set; } = ';';

        /// <summary>
        /// Разделитель для счётчиков и услуг (по умолчанию ":")
        /// </summary>
        public char SubDelimiter { get; set; } = ':';

        /// <summary>
        /// Маркер начала блока услуг (по умолчанию "[!]")
        /// </summary>
        public string ServicesBlockMarker { get; set; } = "[!]";

        /// <summary>
        /// Маппинг полей плательщика
        /// </summary>
        public List<FieldMapping> PayerMappings { get; set; }

        /// <summary>
        /// Маппинг полей счётчика (повторяющаяся группа)
        /// </summary>
        public List<FieldMapping> CounterMappings { get; set; }

        /// <summary>
        /// Маппинг полей услуги (повторяющаяся группа)
        /// </summary>
        public List<FieldMapping> ServiceMappings { get; set; }

        /// <summary>
        /// Количество полей в одном блоке счётчика
        /// </summary>
        public int CounterBlockSize { get; set; } = 3;

        /// <summary>
        /// Количество полей в одном блоке услуги
        /// </summary>
        public int ServiceBlockSize { get; set; } = 3;

        /// <summary>
        /// Разделитель десятичных знаков  (по умолчанию ".")
        /// </summary>
        public string DecimalSeparator { get; set; } = ".";

        /// <summary>
        /// Формат даты для периода  (по умолчанию "MMyy")
        /// </summary>
        public string PeriodDateFormat { get; set; } = "MMyy";
    }
}
