using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Presentation.Helpers;

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
    /// 検索・置換パネルのViewModelを取得します。
    /// </summary>
    public SearchReplacePanelViewModel SearchReplacePanelViewModel { get; }

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
    /// 検索・置換パネルを開くコマンドを取得します。
    /// </summary>
    public IRelayCommand OpenSearchReplacePanelCommand { get; }

    /// <summary>
    /// 比較コマンドを取得します。
    /// </summary>
    public IRelayCommand CompareCommand { get; }

    /// <summary>
    /// 次の検索結果へ移動するコマンドを取得します。
    /// </summary>
    public IRelayCommand FindNextCommand { get; }

    /// <summary>
    /// 前の検索結果へ移動するコマンドを取得します。
    /// </summary>
    public IRelayCommand FindPreviousCommand { get; }

    /// <summary>
    /// 次の一致箇所を置換するコマンドを取得します。
    /// </summary>
    public IRelayCommand ReplaceNextCommand { get; }

    /// <summary>
    /// 文書内の一致箇所をすべて置換するコマンドを取得します。
    /// </summary>
    public IRelayCommand ReplaceAllCommand { get; }

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
    /// <param name="searchTextUseCase">検索UseCase</param>
    /// <param name="replaceTextUseCase">置換UseCase</param>
    /// <param name="workspace">アプリケーション全体の作業領域</param>
    public MainWindowViewModel(
        IUserNotificationService notificationService,
        NewDocumentUseCase newDocumentUseCase,
        OpenDocumentUseCase openDocumentUseCase,
        SaveDocumentUseCase saveDocumentUseCase,
        SaveDocumentAsUseCase saveDocumentAsUseCase,
        CloseDocumentUseCase closeDocumentUseCase,
        ExitApplicationUseCase exitApplicationUseCase,
        SearchTextUseCase searchTextUseCase,
        ReplaceTextUseCase replaceTextUseCase,
        Workspace workspace)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _newDocumentUseCase = newDocumentUseCase ?? throw new ArgumentNullException(nameof(newDocumentUseCase));
        _openDocumentUseCase = openDocumentUseCase ?? throw new ArgumentNullException(nameof(openDocumentUseCase));
        _saveDocumentUseCase = saveDocumentUseCase ?? throw new ArgumentNullException(nameof(saveDocumentUseCase));
        _saveDocumentAsUseCase = saveDocumentAsUseCase ?? throw new ArgumentNullException(nameof(saveDocumentAsUseCase));
        _closeDocumentUseCase = closeDocumentUseCase ?? throw new ArgumentNullException(nameof(closeDocumentUseCase));
        _exitApplicationUseCase = exitApplicationUseCase ?? throw new ArgumentNullException(nameof(exitApplicationUseCase));

        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        SearchReplacePanelViewModel = new SearchReplacePanelViewModel(
            searchTextUseCase,
            replaceTextUseCase);

        NewCommand = new RelayCommand(ExecuteNew, CanExecuteNew);
        OpenCommand = new RelayCommand(ExecuteOpen, CanExecuteOpen);
        SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        SaveAsCommand = new RelayCommand(ExecuteSaveAs, CanExecuteSaveAs);
        CloseCommand = new RelayCommand(ExecuteClose, CanExecuteClose);
        ExitCommand = new RelayCommand(ExecuteExit, CanExecuteExit);
        OpenSearchReplacePanelCommand = new RelayCommand(ExecuteOpenSearchReplacePanel, CanExecuteOpenSearchReplacePanel);
        CompareCommand = new RelayCommand(ExecuteCompare, CanExecuteCompare);
        FindNextCommand = new RelayCommand(ExecuteFindNext, CanExecuteFindNext);
        FindPreviousCommand = new RelayCommand(ExecuteFindPrevious, CanExecuteFindPrevious);
        ReplaceNextCommand = new RelayCommand(ExecuteReplaceNext, CanExecuteReplaceNext);
        ReplaceAllCommand = new RelayCommand(ExecuteReplaceAll, CanExecuteReplaceAll);

        // 起動直後から編集できるよう、初期Documentを作成してWorkspaceへ追加する。
        var initialDocument = _newDocumentUseCase.Execute();
        var initialTab = new DocumentTabViewModel(initialDocument);
        Workspace.AddTab(initialTab);

        // ActiveTabの切り替えと、アクティブなDocumentTabの状態変更を監視する。
        Workspace.PropertyChanged += OnWorkspacePropertyChanged;
        SearchReplacePanelViewModel.ActiveDocumentTab = Workspace.ActiveTab as DocumentTabViewModel;
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
        catch (Exception)
        {
            _notificationService.ShowError("新規作成に失敗しました。");
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
        catch (TextWorkbenchException ex)
        {
            _notificationService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            _notificationService.ShowError("ファイルを開くことに失敗しました。");
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
        catch (TextWorkbenchException ex)
        {
            _notificationService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            _notificationService.ShowError("保存に失敗しました。");
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
        catch (TextWorkbenchException ex)
        {
            _notificationService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            _notificationService.ShowError("名前を付けて保存に失敗しました。");
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
        catch (TextWorkbenchException ex)
        {
            _notificationService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            // 保存失敗などの場合はタブを残してエラーを通知する。
            _notificationService.ShowError("閉じることに失敗しました。");
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
        catch (TextWorkbenchException ex)
        {
            _notificationService.ShowError(ex.Message);
            return false;
        }
        catch (Exception)
        {
            _notificationService.ShowError("アプリケーションの終了処理に失敗しました。");
            return false;
        }
    }

    /// <summary>
    /// 検索・置換パネルを開けるかどうかを判定します。
    /// </summary>
    private bool CanExecuteOpenSearchReplacePanel() => !IsProcessing;

    /// <summary>
    /// 検索・置換パネルを開きます。
    /// </summary>
    private void ExecuteOpenSearchReplacePanel()
    {
        OpenSearchReplacePanel();
    }

    private bool CanExecuteCompare() => !IsProcessing;
    private void ExecuteCompare() => _notificationService.ShowInfo("比較（仮実装）");

    private bool CanExecuteFindNext() =>
        Workspace.ActiveTab is DocumentTabViewModel documentTab
        && documentTab.SearchResults.Count > 0;

    private void ExecuteFindNext()
    {
        if (Workspace.ActiveTab is DocumentTabViewModel documentTab)
        {
            documentTab.FindNext();
        }
    }

    private bool CanExecuteFindPrevious() =>
        Workspace.ActiveTab is DocumentTabViewModel documentTab
        && documentTab.SearchResults.Count > 0;

    private void ExecuteFindPrevious()
    {
        if (Workspace.ActiveTab is DocumentTabViewModel documentTab)
        {
            documentTab.FindPrevious();
        }
    }

    /// <summary>
    /// Replace Nextを実行できるかどうかを判定します。
    /// </summary>
    private bool CanExecuteReplaceNext() =>
        !IsProcessing
        && Workspace.ActiveTab is DocumentTabViewModel documentTab
        && documentTab.Document.IsEditable;

    /// <summary>
    /// 次の一致箇所を1件置換し、Documentへ反映します。
    /// </summary>
    private void ExecuteReplaceNext()
    {
        if (Workspace.ActiveTab is not DocumentTabViewModel documentTab
            || !documentTab.Document.IsEditable)
        {
            return;
        }

        try
        {
            var result = SearchReplacePanelViewModel.ReplaceFirst();

            // 一致箇所がない場合は文書を変更しません。
            if (result.ReplacementCount == 0)
            {
                return;
            }

            // 置換成功時には、置換位置と挿入文字列の長さから
            // 次回の検索開始位置を計算します。
            var matchStartIndex = result.FirstMatchStartIndex!.Value;
            var matchLength = result.FirstMatchLength!.Value;
            var replacementLength = result.ReplacementLength!.Value;

            var nextStartIndex = matchStartIndex + replacementLength;

            // 長さ0の一致を空文字列へ置換した場合も、
            // 同じ位置を次回の開始位置として再利用しません。
            if (matchLength == 0)
            {
                nextStartIndex = matchStartIndex
                    + Math.Max(replacementLength, 1);
            }

            // Documentを更新し、未保存状態とEditorへの同期、
            // 検索結果の破棄を既存処理へ委譲します。
            documentTab.Document.UpdateContent(result.Content);
            documentTab.OnDocumentContentChanged();

            // ドキュメント変更処理によるリセット後に、
            // 次回の置換開始位置を設定します。
            documentTab.ReplaceNextStartIndex = nextStartIndex;

            RefreshCommandStates();
        }
        catch (NotSupportedException ex)
        {
            // 未対応の正規表現置換など。
            _notificationService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            _notificationService.ShowError("置換に失敗しました。");
        }
    }

    /// <summary>
    /// Replace Allを実行できるかどうかを判定します。
    /// </summary>
    private bool CanExecuteReplaceAll() =>
        !IsProcessing
        && Workspace.ActiveTab is DocumentTabViewModel documentTab
        && documentTab.Document.IsEditable;

    /// <summary>
    /// 文書内の一致箇所をすべて置換し、置換件数を通知します。
    /// </summary>
    private void ExecuteReplaceAll()
    {
        if (Workspace.ActiveTab is not DocumentTabViewModel documentTab
            || !documentTab.Document.IsEditable)
        {
            return;
        }

        try
        {
            var result = SearchReplacePanelViewModel.ReplaceAll();

            // 0件の場合も件数を通知しますが、
            // Documentの内容や未保存状態は変更しません。
            if (result.ReplacementCount == 0)
            {
                _notificationService.ShowInfo("0件置換しました");
                return;
            }

            // 置換結果をDocumentへ反映します。
            documentTab.Document.UpdateContent(result.Content);

            // Replace All後は次回のReplace Nextを文書先頭から開始します。
            documentTab.ReplaceNextStartIndex = 0;

            // 未保存状態の通知、Editorへの同期、
            // 古い検索結果の破棄を既存処理へ委譲します。
            documentTab.OnDocumentContentChanged();

            RefreshCommandStates();

            // 置換件数を通知します。
            _notificationService.ShowInfo(
                $"{result.ReplacementCount}件置換しました");
        }
        catch (NotSupportedException ex)
        {
            // 未対応の正規表現置換など。
            _notificationService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            _notificationService.ShowError("すべて置換に失敗しました。");
        }
    }

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
        StatusEncoding = TextEncodingHelper.ToDisplayString(document.Encoding);

        // LineEnding表示
        StatusLineEnding = LineEndingHelper.ToDisplayString(document.LineEnding);

        // アクティブなDocumentTabのCaret位置をStatusBarへ表示する。
        StatusCaretPosition = $"行{documentTab.CaretLine}、列{documentTab.CaretColumn}";

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
    /// 検索・置換パネルを表示します。
    /// </summary>
    private void OpenSearchReplacePanel()
    {
        if (Workspace.ActiveTab is not DocumentTabViewModel documentTab)
        {
            return;
        }

        SearchReplacePanelViewModel.ActiveDocumentTab = documentTab;
        SearchReplacePanelViewModel.IsOpen = true;
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

        SearchReplacePanelViewModel.ActiveDocumentTab = Workspace.ActiveTab as DocumentTabViewModel;
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
        if (e.PropertyName == nameof(DocumentTabViewModel.SearchResults))
        {
            RefreshCommandStates();
            return;
        }

        // Caret位置が変わった場合もStatusBarを更新する。
        if (e.PropertyName is nameof(DocumentTabViewModel.CaretLine)
            or nameof(DocumentTabViewModel.CaretColumn))
        {
            UpdateStatusBarInfo();
            return;
        }

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
        OpenSearchReplacePanelCommand.NotifyCanExecuteChanged();
        CompareCommand.NotifyCanExecuteChanged();
        FindNextCommand.NotifyCanExecuteChanged();
        FindPreviousCommand.NotifyCanExecuteChanged();
        ReplaceNextCommand.NotifyCanExecuteChanged();
        ReplaceAllCommand.NotifyCanExecuteChanged();
    }
}
