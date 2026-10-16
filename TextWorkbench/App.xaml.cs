using System.Windows;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Infrastructure.Encoding;
using TextWorkbench.Infrastructure.File;
using TextWorkbench.Infrastructure.LineEnding;
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
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // Infrastructureサービスを作成
            IFileService fileService = new FileService();
            IEncodingService encodingService = new EncodingService();
            ILineEndingService lineEndingService = new LineEndingService();

            // Presentationサービスを作成
            IUserNotificationService notificationService = new MessageBoxNotificationService();
            IFileDialogService fileDialogService = new FileDialogService();

            // UseCaseを作成
            var newDocumentUseCase = new NewDocumentUseCase();
            var openDocumentUseCase = new OpenDocumentUseCase(
                fileDialogService,
                fileService,
                encodingService,
                lineEndingService);
            var saveDocumentUseCase = new SaveDocumentUseCase(
                fileService,
                encodingService,
                lineEndingService);
            var saveDocumentAsUseCase = new SaveDocumentAsUseCase(
                fileDialogService,
                fileService,
                encodingService,
                lineEndingService);

            // ViewModelを作成
            var viewModel = new MainWindowViewModel(
                notificationService,
                newDocumentUseCase,
                openDocumentUseCase,
                saveDocumentUseCase,
                saveDocumentAsUseCase);

            // MainWindowを作成
            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            mainWindow.Show();
        }
    }
}
