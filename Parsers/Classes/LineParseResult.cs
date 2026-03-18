using RegistryParser.Models.DTOs;
using System.Collections.Generic;

namespace RegistryParser.Parsers.Classes
{
    /// <summary>
    /// Результат парсинга одной строки
    /// </summary>
    public class LineParseResult
    {
        public INREG_PAYER Payer { get; set; }
        public List<INREG_COUNTER> Counters { get; set; } = new List<INREG_COUNTER>();
        public List<INREG_SERVICE> Services { get; set; } = new List<INREG_SERVICE>();
    }
}
