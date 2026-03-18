using RegistryParser.Configuration;
using RegistryParser.Extensions.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Interfaces
{
    /// <summary>
    /// Фабрика парсеров
    /// Создает экземпляры парсеров на основе конфигурации
    /// </summary>
    public interface IParserFactory
    {
        /// <summary>
        /// Создание парсера по типу (с конфигурацией по умолчанию)
        /// </summary>
        IRegistryParser CreateParser(ParserType parserType);

        /// <summary>
        /// Создание парсера по конфигурации
        /// </summary>
        IRegistryParser CreateParser(ParserConfiguration configuration);

        /// <summary>
        /// Регистрация кастомного парсера
        /// </summary>
        void RegisterParser(ParserType parserType, System.Func<IRegistryParser> factory);
    }
}
