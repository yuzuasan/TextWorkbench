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
            // グローバル例外ハンドラで使用する通知サービスを先に生成する
            IUserNotificationService notificationService = new MessageBoxNotificationService();
            RegisterExceptionHandlers(notificationService);

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
            IUserConfirmationService confirmationService = new MessageBoxConfirmationService();
            IExitConfirmationService exitConfirmationService = new MessageBoxExitConfirmationService();
            IFileDialogService fileDialogService = new FileDialogService();

            // Workspaceを作成
            var workspace = new Workspace();

            // Workspace連携Presentationサービスを作成
            IDocumentPathRegistry documentPathRegistry =
                new WorkspaceDocumentPathRegistry(workspace);
            IOpenDocumentProvider openDocumentProvider =
                new WorkspaceOpenDocumentProvider(workspace);
            IDocumentActivationService documentActivationService =
                new WorkspaceDocumentActivationService(workspace);

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
                openDocumentProvider,
                documentActivationService,
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

        /// <summary>
        /// 未処理例外のグローバルハンドラを登録します。
        /// </summary>
        /// <param name="notificationService">通知サービス</param>
        private void RegisterExceptionHandlers(IUserNotificationService notificationService)
        {
            // UIスレッドの未処理例外。通知後に確実に終了する。
            // Shutdown()ではClosingイベント経由で終了確認が走るため、
            // 例外ハンドリング中のダイアログ表示を避けるためEnvironment.Exitを使用する。
            DispatcherUnhandledException += (_, args) =>
            {
                args.Handled = true;
                ShowUnexpectedError(notificationService);
                Environment.Exit(1);
            };

            // バックグラウンドスレッド等の未処理例外。
            // プロセス終了はOSが行うため、ここでは通知のみ行う。
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                ShowUnexpectedError(notificationService);
            };

            // Taskの未観測例外。観測済みにしてプロセス昇格を防ぎ、通知する。
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                args.SetObserved();
                ShowUnexpectedError(notificationService);
            };
        }

        /// <summary>
        /// 予期しないエラーをユーザーへ通知します。
        /// 最終防衛線のため、通知自体の失敗は無視します。
        /// </summary>
        /// <param name="notificationService">通知サービス</param>
        private static void ShowUnexpectedError(IUserNotificationService notificationService)
        {
            try
            {
                notificationService.ShowError("予期しないエラーが発生しました。");
            }
            catch
            {
                // 通知に失敗しても二次例外を発生させない
            }
        }
    }
}
