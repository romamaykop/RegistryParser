using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Parsers.Classes
{
    /// <summary>
    /// Исключение конфигурации
    /// </summary>
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception inner) : base(message, inner) { }
    }
}
