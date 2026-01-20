using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace LinkProcessor.Models
{
    /// <summary>
    /// Модель парсинга ссылки из текста файла
    /// </summary>
    public class TextLink
    {
        /// <summary>
        /// Порядковый номер
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// Ссылка для поиска
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Строка замены
        /// </summary>
        public LinkItem Link { get; set;  }
    }

    /// <summary>
    /// Модель для представления найденной ссылки
    /// </summary>
    public class LinkItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private string _title;

        /// <summary>
        /// Оригинальная ссылка из текста (может быть в формате markdown или чистый URL)
        /// </summary>
        public string OriginalLink { get; set; }

        /// <summary>
        /// Извлеченный URL адрес
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Заголовок ссылки (из markdown или полученный из Google)
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        /// <summary>
        /// Выбрана ли ссылка для обработки
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        /// <summary>
        /// Текст для отображения в списке
        /// </summary>
        public string DisplayText
        {
            get
            {
                if (!string.IsNullOrEmpty(Title))
                    return $"{Title} ({Url})";
                return Url;
            }
        }

        /// <summary>
        /// Позиция ссылки в исходном тексте
        /// </summary>
        public int Position { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Конфигурация приложения
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Список последних открытых файлов
        /// </summary>
        public List<string> RecentFiles { get; set; } = new List<string>();

        /// <summary>
        /// Шаблон для замены ссылки в тексте
        /// Пример: "[{number}]" - заменит ссылку на [1], [2] и т.д.
        /// {number} - порядковый номер ссылки
        /// </summary>
        public string LinkReplacementTemplate { get; set; } = "[{number}]";

        /// <summary>
        /// Шаблон для форматирования ссылки в списке источников
        /// {number} - порядковый номер
        /// {title} - название ссылки
        /// {url} - адрес ссылки
        /// Пример: "{number}. {title} — {url}"
        /// </summary>
        public string ReferenceListTemplate { get; set; } = "{number}. {title} — {url}";

        /// <summary>
        /// Правила замены символов в названиях ссылок
        /// Ключ - что заменяем (регулярное выражение), значение - на что заменяем
        /// </summary>
        public Dictionary<string, string> TitleReplacementRules { get; set; } = new Dictionary<string, string>
        {
            { "\\s+", " " },           // Множественные пробелы на один
            { "^\\s+|\\s+$", "" },     // Пробелы в начале и конце
            { "[«»„\"]", "\"" },        // Разные кавычки на обычные
            { "–|—", "-" },            // Длинные тире на обычное
        };
    }

    /// <summary>
    /// Результат обработки текста
    /// </summary>
    public class ProcessingResult
    {
        public string ProcessedText { get; set; }
        public string ReferenceList { get; set; }
    }

    /// <summary>
    /// Уровень важности лога
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Запись в логе
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }

        public string DisplayText => $"[{Timestamp:HH:mm:ss}] [{Level}] {Message}";
    }
}