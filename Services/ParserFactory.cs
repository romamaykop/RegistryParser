using RegistryParser.Configuration;
using RegistryParser.Extensions.Enums;
using RegistryParser.Interfaces;
using RegistryParser.Parsers;
using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Services
{
    /// <summary>
    /// Фабрика парсеров
    /// Реализует паттерн Factory для создания парсеров
    /// </summary>
    public class ParserFactory : IParserFactory
    {
        private readonly IDictionary<ParserType, Func<IRegistryParser>> _parserFactories;

        /// <summary>
        /// Конструктор с регистрацией стандартных парсеров
        /// </summary>
        public ParserFactory()
        {
            _parserFactories = new Dictionary<ParserType, Func<IRegistryParser>>
            {
                { ParserType.Csv, () => new CsvParser() },
                { ParserType.Dbf, () => new DbfParser() },
                { ParserType.Excel, () => new ExcelParser() },
                { ParserType.UniversalRegex, () => new UniversalRegexParser() }
            };
        }

        /// <summary>
        /// Создание парсера по типу
        /// </summary>
        public IRegistryParser CreateParser(ParserType parserType)
        {
            if (_parserFactories.TryGetValue(parserType, out var factory))
            {
                return factory();
            }

            throw new ArgumentException($"Парсер типа {parserType} не зарегистрирован");
        }

        /// <summary>
        /// Создание парсера по конфигурации
        /// </summary>
        public IRegistryParser CreateParser(ParserConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration),
                    "Конфигурация не может быть null");
            }

            // Создаём парсер с конфигурацией
            IRegistryParser parser = null;
            switch (configuration.ParserType)
            {
                case ParserType.Csv:
                    parser = new CsvParser(configuration);
                    break;
                case ParserType.Dbf:
                    parser = new DbfParser(configuration);
                    break;
                case ParserType.Excel:
                    parser = new ExcelParser(configuration);
                    break;
                case ParserType.UniversalRegex:
                    parser = new UniversalRegexParser(configuration);
                    break;
                default:
                    throw new ArgumentException($"Неподдерживаемый тип парсера: {configuration.ParserType}");
            }

            return parser;
        }

        /// <summary>
        /// Регистрация кастомного парсера
        /// Позволяет добавлять новые парсеры
        /// </summary>
        public void RegisterParser(ParserType parserType, Func<IRegistryParser> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _parserFactories[parserType] = factory;
        }
    }
}
