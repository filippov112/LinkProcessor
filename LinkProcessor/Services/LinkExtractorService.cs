using System.Text.RegularExpressions;
using LinkProcessor.Models;

namespace LinkProcessor.Services
{
    /// <summary>
    /// Сервис для извлечения ссылок из текста
    /// </summary>
    public class LinkExtractorService
    {
        /// <summary>
        /// Извлекает все ссылки из текста
        /// </summary>
        public List<LinkItem> ExtractLinks(string text, AppConfig config)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<LinkItem>();

            var links = new List<LinkItem>();
            var processedPositions = new HashSet<int>();
            try
            {
                foreach(var expression in config.RegularExpressions)
                    // Извлекаем ссылки
                    ExtractLinks(text, links, processedPositions, expression);

                // Удаляем дубликаты по URL и сортируем по позиции
                links = links
                    .GroupBy(l => l.Url)
                    .Select(g => g.First())
                    .OrderBy(l => l.Position)
                    .ToList();

                LogService.Instance.AddLog($"Извлечено уникальных ссылок: {links.Count}");
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog($"Ошибка при извлечении ссылок: {ex.Message}", LogLevel.Error);
            }

            return links;
        }

        /// <summary>
        /// Извлекает ссылки
        /// </summary>
        private void ExtractLinks(string text, List<LinkItem> links, HashSet<int> processedPositions, string expression)
        {
            Regex rgx = new(expression, RegexOptions.Compiled | RegexOptions.Multiline);

            var matches = rgx.Matches(text);

            foreach (Match match in matches)
            {
                try
                {
                    // Пропускаем, если эта позиция уже обработана (была частью markdown ссылки)
                    if (processedPositions.Contains(match.Index))
                        continue;

                    var url = match.Groups[1].Value.Trim();

                    // Проверяем, что это действительно URL
                    if (!IsValidUrl(url))
                        continue;

                    links.Add(new LinkItem
                    {
                        OriginalLink = match.Value,
                        Url = url,
                        Position = match.Index
                    });

                    // Помечаем все позиции этого совпадения как обработанные
                    for (int i = match.Index; i < match.Index + match.Length; i++)
                    {
                        processedPositions.Add(i);
                    }
                }
                catch (Exception ex)
                {
                    LogService.Instance.AddLog($"Ошибка при обработке ссылки: {ex.Message}", LogLevel.Warning);
                }
            }
        }

        /// <summary>
        /// Проверяет, является ли строка валидным URL
        /// </summary>
        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}