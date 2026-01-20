using System;
using System.Collections.ObjectModel;
using LinkProcessor.Models;

namespace LinkProcessor.Services
{
    /// <summary>
    /// Сервис для ведения логов приложения (Singleton)
    /// </summary>
    public class LogService
    {
        private static readonly Lazy<LogService> _instance = new Lazy<LogService>(() => new LogService());

        public static LogService Instance => _instance.Value;

        public ObservableCollection<LogEntry> Logs { get; }

        private LogService()
        {
            Logs = new ObservableCollection<LogEntry>();
        }

        /// <summary>
        /// Добавляет запись в лог
        /// </summary>
        public void AddLog(string message, LogLevel level = LogLevel.Info)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message
            };

            // Добавляем в UI потоке
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Logs.Add(entry);

                    // Ограничиваем количество записей в логе (храним последние 1000)
                    while (Logs.Count > 1000)
                    {
                        Logs.RemoveAt(0);
                    }
                });
            }
        }

        /// <summary>
        /// Очищает все логи
        /// </summary>
        public void ClearLogs()
        {
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Logs.Clear();
                });
            }
        }
    }
}