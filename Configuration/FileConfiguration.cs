using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Configuration
{
    /// <summary>
    /// конфигурация файла
    /// </summary>
    public class FileConfiguration
    {
        public string FilePath { get; set; }
        public ParserConfiguration Configuration { get; set; }
    }
}
