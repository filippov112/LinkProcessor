using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LinkProcessor.Models;

namespace LinkProcessor.Services
{
    /// <summary>
    /// Сервис для извлечения ссылок из текста
    /// </summary>
    public class LinkExtractorService
    {
        // Регулярное выражение для поиска ссылок в markdown формате: [текст](url)
        // Группа 1: текст ссылки
        // Группа 2: URL адрес
        private static readonly Regex MarkdownLinkRegex = new Regex(
            @"\[([^\]]+)\]\(([^\)]+)\)",
            RegexOptions.Compiled | RegexOptions.Multiline);

        // Регулярное выражение для поиска чистых URL
        // Ищет http:// или https:// и далее все до пробела или конца строки
        private static readonly Regex UrlRegex = new Regex(
            @"https?://[^\s\)]+",
            RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Извлекает все ссылки из текста
        /// </summary>
        public List<LinkItem> ExtractLinks(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<LinkItem>();

            var links = new List<LinkItem>();
            var processedPositions = new HashSet<int>();

            try
            {
                // Сначала извлекаем markdown ссылки
                ExtractMarkdownLinks(text, links, processedPositions);

                // Затем извлекаем обычные URL, пропуская уже обработанные позиции
                ExtractPlainUrls(text, links, processedPositions);

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
        /// Извлекает ссылки в формате markdown
        /// </summary>
        private void ExtractMarkdownLinks(string text, List<LinkItem> links, HashSet<int> processedPositions)
        {
            var matches = MarkdownLinkRegex.Matches(text);

            foreach (Match match in matches)
            {
                try
                {
                    var url = match.Groups[2].Value.Trim();

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
                    LogService.Instance.AddLog($"Ошибка при обработке markdown ссылки: {ex.Message}", LogLevel.Warning);
                }
            }
        }

        /// <summary>
        /// Извлекает обычные URL из текста
        /// </summary>
        private void ExtractPlainUrls(string text, List<LinkItem> links, HashSet<int> processedPositions)
        {
            var matches = UrlRegex.Matches(text);

            foreach (Match match in matches)
            {
                try
                {
                    // Пропускаем, если эта позиция уже обработана (была частью markdown ссылки)
                    if (processedPositions.Contains(match.Index))
                        continue;

                    var url = match.Value.Trim();

                    // Проверяем валидность URL
                    if (!IsValidUrl(url))
                        continue;

                    links.Add(new LinkItem
                    {
                        OriginalLink = url,
                        Url = url,
                        Position = match.Index
                    });
                }
                catch (Exception ex)
                {
                    LogService.Instance.AddLog($"Ошибка при обработке URL: {ex.Message}", LogLevel.Warning);
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