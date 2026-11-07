using System.Windows;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Services;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Infrastructure.Diff;
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

            // DiffエンジンAdapterを作成
            IDiffEngineAdapter diffEngineAdapter = new DiffPlexAdapter();

            // Applicationサービスを作成
            ISearchService searchService = new SearchService();
            IReplaceService replaceService = new ReplaceService();
            ITextInfoService textInfoService = new TextInfoService(encodingService, lineEndingService);
            IDiffService diffService = new DiffService(diffEngineAdapter);

            // Presentationサービスを作成
            IUserNotificationService notificationService = new MessageBoxNotificationService();
            IUserConfirmationService confirmationService = new MessageBoxConfirmationService();
            IExitConfirmationService exitConfirmationService = new MessageBoxExitConfirmationService();
            IFileDialogService fileDialogService = new FileDialogService();

            // Workspaceを作成
            var workspace = new Workspace();

            // Workspace連携Presentationサービスを作成
            IDocumentPathRegistry documentPathRegistry =
                new WorkspaceDocumentPathRegistry(workspace);

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
                documentPathRegistry,
                fileService,
                encodingService,
                lineEndingService);
            var closeDocumentUseCase = new CloseDocumentUseCase(
                confirmationService,
                saveDocumentUseCase,
                saveDocumentAsUseCase);
            var exitApplicationUseCase = new ExitApplicationUseCase(
                exitConfirmationService,
                notificationService,
                saveDocumentUseCase,
                saveDocumentAsUseCase);
            var searchTextUseCase = new SearchTextUseCase(searchService);
            var replaceTextUseCase = new ReplaceTextUseCase(replaceService);
            var getTextInfoUseCase = new GetTextInfoUseCase(textInfoService);

            // IDocumentContentProviderを作成
            IDocumentContentProvider documentContentProvider = new DocumentContentProvider(workspace);

            var compareDocumentsUseCase =
                new CompareDocumentsUseCase(
                    fileService,
                    encodingService,
                    lineEndingService,
                    diffService,
                    documentContentProvider);

            // ViewModelを作成
            var viewModel = new MainWindowViewModel(
                notificationService,
                fileDialogService,
                newDocumentUseCase,
                openDocumentUseCase,
                saveDocumentUseCase,
                saveDocumentAsUseCase,
                closeDocumentUseCase,
                exitApplicationUseCase,
                getTextInfoUseCase,
                searchTextUseCase,
                replaceTextUseCase,
                compareDocumentsUseCase,
                workspace);

            // MainWindowを作成
            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            mainWindow.Show();
        }
    }
}
