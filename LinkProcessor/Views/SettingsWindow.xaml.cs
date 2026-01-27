using System;
using System.Linq;
using System.Windows;
using LinkProcessor.Models;
using LinkProcessor.Services;

namespace LinkProcessor.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigService _configService;
        private AppConfig _config = new();

        public SettingsWindow(ConfigService configService)
        {
            InitializeComponent();
            _configService = configService;
            try
            {
                _config = _configService.LoadConfig();
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog($"Ошибка при загрузке настроек: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Ошибка при загрузке настроек: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            ApplyLoadSettings();
        }

        /// <summary>
        /// Загружает текущие настройки в элементы управления
        /// </summary>
        private void ApplyLoadSettings()
        {
            RegularExpressionsTextBox.Text = string.Join(Environment.NewLine, _config.RegularExpressions);
            LinkTemplateTextBox.Text = _config.LinkReplacementTemplate;
            ReferenceTemplateTextBox.Text = _config.ReferenceListTemplate;
            ReplacementRulesTextBox.Text = string.Join(Environment.NewLine, _config.TitleReplacementRules.Select(r => $"{r.Key} → {r.Value}"));
        }

        /// <summary>
        /// Сбрасывает настройки к заводским
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var new_config = new AppConfig
            {
                RecentFiles = _config.RecentFiles
            };
            _config = new_config;
            ApplyLoadSettings();
        }

        /// <summary>
        /// Сохраняет настройки
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация шаблонов
                if (string.IsNullOrWhiteSpace(LinkTemplateTextBox.Text))
                {
                    MessageBox.Show("Шаблон замены ссылок не может быть пустым", "Ошибка валидации",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ReferenceTemplateTextBox.Text))
                {
                    MessageBox.Show("Шаблон списка источников не может быть пустым", "Ошибка валидации",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Сохраняем настройки
                _config.LinkReplacementTemplate = LinkTemplateTextBox.Text.Trim();
                _config.ReferenceListTemplate = ReferenceTemplateTextBox.Text.Trim();

                // Парсим правила замены
                _config.TitleReplacementRules.Clear();
                if (!string.IsNullOrWhiteSpace(ReplacementRulesTextBox.Text))
                {
                    var lines = ReplacementRulesTextBox.Text.Split(new[] { Environment.NewLine },
                        StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        var parts = line.Split(new[] { "→", "->" }, StringSplitOptions.None);

                        if (parts.Length == 2)
                        {
                            var pattern = parts[0].Trim();
                            var replacement = parts[1].Trim();

                            if (!string.IsNullOrEmpty(pattern))
                            {
                                // Проверяем валидность регулярного выражения
                                try
                                {
                                    System.Text.RegularExpressions.Regex.IsMatch("test", pattern);
                                    _config.TitleReplacementRules[pattern] = replacement;
                                }
                                catch (ArgumentException)
                                {
                                    MessageBox.Show($"Некорректное регулярное выражение: {pattern}",
                                        "Ошибка валидации",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }
                        }
                    }
                }

                // Парсим регулярные выражения поиска ссылок
                _config.RegularExpressions.Clear();
                if (!string.IsNullOrWhiteSpace(RegularExpressionsTextBox.Text))
                {
                    var lines = RegularExpressionsTextBox.Text.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        var pattern = line.Trim();
                        if (!string.IsNullOrEmpty(pattern))
                        {
                            // Проверяем валидность регулярного выражения
                            try
                            {
                                System.Text.RegularExpressions.Regex.IsMatch("test", pattern);
                                _config.RegularExpressions.Add(pattern);
                            }
                            catch (ArgumentException)
                            {
                                MessageBox.Show($"Некорректное регулярное выражение: {pattern}",
                                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                        
                    }
                }

                _configService.SaveConfig(_config);

                LogService.Instance.AddLog("Настройки успешно сохранены");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                LogService.Instance.AddLog($"Ошибка при сохранении настроек: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}