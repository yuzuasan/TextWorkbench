using System.Text;
using System.Windows;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Presentation.Services;
using TextWorkbench.Presentation.ViewModels;
using TextWorkbench.Presentation.Views;

namespace TextWorkbench
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            // Shift-JIS対応のためCodePagesEncodingProviderを登録
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // サービスを作成
            IUserNotificationService notificationService = new MessageBoxNotificationService();

            // ViewModelを作成
            var viewModel = new MainWindowViewModel(notificationService);

            // MainWindowを作成
            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            mainWindow.Show();
        }
    }
}
