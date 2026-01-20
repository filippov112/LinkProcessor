using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using LinkProcessor.Services;

namespace LinkProcessor.Views
{
    public partial class LogsWindow : Window
    {
        private readonly LogService _logService;

        public LogsWindow()
        {
            InitializeComponent();
            _logService = LogService.Instance;
            DataContext = _logService;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите очистить журнал событий?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _logService.ClearLogs();
                _logService.AddLog("Журнал событий очищен");
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                    FileName = $"LinkProcessor_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    Title = "Экспорт журнала событий"
                };

                if (dialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("=".PadRight(80, '='));
                    sb.AppendLine($"Журнал событий LinkProcessor");
                    sb.AppendLine($"Дата экспорта: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                    sb.AppendLine("=".PadRight(80, '='));
                    sb.AppendLine();

                    foreach (var log in _logService.Logs)
                    {
                        sb.AppendLine(log.DisplayText);
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);

                    _logService.AddLog($"Журнал экспортирован в файл: {dialog.FileName}");

                    MessageBox.Show(
                        "Журнал событий успешно экспортирован",
                        "Экспорт завершен",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logService.AddLog($"Ошибка при экспорте журнала: {ex.Message}", Models.LogLevel.Error);
                MessageBox.Show(
                    $"Ошибка при экспорте журнала: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}