using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Services.Logger
{
    /// <summary>
    /// Интерфейс логгера для интеграции с существующим логгером проекта
    /// </summary>
    public interface ILogger
    {
        void LogInformation(string message);
        void LogError(string message, Exception ex = null);
        void LogWarning(string message);
        void LogDebug(string message);
    }
}
