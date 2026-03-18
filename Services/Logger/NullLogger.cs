using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Services.Logger
{
    /// <summary>
    /// Пустой логгер (заглушка)
    /// </summary>
    public class NullLogger : ILogger
    {
        public void LogInformation(string message) { }
        public void LogError(string message, Exception ex = null) { }
        public void LogWarning(string message) { }
        public void LogDebug(string message) { }
    }
}
