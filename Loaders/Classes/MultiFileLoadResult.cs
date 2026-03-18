using RegistryParser.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Loaders.Classes
{
    /// <summary>
    /// Результат загрузки нескольких файлов
    /// </summary>
    public class MultiFileLoadResult
    {
        public MultiFileLoadResult()
        {
            FileContents = new Dictionary<string, IDictionary<string, string>>();
            Errors = new List<ParseError>();
        }

        /// <summary>
        /// Содержимое файлов, сгруппированное по типу
        /// </summary>
        public IDictionary<string, IDictionary<string, string>> FileContents { get; set; }

        /// <summary>
        /// Ошибки загрузки
        /// </summary>
        public List<ParseError> Errors { get; set; }

        /// <summary>
        /// Флаг успешности
        /// </summary>
        public bool IsSuccess { get; set; }
    }
}
