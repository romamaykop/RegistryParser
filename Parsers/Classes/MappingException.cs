using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Parsers.Classes
{
    /// <summary>
    /// Исключение маппинга
    /// </summary>
    public class MappingException : Exception
    {
        public MappingException(string message) : base(message) { }
        public MappingException(string message, Exception inner) : base(message, inner) { }
    }
}
