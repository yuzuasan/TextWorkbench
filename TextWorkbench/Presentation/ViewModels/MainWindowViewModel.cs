using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.UseCases;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// MainWindowのViewModelです。
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IUserNotificationService _notificationService;
    private readonly NewDocumentUseCase _newDocumentUseCase;
    private readonly OpenDocumentUseCase _openDocumentUseCase;
    private readonly SaveDocumentUseCase _saveDocumentUseCase;
    private readonly SaveDocumentAsUseCase _saveDocumentAsUseCase;
    private readonly CloseDocumentUseCase _closeDocumentUseCase;

    /// <summary>
    /// Workspaceを取得します。
    /// </summary>
    public Workspace Workspace { get; }

    /// <summary>
    /// 処理中かどうかを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private bool _isProcessing;

    /// <summary>
    /// 新規作成コマンドを取得します。
    /// </summary>
    public IRelayCommand NewCommand { get; }

    /// <summary>
    /// 開くコマンドを取得します。
    /// </summary>
    public IRelayCommand OpenCommand { get; }

    /// <summary>
    /// 保存コマンドを取得します。
    /// </summary>
    public IRelayCommand SaveCommand { get; }

    /// <summary>
    /// 名前を付けて保存コマンドを取得します。
    /// </summary>
    public IRelayCommand SaveAsCommand { get; }

    /// <summary>
    /// 閉じるコマンドを取得します。
    /// </summary>
    public IRelayCommand CloseCommand { get; }

    /// <summary>
    /// 終了コマンドを取得します。
    /// </summary>
    public IRelayCommand ExitCommand { get; }

    /// <summary>
    /// 検索コマンドを取得します。
    /// </summary>
    public IRelayCommand SearchCommand { get; }

    /// <summary>
    /// 置換コマンドを取得します。
    /// </summary>
    public IRelayCommand ReplaceCommand { get; }

    /// <summary>
    /// 比較コマンドを取得します。
    /// </summary>
    public IRelayCommand CompareCommand { get; }

    /// <summary>
    /// <see cref="MainWindowViewModel"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="notificationService">通知サービス</param>
    /// <param name="newDocumentUseCase">新規Document作成UseCase</param>
    /// <param name="openDocumentUseCase">ファイルを開くUseCase</param>
    /// <param name="saveDocumentUseCase">Document保存UseCase</param>
    /// <param name="saveDocumentAsUseCase">名前を付けて保存UseCase</param>
    /// <param name="closeDocumentUseCase">Documentを閉じるUseCase</param>
    public MainWindowViewModel(
        IUserNotificationService notificationService,
        NewDocumentUseCase newDocumentUseCase,
        OpenDocumentUseCase openDocumentUseCase,
        SaveDocumentUseCase saveDocumentUseCase,
        SaveDocumentAsUseCase saveDocumentAsUseCase,
        CloseDocumentUseCase closeDocumentUseCase)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _newDocumentUseCase = newDocumentUseCase ?? throw new ArgumentNullException(nameof(newDocumentUseCase));
        _openDocumentUseCase = openDocumentUseCase ?? throw new ArgumentNullException(nameof(openDocumentUseCase));
        _saveDocumentUseCase = saveDocumentUseCase ?? throw new ArgumentNullException(nameof(saveDocumentUseCase));
        _saveDocumentAsUseCase = saveDocumentAsUseCase ?? throw new ArgumentNullException(nameof(saveDocumentAsUseCase));
        _closeDocumentUseCase = closeDocumentUseCase ?? throw new ArgumentNullException(nameof(closeDocumentUseCase));

        Workspace = new Workspace();

        NewCommand = new RelayCommand(ExecuteNew, CanExecuteNew);
        OpenCommand = new RelayCommand(ExecuteOpen, CanExecuteOpen);
        SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        SaveAsCommand = new RelayCommand(ExecuteSaveAs, CanExecuteSaveAs);
        CloseCommand = new RelayCommand(ExecuteClose, CanExecuteClose);
        ExitCommand = new RelayCommand(ExecuteExit, CanExecuteExit);
        SearchCommand = new RelayCommand(ExecuteSearch, CanExecuteSearch);
        ReplaceCommand = new RelayCommand(ExecuteReplace, CanExecuteReplace);
        CompareCommand = new RelayCommand(ExecuteCompare, CanExecuteCompare);
    }

    private bool CanExecuteNew() => !IsProcessing;

    private void ExecuteNew()
    {
        try
        {
            var document = _newDocumentUseCase.Execute();
            var tab = new DocumentTabViewModel(document);
            Workspace.AddTab(tab);
            RefreshCommandStates();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"新規作成に失敗しました: {ex.Message}");
        }
    }

    private bool CanExecuteOpen() => !IsProcessing;

    private void ExecuteOpen()
    {
        try
        {
            var document = _openDocumentUseCase.Execute();
            if (document == null)
            {
                return;
            }

            // 同一ファイルの重複防止
            var existingTab = Workspace.FindDocumentByFilePath(document.FilePath!);
            if (existingTab != null)
            {
                Workspace.SetActiveTab(existingTab);
                RefreshCommandStates();
                return;
            }

            var tab = new DocumentTabViewModel(document);
            Workspace.AddTab(tab);
            RefreshCommandStates();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"ファイルを開くことに失敗しました: {ex.Message}");
        }
    }

    private bool CanExecuteSave()
    {
        if (IsProcessing || Workspace.ActiveTab is not DocumentTabViewModel documentTab)
        {
            return false;
        }

        return !documentTab.Document.IsReadOnly
            && !documentTab.Document.IsLoading
            && !documentTab.Document.IsSaving;
    }

    private void ExecuteSave()
    {
        if (Workspace.ActiveTab is not DocumentTabViewModel documentTab)
        {
            return;
        }

        var document = documentTab.Document;

        // 新規Document（FilePath == null）の場合はSave Asに委譲
        if (document.FilePath == null)
        {
            ExecuteSaveAs();
            return;
        }

        try
        {
            _saveDocumentUseCase.Execute(document);
            documentTab.OnDocumentSaved();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"保存に失敗しました: {ex.Message}");
        }
        finally
        {
            RefreshCommandStates();
        }
    }

    private bool CanExecuteSaveAs()
    {
        if (IsProcessing || Workspace.ActiveTab is not DocumentTabViewModel documentTab)
        {
            return false;
        }

        return !documentTab.Document.IsLoading
            && !documentTab.Document.IsSaving;
    }

    private void ExecuteSaveAs()
    {
        if (Workspace.ActiveTab is not DocumentTabViewModel documentTab)
        {
            return;
        }

        try
        {
            var succeeded = _saveDocumentAsUseCase.Execute(
                documentTab.Document,
                Workspace);

            if (succeeded)
            {
                documentTab.OnDocumentSavedAs();
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"名前を付けて保存に失敗しました: {ex.Message}");
        }
        finally
        {
            RefreshCommandStates();
        }
    }

    private bool CanExecuteClose()
    {
        // 処理中、または閉じる対象のDocumentTabがない場合は実行できない。
        if (IsProcessing || Workspace.ActiveTab is not DocumentTabViewModel documentTab)
        {
            return false;
        }

        // 読み込み中・保存中のDocumentは閉じない。
        return !documentTab.Document.IsLoading
            && !documentTab.Document.IsSaving;
    }

    /// <summary>
    /// アクティブなDocumentTabを閉じます。
    /// </summary>
    private void ExecuteClose()
    {
        if (Workspace.ActiveTab is not DocumentTabViewModel documentTab)
        {
            return;
        }

        try
        {
            // 保存確認と保存処理を行い、閉じてよい場合のみタブを削除する。
            if (!_closeDocumentUseCase.Execute(documentTab, Workspace))
            {
                return;
            }

            // タブの追加・削除はWorkspaceを通して行う。
            Workspace.RemoveTab(documentTab);
        }
        catch (Exception ex)
        {
            // 保存失敗などの場合はタブを残してエラーを通知する。
            _notificationService.ShowError($"閉じることに失敗しました: {ex.Message}");
        }
        finally
        {
            // タブの選択状態が変わるため、各コマンドの実行可否を更新する。
            RefreshCommandStates();
        }
    }

    private bool CanExecuteExit() => !IsProcessing;
    private void ExecuteExit() => _notificationService.ShowInfo("終了（仮実装）");

    private bool CanExecuteSearch() => !IsProcessing;
    private void ExecuteSearch() => _notificationService.ShowInfo("検索（仮実装）");

    private bool CanExecuteReplace() => !IsProcessing;
    private void ExecuteReplace() => _notificationService.ShowInfo("置換（仮実装）");

    private bool CanExecuteCompare() => !IsProcessing;
    private void ExecuteCompare() => _notificationService.ShowInfo("比較（仮実装）");

    /// <summary>
    /// 全てのコマンドのCanExecute状態を更新します。
    /// </summary>
    public void RefreshCommandStates()
    {
        NewCommand.NotifyCanExecuteChanged();
        OpenCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        SaveAsCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
        ExitCommand.NotifyCanExecuteChanged();
        SearchCommand.NotifyCanExecuteChanged();
        ReplaceCommand.NotifyCanExecuteChanged();
        CompareCommand.NotifyCanExecuteChanged();
    }
}
