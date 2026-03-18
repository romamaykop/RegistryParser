using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Configuration
{
    /// <summary>
    /// запрос на парсинг нескольких файлов
    /// </summary>
    public class MultiFileParseRequest
    {
        public FileConfiguration PayersFileConfig { get; set; }
        public FileConfiguration CountersFileConfig { get; set; }
        public FileConfiguration ServicesFileConfig { get; set; }
    }
}
