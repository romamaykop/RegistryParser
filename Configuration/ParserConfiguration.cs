using RegistryParser.Extensions.Enums;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Configuration
{
    /// <summary>
    /// Конфигурация парсера
    /// Передаётся в парсер как объект
    /// </summary>
    public class ParserConfiguration
    {
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public ParserConfiguration()
        {
            TagMappings = new List<TagMapping>();
            Encoding = Encoding.UTF8;
        }

        /// <summary>
        /// Имя конфигурации (для логирования)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Тип парсера
        /// </summary>
        public ParserType ParserType { get; set; }

        /// <summary>
        /// Список правил маппинга
        /// Пример: ColumnIndex = 0 → PropertyName = "IRP_PAYER_FULLNAME"
        /// </summary>
        public List<TagMapping> TagMappings { get; set; }

        /// <summary>
        /// Разделитель полей для CSV/TXT (по умолчанию ";")
        /// </summary>
        public char Delimiter { get; set; } = ';';

        /// <summary>
        /// Кодировка файла (по умолчанию UTF-8)
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Пропускать пустые строки (по умолчанию true)
        /// </summary>
        public bool SkipEmptyLines { get; set; } = true;

        /// <summary>
        /// Пропускать строки с ошибками (по умолчанию true)
        /// </summary>
        public bool SkipErrorLines { get; set; } = true;

        /// <summary>
        /// Разделитель десятичных знаков (по умолчанию ",")
        /// </summary>
        public string DecimalSeparator { get; set; } = ",";

        /// <summary>
        /// Флаг наличия заголовка в файле
        /// </summary>
        public bool HasHeader { get; set; } = false;

        /// <summary>
        /// Максимальное количество строк (0 = без ограничений)
        /// </summary>
        public int MaxLines { get; set; } = 0;

        /// <summary>
        /// Размер буфера для чтения (в байтах)
        /// </summary>
        public int BufferSize { get; set; } = 81920;

        /// <summary>
        /// Парсить счётчики из этого файла
        /// </summary>
        public bool ParseCounters { get; set; } = false;

        /// <summary>
        /// Парсить услуги из этого файла
        /// </summary>
        public bool ParseServices { get; set; } = false;

        /// <summary>
        /// Поле для связи с плательщиком (например, IRP_PAYER_FCNUMBER)
        /// </summary>
        public string PayerLinkField { get; set; }
    }
}