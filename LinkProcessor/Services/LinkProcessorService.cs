using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LinkProcessor.Models;

namespace LinkProcessor.Services
{
    /// <summary>
    /// Сервис для обработки ссылок и формирования результатов
    /// </summary>
    public class LinkProcessorService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static LinkProcessorService()
        {
            // Устанавливаем User-Agent чтобы Google не блокировал запросы
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Получает заголовки для ссылок
        /// </summary>
        public async Task FetchTitlesAsync(List<LinkItem> links)
        {
            
            var tasksWithLinks = links
                .Where(l => string.IsNullOrEmpty(l.Title))
                .Select(async link =>
                {
                    try
                    {
                        var title = await FetchTitleFromMetaAsync(link.Url);
                        if (!string.IsNullOrEmpty(title))
                        {
                            link.Title = title;
                            LogService.Instance.AddLog($"Получен заголовок: {title}");
                        }
                        else
                        {
                            LogService.Instance.AddLog($"Ошибка получения заголовка для {link.Url}", LogLevel.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Instance.AddLog($"Ошибка получения заголовка для {link.Url}: {ex.Message}", LogLevel.Error);
                    }
                })
                .ToList();

            await Task.WhenAll(tasksWithLinks);
        }

        /// <summary>
        /// Получает заголовок ссылки из метаданных страницы
        /// </summary>
        private async Task<string> FetchTitleFromMetaAsync(string url)
        {
            try
            {
                // Формируем запрос
                var searchUrl = url;

                var response = await _httpClient.GetStringAsync(searchUrl);

                // Ищем заголовок в мета-тегах
                var metaMatch = Regex.Match(response, @"<title>([^<]+)</title>");
                if (metaMatch.Success)
                {
                    var title = System.Net.WebUtility.HtmlDecode(metaMatch.Groups[1].Value);
                    return title.Trim();
                }

                return null;
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog($"Ошибка запроса к {url}: {ex.Message}", LogLevel.Warning);
                return null;
            }
        }

        /// <summary>
        /// Извлекает доменное имя из URL для использования в качестве fallback заголовка
        /// </summary>
        private string ExtractDomainFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return url;
            }
        }

        /// <summary>
        /// Обрабатывает текст, заменяя ссылки на номера и формируя список источников
        /// </summary>
        public ProcessingResult ProcessText(string originalText, List<LinkItem> selectedLinks, AppConfig config)
        {
            try
            {
                // Применяем правила замены к заголовкам
                ApplyTitleReplacements(selectedLinks, config.TitleReplacementRules);

                // Создаем словарь уникальных ссылок с их номерами
                var uniqueLinks = selectedLinks
                    .GroupBy(l => l.Url)
                    .Select((g, index) => new TextLink { Url = g.Key, Number = index + 1, Link = g.First() })
                    .ToDictionary(x => x.Url, x => x);

                // Заменяем ссылки в тексте на номера
                var processedText = ReplaceLinksInText(originalText, uniqueLinks, config.LinkReplacementTemplate);

                // Формируем список источников
                var referenceList = FormatReferenceList(uniqueLinks.Values.OrderBy(x => x.Number), config.ReferenceListTemplate);

                LogService.Instance.AddLog("Текст обработан успешно");

                return new ProcessingResult
                {
                    ProcessedText = processedText,
                    ReferenceList = referenceList
                };
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog($"Ошибка при обработке текста: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Обрабатывает текст в обратном режиме, заменяя номера ссылок в тексте на фактические URL адреса из списка
        /// </summary>
        public ProcessingResult ProcessTextReverse(string originalText, List<LinkItem> selectedLinks, AppConfig config)
        {
            try
            {
                // Создаем словарь уникальных ссылок с их номерами
                var uniqueLinks = selectedLinks
                    .GroupBy(l => l.Url)
                    .Select((g, index) => new TextLink { Url = g.Key, Number = index + 1, Link = g.First() })
                    .ToDictionary(x => x.Url, x => x);

                // Заменяем номера в тексте на ссылки
                var processedText = ReplaceNumbersInText(originalText, uniqueLinks, config.LinkReplacementTemplate);

                LogService.Instance.AddLog("Текст обработан успешно");

                return new ProcessingResult
                {
                    ProcessedText = processedText
                };
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog($"Ошибка при обработке текста: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// Применяет правила замены символов к заголовкам ссылок
        /// </summary>
        private void ApplyTitleReplacements(List<LinkItem> links, Dictionary<string, string> rules)
        {
            foreach (var link in links)
            {
                if (string.IsNullOrEmpty(link.Title))
                    continue;

                var title = link.Title;

                foreach (var rule in rules)
                {
                    try
                    {
                        title = Regex.Replace(title, rule.Key, rule.Value);
                    }
                    catch (Exception ex)
                    {
                        LogService.Instance.AddLog($"Ошибка применения правила '{rule.Key}': {ex.Message}", LogLevel.Warning);
                    }
                }

                link.Title = title;
            }
        }

        /// <summary>
        /// Заменяет ссылки в тексте на их порядковые номера
        /// </summary>
        private string ReplaceLinksInText(string text,
            Dictionary<string, TextLink> uniqueLinks, string template)
        {
            var result = text;
            foreach (var link in uniqueLinks)
            {
                var linkInfo = link.Value;
                var replacement = template.Replace("{number}", linkInfo.Number.ToString());

                // Заменяем оригинальную ссылку на номер
                result = result.Replace(linkInfo.Link.OriginalLink, replacement);
            }

            return result;
        }

        /// <summary>
        /// Заменяет номера в тексте на URL адреса
        /// </summary>
        private string ReplaceNumbersInText(string text,
            Dictionary<string, TextLink> uniqueLinks, string template)
        {
            var result = text;
            foreach (var link in uniqueLinks)
            {
                var linkInfo = link.Value;
                var find = template.Replace("{number}", linkInfo.Number.ToString());

                // Заменяем номер на адрес
                result = result.Replace(find, linkInfo.Url);
            }

            return result;
        }

        /// <summary>
        /// Форматирует список источников
        /// </summary>
        private string FormatReferenceList(IEnumerable<TextLink> links, string template)
        {
            var sb = new StringBuilder();

            foreach (var linkInfo in links)
            {
                var line = template
                    .Replace("{number}", linkInfo.Number.ToString())
                    .Replace("{title}", linkInfo.Link.Title)
                    .Replace("{url}", linkInfo.Link.Url);

                sb.AppendLine(line);
            }

            return sb.ToString();
        }
    }
}