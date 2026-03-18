using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Models.Enums
{
    /// <summary>
    /// Статусы обработки реестра
    /// </summary>
    public enum RegistryStatus
    {
        /// <summary>
        /// Ожидает обработки
        /// </summary>
        Pending,

        /// <summary>
        /// В процессе обработки
        /// </summary>
        Processing,

        /// <summary>
        /// Обработка завершена успешно
        /// </summary>
        Completed,

        /// <summary>
        /// Обработка завершена с ошибками
        /// </summary>
        CompletedWithErrors,

        /// <summary>
        /// Обработка прервана из-за критической ошибки
        /// </summary>
        Failed
    }
}
