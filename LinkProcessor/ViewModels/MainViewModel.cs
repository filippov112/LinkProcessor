using LinkProcessor.Models;
using LinkProcessor.Services;
using LinkProcessor.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace LinkProcessor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ConfigService _configService;
        private readonly LinkExtractorService _linkExtractor;
        private readonly LinkProcessorService _linkProcessor;
        private readonly LogService _logService;

        private string _currentFilePath;
        private string _originalText;
        private string _statusText;
        private bool _isProcessing;
        private bool _isReverseMode;
        private string _processedText;
        private string _referenceList;

        public MainViewModel()
        {
            _configService = new ConfigService();
            _linkExtractor = new LinkExtractorService();
            _linkProcessor = new LinkProcessorService();
            _logService = LogService.Instance;

            FoundLinks = new ObservableCollection<LinkItem>();
            RecentFiles = new ObservableCollection<string>();

            InitializeCommands();
            LoadConfiguration();

            StatusText = "Готов к работе";
            _logService.AddLog("Приложение запущено");
        }

        #region Properties

        public ObservableCollection<LinkItem> FoundLinks { get; }
        public ObservableCollection<string> RecentFiles { get; }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public string ProcessedText
        {
            get => _processedText;
            set => SetProperty(ref _processedText, value);
        }

        public string ReferenceList
        {
            get => _referenceList;
            set => SetProperty(ref _referenceList, value);
        }

        public bool IsReverseMode
        {
            get => _isReverseMode;
            set => SetProperty(ref _isReverseMode, value);
        }

        #endregion

        #region Commands

        public ICommand OpenFileCommand { get; private set; }
        public ICommand OpenRecentFileCommand { get; private set; }
        public ICommand OpenSettingsCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }
        public ICommand SelectAllLinksCommand { get; private set; }
        public ICommand DeselectAllLinksCommand { get; private set; }
        public ICommand ProcessLinksCommand { get; private set; }
        public ICommand CopyProcessedTextCommand { get; private set; }
        public ICommand CopyReferenceListCommand { get; private set; }
        public ICommand ShowLogsCommand { get; private set; }

        private void InitializeCommands()
        {
            OpenFileCommand = new RelayCommand(ExecuteOpenFile);
            OpenRecentFileCommand = new RelayCommand<string>(ExecuteOpenRecentFile);
            OpenSettingsCommand = new RelayCommand(ExecuteOpenSettings);
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            AboutCommand = new RelayCommand(ExecuteAbout);
            SelectAllLinksCommand = new RelayCommand(ExecuteSelectAllLinks);
            DeselectAllLinksCommand = new RelayCommand(ExecuteDeselectAllLinks);
            ProcessLinksCommand = new RelayCommand(ExecuteProcessLinks, CanProcessLinks);
            CopyProcessedTextCommand = new RelayCommand(ExecuteCopyProcessedText, _ => !string.IsNullOrEmpty(ProcessedText));
            CopyReferenceListCommand = new RelayCommand(ExecuteCopyReferenceList, _ => !string.IsNullOrEmpty(ReferenceList));
            ShowLogsCommand = new RelayCommand(ExecuteShowLogs);
        }

        #endregion

        #region Command Implementations

        private void ExecuteOpenFile(object parameter)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Текстовые файлы (*.txt;*.md)|*.txt;*.md|Все файлы (*.*)|*.*",
                    Title = "Выберите текстовый файл"
                };

                if (dialog.ShowDialog() == true)
                {
                    LoadFile(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _logService.AddLog($"Ошибка при открытии файла: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteOpenRecentFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                LoadFile(filePath);
            }
            else
            {
                _logService.AddLog($"Файл не найден: {filePath}", LogLevel.Warning);
                MessageBox.Show("Файл не найден или был удален", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                var config = _configService.LoadConfig();
                config.RecentFiles.Remove(filePath);
                _configService.SaveConfig(config);
                RecentFiles.Remove(filePath);
            }
        }

        private void ExecuteOpenSettings(object parameter)
        {
            var settingsWindow = new SettingsWindow(_configService);
            if (settingsWindow.ShowDialog() == true)
            {
                _logService.AddLog("Настройки обновлены");
                StatusText = "Настройки сохранены";
            }
        }

        private void ExecuteAbout(object parameter)
        {
            MessageBox.Show(
                "LinkProcessor v1.0\n\n" +
                "Приложение для обработки ссылок в текстовых документах.\n\n" +
                "Возможности:\n" +
                "• Извлечение ссылок из текста\n" +
                "• Автоматическое получение названий веб-ресурсов\n" +
                "• Формирование списка источников\n" +
                "• Замена ссылок на номера в тексте\n" +
                "• Обратный режим работы (замена номеров на ссылки)\n\n" +
                "© 2025",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ExecuteSelectAllLinks(object parameter)
        {
            foreach (var link in FoundLinks)
                link.IsSelected = true;

            _logService.AddLog($"Выбраны все ссылки ({FoundLinks.Count})");
        }

        private void ExecuteDeselectAllLinks(object parameter)
        {
            foreach (var link in FoundLinks)
                link.IsSelected = false;

            _logService.AddLog("Снято выделение со всех ссылок");
        }

        private async void ExecuteProcessLinks(object parameter)
        {
            try
            {
                IsProcessing = true;
                StatusText = "Обработка ссылок...";
                _logService.AddLog("Начало обработки ссылок");

                var selectedLinks = FoundLinks.Where(l => l.IsSelected).ToList();

                if (selectedLinks.Count == 0)
                {
                    MessageBox.Show("Не выбрано ни одной ссылки для обработки", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var config = _configService.LoadConfig();

                ProcessingResult result = new();
                if (!_isReverseMode)
                {
                    // Получение заголовков
                    StatusText = $"Получение заголовков для {selectedLinks.Count} ссылок...";
                    await _linkProcessor.FetchTitlesAsync(selectedLinks);

                    // Обработка текста и формирование списка источников
                    StatusText = "Формирование результатов...";
                    result = _linkProcessor.ProcessText(_originalText, selectedLinks, config);
                }
                else
                {
                    // Обработка текста в обратном режиме (номера -> ссылки)
                    StatusText = "Формирование результатов...";
                    result = _linkProcessor.ProcessTextReverse(_originalText, selectedLinks, config);
                }


                ProcessedText = result.ProcessedText;
                ReferenceList = result.ReferenceList;

                StatusText = $"Обработка завершена. Обработано ссылок: {selectedLinks.Count}";
                _logService.AddLog($"Обработка завершена успешно. Обработано ссылок: {selectedLinks.Count}");
            }
            catch (Exception ex)
            {
                _logService.AddLog($"Ошибка при обработке ссылок: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Ошибка при обработке: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "Ошибка при обработке";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanProcessLinks(object parameter)
        {
            return FoundLinks.Any(l => l.IsSelected) && !IsProcessing;
        }

        private void ExecuteCopyProcessedText(object parameter)
        {
            try
            {
                Clipboard.SetText(ProcessedText);
                StatusText = "Обработанный текст скопирован в буфер обмена";
                _logService.AddLog("Обработанный текст скопирован в буфер обмена");
            }
            catch (Exception ex)
            {
                _logService.AddLog($"Ошибка при копировании текста: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Ошибка при копировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCopyReferenceList(object parameter)
        {
            try
            {
                Clipboard.SetText(ReferenceList);
                StatusText = "Список источников скопирован в буфер обмена";
                _logService.AddLog("Список источников скопирован в буфер обмена");
            }
            catch (Exception ex)
            {
                _logService.AddLog($"Ошибка при копировании списка: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Ошибка при копировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteShowLogs(object parameter)
        {
            var logsWindow = new LogsWindow();
            logsWindow.Show();
        }

        #endregion

        #region Private Methods

        private void LoadFile(string filePath)
        {
            try
            {
                StatusText = "Загрузка файла...";
                _logService.AddLog($"Загрузка файла: {filePath}");

                _currentFilePath = filePath;
                _originalText = File.ReadAllText(filePath);

                // Извлечение ссылок
                var links = _linkExtractor.ExtractLinks(_originalText);

                FoundLinks.Clear();
                foreach (var link in links)
                {
                    FoundLinks.Add(link);
                }

                // Очистка результатов
                ProcessedText = string.Empty;
                ReferenceList = string.Empty;

                // Обновление списка последних файлов
                UpdateRecentFiles(filePath);

                StatusText = $"Загружен файл: {Path.GetFileName(filePath)}. Найдено ссылок: {links.Count}";
                _logService.AddLog($"Файл загружен. Найдено ссылок: {links.Count}");
            }
            catch (Exception ex)
            {
                _logService.AddLog($"Ошибка при загрузке файла: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "Ошибка при загрузке файла";
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                var config = _configService.LoadConfig();

                RecentFiles.Clear();
                foreach (var file in config.RecentFiles)
                {
                    RecentFiles.Add(file);
                }

                _logService.AddLog("Конфигурация загружена");
            }
            catch (Exception ex)
            {
                _logService.AddLog($"Ошибка при загрузке конфигурации: {ex.Message}", LogLevel.Warning);
            }
        }

        private void UpdateRecentFiles(string filePath)
        {
            try
            {
                var config = _configService.LoadConfig();

                // Удаляем файл из списка, если он уже там есть
                config.RecentFiles.Remove(filePath);

                // Добавляем в начало списка
                config.RecentFiles.Insert(0, filePath);

                // Ограничиваем количество последних файлов
                while (config.RecentFiles.Count > 10)
                {
                    config.RecentFiles.RemoveAt(config.RecentFiles.Count - 1);
                }

                _configService.SaveConfig(config);

                // Обновляем UI
                RecentFiles.Clear();
                foreach (var file in config.RecentFiles)
                {
                    RecentFiles.Add(file);
                }
            }
            catch (Exception ex)
            {
                _logService.AddLog($"Ошибка при обновлении списка последних файлов: {ex.Message}", LogLevel.Warning);
            }
        }

        #endregion
    }
}