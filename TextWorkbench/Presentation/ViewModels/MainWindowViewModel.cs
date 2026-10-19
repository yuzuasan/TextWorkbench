using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Domain.Enums;

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
    private readonly ExitApplicationUseCase _exitApplicationUseCase;

    /// <summary>
    /// StatusBar更新の監視対象となるDocumentTabを取得または設定します。
    /// </summary>
    private DocumentTabViewModel? _observedStatusTab;

    /// <summary>
    /// Workspaceを取得します。
    /// </summary>
    public Workspace Workspace { get; }

    /// <summary>
    /// ウィンドウの終了要求を通知します。
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// 処理中かどうかを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private bool _isProcessing;

    /// <summary>
    /// StatusBarのEncoding表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _statusEncoding = string.Empty;

    /// <summary>
    /// StatusBarのLineEnding表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _statusLineEnding = string.Empty;

    /// <summary>
    /// StatusBarのCaret位置表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _statusCaretPosition = string.Empty;

    /// <summary>
    /// StatusBarの変更状態表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _statusModificationState = string.Empty;

    /// <summary>
    /// StatusBarの読み取り専用状態表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _statusReadOnlyState = string.Empty;

    /// <summary>
    /// StatusBarの処理中状態表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _statusProcessingState = string.Empty;

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
    /// <param name="exitApplicationUseCase">アプリケーション終了UseCase</param>
    public MainWindowViewModel(
        IUserNotificationService notificationService,
        NewDocumentUseCase newDocumentUseCase,
        OpenDocumentUseCase openDocumentUseCase,
        SaveDocumentUseCase saveDocumentUseCase,
        SaveDocumentAsUseCase saveDocumentAsUseCase,
        CloseDocumentUseCase closeDocumentUseCase,
        ExitApplicationUseCase exitApplicationUseCase)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _newDocumentUseCase = newDocumentUseCase ?? throw new ArgumentNullException(nameof(newDocumentUseCase));
        _openDocumentUseCase = openDocumentUseCase ?? throw new ArgumentNullException(nameof(openDocumentUseCase));
        _saveDocumentUseCase = saveDocumentUseCase ?? throw new ArgumentNullException(nameof(saveDocumentUseCase));
        _saveDocumentAsUseCase = saveDocumentAsUseCase ?? throw new ArgumentNullException(nameof(saveDocumentAsUseCase));
        _closeDocumentUseCase = closeDocumentUseCase ?? throw new ArgumentNullException(nameof(closeDocumentUseCase));
        _exitApplicationUseCase = exitApplicationUseCase ?? throw new ArgumentNullException(nameof(exitApplicationUseCase));

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

        // 起動直後から編集できるよう、初期Documentを作成してWorkspaceへ追加する。
        var initialDocument = _newDocumentUseCase.Execute();
        var initialTab = new DocumentTabViewModel(initialDocument);
        Workspace.AddTab(initialTab);

        // ActiveTabの切り替えと、アクティブなDocumentTabの状態変更を監視する。
        Workspace.PropertyChanged += OnWorkspacePropertyChanged;
        ObserveActiveTabForStatusBar();
        UpdateStatusBarInfo();

        // 初期タブに対応するコマンドの実行可否を更新する。
        RefreshCommandStates();
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

    /// <summary>
    /// Viewへウィンドウの終了を要求します。
    /// 終了確認と終了可否の判定はClosingイベントで行います。
    /// </summary>
    private void ExecuteExit()
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// アプリケーションの終了確認と必要な保存を実行し、
    /// ウィンドウを閉じてよいかを返します。
    /// 実際のウィンドウ終了処理はPresentation側で行います。
    /// </summary>
    /// <returns>
    /// 終了してよい場合はtrue、終了を中止する場合はfalse。
    /// </returns>
    public bool CanExit()
    {
        if (IsProcessing)
        {
            return false;
        }

        try
        {
            return _exitApplicationUseCase.Execute(Workspace);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(
                $"アプリケーションの終了処理に失敗しました: {ex.Message}");

            return false;
        }
    }

    private bool CanExecuteSearch() => !IsProcessing;
    private void ExecuteSearch() => _notificationService.ShowInfo("検索（仮実装）");

    private bool CanExecuteReplace() => !IsProcessing;
    private void ExecuteReplace() => _notificationService.ShowInfo("置換（仮実装）");

    private bool CanExecuteCompare() => !IsProcessing;
    private void ExecuteCompare() => _notificationService.ShowInfo("比較（仮実装）");

    /// <summary>
    /// StatusBar情報を更新します。
    /// </summary>
    private void UpdateStatusBarInfo()
    {
        if (Workspace.ActiveTab is not DocumentTabViewModel documentTab)
        {
            // DocumentTabでない場合はクリア
            StatusEncoding = string.Empty;
            StatusLineEnding = string.Empty;
            StatusCaretPosition = string.Empty;
            StatusModificationState = string.Empty;
            StatusReadOnlyState = string.Empty;
            StatusProcessingState = string.Empty;
            return;
        }

        var document = documentTab.Document;

        // Encoding表示
        StatusEncoding = document.Encoding switch
        {
            TextEncoding.Utf8 => "UTF-8",
            TextEncoding.Utf8Bom => "UTF-8 BOM",
            TextEncoding.ShiftJis => "Shift-JIS",
            _ => "Unknown"
        };

        // LineEnding表示
        StatusLineEnding = document.LineEnding switch
        {
            LineEnding.CrLf => "CRLF",
            LineEnding.Lf => "LF",
            LineEnding.Cr => "CR",
            _ => "Unknown"
        };

        // Caret位置表示（暫定実装：空文字）
        StatusCaretPosition = string.Empty;

        // 変更状態表示
        if (document.IsLoading || document.IsSaving)
        {
            StatusModificationState = string.Empty;
        }
        else if (document.IsModified)
        {
            StatusModificationState = "変更あり";
        }
        else
        {
            StatusModificationState = string.Empty;
        }

        // 読み取り専用表示
        if (document.IsReadOnly)
        {
            StatusReadOnlyState = "読み取り専用";
        }
        else
        {
            StatusReadOnlyState = string.Empty;
        }

        // 処理中表示
        if (document.IsLoading)
        {
            StatusProcessingState = "読み込み中...";
        }
        else if (document.IsSaving)
        {
            StatusProcessingState = "保存中...";
        }
        else
        {
            StatusProcessingState = string.Empty;
        }
    }

    /// <summary>
    /// Workspaceのプロパティ変更を処理します。
    /// </summary>
    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Workspace.ActiveTab))
        {
            return;
        }

        ObserveActiveTabForStatusBar();
        UpdateStatusBarInfo();
        RefreshCommandStates();
    }

    /// <summary>
    /// 現在のアクティブタブをStatusBar更新の監視対象にします。
    /// 以前の監視対象がある場合はイベント購読を解除します。
    /// </summary>
    private void ObserveActiveTabForStatusBar()
    {
        if (_observedStatusTab != null)
        {
            _observedStatusTab.PropertyChanged -= OnActiveTabPropertyChanged;
        }

        _observedStatusTab = Workspace.ActiveTab as DocumentTabViewModel;

        if (_observedStatusTab != null)
        {
            _observedStatusTab.PropertyChanged += OnActiveTabPropertyChanged;
        }
    }

    /// <summary>
    /// アクティブなDocumentTabの状態変更を処理します。
    /// </summary>
    private void OnActiveTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DocumentTabViewModel.IsModified)
            or nameof(DocumentTabViewModel.IsReadOnly)
            or nameof(DocumentTabViewModel.IsLoading)
            or nameof(DocumentTabViewModel.IsSaving)
            or nameof(DocumentTabViewModel.IsEditable))
        {
            UpdateStatusBarInfo();
            RefreshCommandStates();
        }
    }

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
