using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RegistryParser.Services
{
    /// <summary>
    /// Расширения для работы со строками
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Безопасное извлечение подстроки
        /// </summary>
        public static string SafeSubstring(this string value, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (startIndex < 0)
                startIndex = 0;

            if (startIndex >= value.Length)
                return string.Empty;

            if (length < 0)
                length = 0;

            if (startIndex + length > value.Length)
                length = value.Length - startIndex;

            return value.Substring(startIndex, length);
        }

        /// <summary>
        /// Очистка строки от специальных символов
        /// </summary>
        public static string Clean(this string value, bool removeWhitespace = false)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var result = Regex.Replace(value, @"[^\w\s\.\-\,\;\:]", string.Empty);

            if (removeWhitespace)
            {
                result = Regex.Replace(result, @"\s+", string.Empty);
            }

            return result;
        }

        /// <summary>
        /// Нормализация разделителя десятичных знаков
        /// </summary>
        public static string NormalizeDecimalSeparator(this string value, string targetSeparator = ".")
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Replace(",", targetSeparator);
        }

        /// <summary>
        /// Проверка на пустое или null значение
        /// </summary>
        public static bool IsNullOrEmptyOrWhitespace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Извлечение числа из строки
        /// </summary>
        public static int? ExtractInt(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            var match = Regex.Match(value, @"\d+");
            return match.Success ? int.Parse(match.Value) : (int?)null;
        }

        /// <summary>
        /// Извлечение decimal из строки
        /// </summary>
        public static decimal? ExtractDecimal(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            var match = Regex.Match(value, @"[\d\,\.\-]+");
            return match.Success && decimal.TryParse(match.Value, out var result)
                ? result
                : (decimal?)null;
        }
    }
}
