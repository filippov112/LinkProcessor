using System;
using System.IO;
using System.Text.Json;
using LinkProcessor.Models;

namespace LinkProcessor.Services
{
    /// <summary>
    /// Сервис для работы с конфигурацией приложения
    /// </summary>
    public class ConfigService
    {
        private static readonly string ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LinkProcessor");

        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

        /// <summary>
        /// Загрузка конфигурации из файла
        /// </summary>
        public AppConfig LoadConfig()
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                if (!File.Exists(ConfigFilePath))
                {
                    // Создаем конфигурацию по умолчанию
                    var defaultConfig = new AppConfig();
                    SaveConfig(defaultConfig);
                    return defaultConfig;
                }

                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);

                // Проверка на null и создание объектов если они отсутствуют
                config ??= new AppConfig();
                config.RecentFiles ??= new System.Collections.Generic.List<string>();
                config.TitleReplacementRules ??= new System.Collections.Generic.Dictionary<string, string>();

                return config;
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog($"Ошибка при загрузке конфигурации: {ex.Message}", LogLevel.Warning);
                return new AppConfig();
            }
        }

        /// <summary>
        /// Сохранение конфигурации в файл
        /// </summary>
        public void SaveConfig(AppConfig config)
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog($"Ошибка при сохранении конфигурации: {ex.Message}", LogLevel.Error);
                throw;
            }
        }
    }
}