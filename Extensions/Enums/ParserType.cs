using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Extensions.Enums
{
    /// <summary>
    /// Типы парсеров
    /// </summary>
    public enum ParserType
    {
        /// <summary>
        /// CSV/TXT парсер
        /// </summary>
        Csv,

        /// <summary>
        /// DBF парсер
        /// </summary>
        Dbf,

        /// <summary>
        /// Excel (XLS/XLSX) парсер
        /// </summary>
        Excel,

        /// <summary>
        /// Универсальный парсер с регулярными выражениями
        /// </summary>
        UniversalRegex
    }
}
