using RegistryParser.Models.DTOs;
using RegistryParser.Models.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RegistryParser.Models
{
    /// <summary>
    /// Результат парсинга реестра
    /// Содержит список плательщиков, счётчиков, услуг и ошибок
    /// </summary>
    public class ParseResult
    {
        public ParseResult()
        {
            Payers = new List<INREG_PAYER>();
            Counters = new List<INREG_COUNTER>();
            Services = new List<INREG_SERVICE>();
            Errors = new List<ParseError>();
        }

        public List<INREG_PAYER> Payers { get; set; }
        public List<INREG_COUNTER> Counters { get; set; }
        public List<INREG_SERVICE> Services { get; set; }
        public List<ParseError> Errors { get; set; }
        public int TotalLinesProcessed { get; set; }
        public int SuccessLinesCount { get; set; }
        public int SkippedLinesCount { get; set; }
        public bool IsSuccess => !Errors.Exists(e => e.ErrorType == ErrorType.FileError);
        public Stopwatch ProcessingTime { get; set; }
    }
}
