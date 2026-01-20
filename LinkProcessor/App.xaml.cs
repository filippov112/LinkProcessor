using System.Windows;

namespace LinkProcessor
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Глобальный обработчик необработанных исключений
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Services.LogService.Instance.AddLog(
                $"Необработанное исключение: {e.Exception.Message}",
                Models.LogLevel.Error);

            MessageBox.Show(
                $"Произошла критическая ошибка:\n\n{e.Exception.Message}\n\nПодробности в журнале событий.",
                "Критическая ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}