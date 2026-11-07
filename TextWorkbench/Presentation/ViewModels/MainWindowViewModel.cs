using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.Helpers;
using TextWorkbench.Presentation.Views;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// MainWindowのViewModelです。
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IUserNotificationService _notificationService;
    private readonly IFileDialogService _fileDialogService;
    private readonly NewDocumentUseCase _newDocumentUseCase;
    private readonly OpenDocumentUseCase _openDocumentUseCase;
    private readonly SaveDocumentUseCase _saveDocumentUseCase;
    private readonly SaveDocumentAsUseCase _saveDocumentAsUseCase;
    private readonly CloseDocumentUseCase _closeDocumentUseCase;
    private readonly ExitApplicationUseCase _exitApplicationUseCase;
    private readonly GetTextInfoUseCase _getTextInfoUseCase;
    private readonly CompareDocumentsUseCase _compareDocumentsUseCase;

    /// <summary>
    /// StatusBar更新の監視対象となるTabを取得または設定します。
    /// </summary>
    private TabViewModel? _observedStatusTab;

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
    /// Document用StatusBarの表示状態を取得します。
    /// </summary>
    public bool IsDocumentStatusVisible =>
        Workspace.ActiveTab is DocumentTabViewModel;

    /// <summary>
    /// Diff用StatusBarの表示状態を取得します。
    /// </summary>
    public bool IsDiffStatusVisible =>
        Workspace.ActiveTab is DiffTabViewModel;

    /// <summary>
    /// DocumentのEncoding表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _documentStatusEncoding = string.Empty;

    /// <summary>
    /// DocumentのLineEnding表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _documentStatusLineEnding = string.Empty;

    /// <summary>
    /// DocumentのCaret位置表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _documentStatusCaretPosition = string.Empty;

    /// <summary>
    /// Documentの変更状態表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _documentStatusModificationState = string.Empty;

    /// <summary>
    /// Documentの読み取り専用状態表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _documentStatusReadOnlyState = string.Empty;

    /// <summary>
    /// Documentの処理中状態表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _documentStatusProcessingState = string.Empty;

    /// <summary>
    /// Diffの比較対象表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _diffStatusComparisonText = string.Empty;

    /// <summary>
    /// Diffの現在位置表示を取得します。
    /// </summary>
    [ObservableProperty]
    private string _diffStatusPositionText = string.Empty;

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
    /// 現在のドキュメントと保存済みファイルを比較するコマンドを取得します。
    /// </summary>
    public IRelayCommand CompareWithSavedCommand { get; }

    /// <summary>
    /// 次の差分コマンドを取得します。
    /// </summary>
    public IRelayCommand NextDiffCommand { get; }

    /// <summary>
    /// 前の差分コマンドを取得します。
    /// </summary>
    public IRelayCommand PreviousDiffCommand { get; }

    /// <summary>
    /// テキスト情報コマンドを取得します。
    /// </summary>
    public IRelayCommand TextInfoCommand { get; }

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
    /// <param name="fileDialogService">ファイルダイアログサービス</param>
    /// <param name="newDocumentUseCase">新規Document作成UseCase</param>
    /// <param name="openDocumentUseCase">ファイルを開くUseCase</param>
    /// <param name="saveDocumentUseCase">Document保存UseCase</param>
    /// <param name="saveDocumentAsUseCase">名前を付けて保存UseCase</param>
    /// <param name="closeDocumentUseCase">Documentを閉じるUseCase</param>
    /// <param name="exitApplicationUseCase">アプリケーション終了UseCase</param>
    /// <param name="getTextInfoUseCase">テキスト情報取得UseCase</param>
    /// <param name="searchTextUseCase">検索UseCase</param>
    /// <param name="replaceTextUseCase">置換UseCase</param>
    /// <param name="compareDocumentsUseCase">ファイル比較UseCase</param>
    /// <param name="workspace">アプリケーション全体の作業領域</param>
    public MainWindowViewModel(
        IUserNotificationService notificationService,
        IFileDialogService fileDialogService,
        NewDocumentUseCase newDocumentUseCase,
        OpenDocumentUseCase openDocumentUseCase,
        SaveDocumentUseCase saveDocumentUseCase,
        SaveDocumentAsUseCase saveDocumentAsUseCase,
        CloseDocumentUseCase closeDocumentUseCase,
        ExitApplicationUseCase exitApplicationUseCase,
        GetTextInfoUseCase getTextInfoUseCase,
        SearchTextUseCase searchTextUseCase,
        ReplaceTextUseCase replaceTextUseCase,
        CompareDocumentsUseCase compareDocumentsUseCase,
        Workspace workspace)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _newDocumentUseCase = newDocumentUseCase ?? throw new ArgumentNullException(nameof(newDocumentUseCase));
        _openDocumentUseCase = openDocumentUseCase ?? throw new ArgumentNullException(nameof(openDocumentUseCase));
        _saveDocumentUseCase = saveDocumentUseCase ?? throw new ArgumentNullException(nameof(saveDocumentUseCase));
        _saveDocumentAsUseCase = saveDocumentAsUseCase ?? throw new ArgumentNullException(nameof(saveDocumentAsUseCase));
        _closeDocumentUseCase = closeDocumentUseCase ?? throw new ArgumentNullException(nameof(closeDocumentUseCase));
        _exitApplicationUseCase = exitApplicationUseCase ?? throw new ArgumentNullException(nameof(exitApplicationUseCase));
        _getTextInfoUseCase = getTextInfoUseCase ?? throw new ArgumentNullException(nameof(getTextInfoUseCase));
        _compareDocumentsUseCase = compareDocumentsUseCase ?? throw new ArgumentNullException(nameof(compareDocumentsUseCase));

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
        CompareWithSavedCommand = new RelayCommand(ExecuteCompareWithSaved, CanExecuteCompareWithSaved);
        NextDiffCommand = new RelayCommand(ExecuteNextDiff, CanExecuteNextDiff);
        PreviousDiffCommand = new RelayCommand(ExecutePreviousDiff, CanExecutePreviousDiff);
        FindNextCommand = new RelayCommand(ExecuteFindNext, CanExecuteFindNext);
        FindPreviousCommand = new RelayCommand(ExecuteFindPrevious, CanExecuteFindPrevious);
        ReplaceNextCommand = new RelayCommand(ExecuteReplaceNext, CanExecuteReplaceNext);
        ReplaceAllCommand = new RelayCommand(ExecuteReplaceAll, CanExecuteReplaceAll);
        TextInfoCommand = new RelayCommand(ExecuteTextInfo, CanExecuteTextInfo);

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
            var succeeded = _saveDocumentAsUseCase.Execute(documentTab.Document);

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
        // 処理中、または閉じる対象のTabがない場合は実行できない。
        if (IsProcessing || Workspace.ActiveTab == null)
        {
            return false;
        }

        // DiffTabは常に閉じ可能
        if (Workspace.ActiveTab is DiffTabViewModel)
        {
            return true;
        }

        // DocumentTabの場合は読み込み中・保存中でない場合のみ閉じ可能
        if (Workspace.ActiveTab is DocumentTabViewModel documentTab)
        {
            return !documentTab.Document.IsLoading
                && !documentTab.Document.IsSaving;
        }

        return false;
    }

    /// <summary>
    /// アクティブなTabを閉じます。
    /// </summary>
    private void ExecuteClose()
    {
        if (Workspace.ActiveTab == null)
        {
            return;
        }

        // DiffTabは保存確認なしで閉じる
        if (Workspace.ActiveTab is DiffTabViewModel diffTab)
        {
            Workspace.RemoveTab(diffTab);
            RefreshCommandStates();
            return;
        }

        // DocumentTabは保存確認を行う
        if (Workspace.ActiveTab is DocumentTabViewModel documentTab)
        {
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
    private bool CanExecuteOpenSearchReplacePanel() =>
        !IsProcessing && Workspace.ActiveTab is DocumentTabViewModel;

    /// <summary>
    /// 検索・置換パネルを開きます。
    /// </summary>
    private void ExecuteOpenSearchReplacePanel()
    {
        OpenSearchReplacePanel();
    }

    private bool CanExecuteTextInfo() =>
        !IsProcessing && Workspace.ActiveTab is DocumentTabViewModel;

    private void ExecuteTextInfo()
    {
        if (Workspace.ActiveTab is not DocumentTabViewModel documentTab)
            return;

        try
        {
            var document = documentTab.Document;
            var textInfo = _getTextInfoUseCase.Execute(document.CurrentContent, document.Encoding, document.LineEnding);

            var dialog = new TextInfoDialog();
            var viewModel = dialog.DataContext as TextInfoDialogViewModel;
            viewModel?.SetTextInfo(textInfo);

            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();
        }
        catch (TextWorkbenchException ex)
        {
            _notificationService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            _notificationService.ShowError("テキスト情報の取得に失敗しました。");
        }
    }

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
        && documentTab.Document.IsEditable
        && documentTab.SearchResults.Count > 0;

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
        catch (TextWorkbenchException ex)
        {
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
        && documentTab.Document.IsEditable
        && documentTab.SearchResults.Count > 0;

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
        catch (TextWorkbenchException ex)
        {
            _notificationService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            _notificationService.ShowError("すべて置換に失敗しました。");
        }
    }

    /// <summary>
    /// ファイル比較を実行できるかどうかを判定します。
    /// </summary>
    private bool CanExecuteCompare() => !IsProcessing;

    /// <summary>
    /// 比較元を選択して比較し、DiffTabをWorkspaceへ追加します。
    /// </summary>
    private void ExecuteCompare()
    {
        try
        {
            IsProcessing = true;
            RefreshCommandStates();

            // 比較元選択ダイアログを作成
            var dialogViewModel = new ComparisonSourceSelectionDialogViewModel(
                Workspace,
                _fileDialogService);

            var dialog = new ComparisonSourceSelectionDialog
            {
                DataContext = dialogViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };

            // ダイアログを表示
            var result = dialog.ShowDialog();

            if (result != true ||
                dialogViewModel.LeftComparisonSource == null ||
                dialogViewModel.RightComparisonSource == null)
            {
                return;
            }

            // 比較を実行
            var diffSession = _compareDocumentsUseCase.Execute(
                dialogViewModel.LeftComparisonSource,
                dialogViewModel.RightComparisonSource);

            // 比較成功後にDiffTabを生成
            var diffTab = new DiffTabViewModel(diffSession);

            // Workspaceへ追加するとActiveTabも自動的に切り替わる
            Workspace.AddTab(diffTab);
        }
        catch (TextWorkbenchException ex)
        {
            _notificationService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            _notificationService.ShowError("比較に失敗しました。");
        }
        finally
        {
            IsProcessing = false;

            RefreshCommandStates();
        }
    }

    /// <summary>
    /// 現在のドキュメントと保存済みファイルを比較できるかどうかを判定します。
    /// </summary>
    private bool CanExecuteCompareWithSaved()
    {
        return !IsProcessing
            && Workspace.ActiveTab is DocumentTabViewModel documentTab
            && documentTab.Document.FilePath != null
            && documentTab.Document.IsModified;
    }

    /// <summary>
    /// 現在のドキュメントと保存済みファイルを比較します。
    /// </summary>
    private void ExecuteCompareWithSaved()
    {
        try
        {
            IsProcessing = true;
            RefreshCommandStates();

            if (Workspace.ActiveTab is not DocumentTabViewModel documentTab)
            {
                return;
            }

            // 左側：保存済みファイル（SourceType.File、Document.FilePath）
            var leftSource = new ComparisonSource(
                SourceType.File,
                documentTab.Document.FilePath!);

            // 右側：現在のドキュメント（SourceType.OpenDocument、Document.Id）
            var rightSource = new ComparisonSource(
                SourceType.OpenDocument,
                documentTab.Document.Id.ToString());

            // 比較を実行
            var diffSession = _compareDocumentsUseCase.Execute(
                leftSource,
                rightSource);

            // 比較成功後にDiffTabを生成
            var diffTab = new DiffTabViewModel(diffSession);

            // Workspaceへ追加するとActiveTabも自動的に切り替わる
            Workspace.AddTab(diffTab);
        }
        catch (TextWorkbenchException ex)
        {
            _notificationService.ShowError(ex.Message);
        }
        catch (Exception)
        {
            _notificationService.ShowError("比較に失敗しました。");
        }
        finally
        {
            IsProcessing = false;

            RefreshCommandStates();
        }
    }

    /// <summary>
    /// 次の差分に移動できるかどうかを判定します。
    /// </summary>
    private bool CanExecuteNextDiff()
    {
        return !IsProcessing
            && Workspace.ActiveTab is DiffTabViewModel diffTab
            && diffTab.DiffCount > 0
            && diffTab.CurrentDiffIndex < diffTab.DiffCount - 1; 
    }

    /// <summary>
    /// 次の差分に移動します。
    /// </summary>
    private void ExecuteNextDiff()
    {
        if (Workspace.ActiveTab is DiffTabViewModel diffTab)
        {
            diffTab.NextDiff();
            RefreshCommandStates();
        }
    }

    /// <summary>
    /// 前の差分に移動できるかどうかを判定します。
    /// </summary>
    private bool CanExecutePreviousDiff()
    {
        return !IsProcessing
            && Workspace.ActiveTab is DiffTabViewModel diffTab
            && diffTab.DiffCount > 0
            && diffTab.CurrentDiffIndex > 0;
    }

    /// <summary>
    /// 前の差分に移動します。
    /// </summary>
    private void ExecutePreviousDiff()
    {
        if (Workspace.ActiveTab is DiffTabViewModel diffTab)
        {
            diffTab.PreviousDiff();
            RefreshCommandStates();
        }
    }

    /// <summary>
    /// StatusBar情報を更新します。
    /// </summary>
    private void UpdateStatusBarInfo()
    {
        // DiffTabがアクティブな場合
        if (Workspace.ActiveTab is DiffTabViewModel diffTab)
        {
            var leftSource = diffTab.DiffSession.LeftSource;
            var rightSource = diffTab.DiffSession.RightSource;

            // 比較対象と文字コード
            DiffStatusComparisonText =
                $"比較: {leftSource.DisplayName} " +
                $"({TextEncodingHelper.ToDisplayString(leftSource.Encoding)}) " +
                $"↔ {rightSource.DisplayName} " +
                $"({TextEncodingHelper.ToDisplayString(rightSource.Encoding)})";

            DiffStatusPositionText = diffTab.CurrentDiffIndex < 0
                ? $"差分 {diffTab.DiffCount}件"
                : $"差分 {diffTab.CurrentDiffIndex + 1} / {diffTab.DiffCount}";

            ClearDocumentStatus();
            return;
        }

        // DocumentTab以外の場合はクリア
        if (Workspace.ActiveTab is not DocumentTabViewModel documentTab)
        {
            ClearDocumentStatus();
            ClearDiffStatus();
            return;
        }

        var document = documentTab.Document;

        // Encoding表示
        DocumentStatusEncoding = TextEncodingHelper.ToDisplayString(document.Encoding);

        // LineEnding表示
        DocumentStatusLineEnding = LineEndingHelper.ToDisplayString(document.LineEnding);

        // Caret位置表示
        DocumentStatusCaretPosition = $"行{documentTab.CaretLine}、列{documentTab.CaretColumn}";

        // 変更状態表示
        if (document.IsLoading || document.IsSaving)
        {
            DocumentStatusModificationState = string.Empty;
        }
        else
        {
            DocumentStatusModificationState =
                document.IsModified ? "変更あり" : string.Empty;
        }

        // 読み取り専用表示
        DocumentStatusReadOnlyState =
            document.IsReadOnly ? "読み取り専用" : string.Empty;

        // 処理中表示
        if (document.IsLoading)
        {
            DocumentStatusProcessingState = "読み込み中...";
        }
        else if (document.IsSaving)
        {
            DocumentStatusProcessingState = "保存中...";
        }
        else
        {
            DocumentStatusProcessingState = string.Empty;
        }

        ClearDiffStatus();
    }

    /// <summary>
    /// Document用StatusBar情報をクリアします。
    /// </summary>
    private void ClearDocumentStatus()
    {
        DocumentStatusEncoding = string.Empty;
        DocumentStatusLineEnding = string.Empty;
        DocumentStatusCaretPosition = string.Empty;
        DocumentStatusModificationState = string.Empty;
        DocumentStatusReadOnlyState = string.Empty;
        DocumentStatusProcessingState = string.Empty;
    }

    /// <summary>
    /// Diff用StatusBar情報をクリアします。
    /// </summary>
    private void ClearDiffStatus()
    {
        DiffStatusComparisonText = string.Empty;
        DiffStatusPositionText = string.Empty;
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
        OnPropertyChanged(nameof(IsDocumentStatusVisible));
        OnPropertyChanged(nameof(IsDiffStatusVisible));
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

        _observedStatusTab = Workspace.ActiveTab;

        if (_observedStatusTab != null)
        {
            _observedStatusTab.PropertyChanged += OnActiveTabPropertyChanged;
        }
    }

    /// <summary>
    /// アクティブTabの状態変更を処理します。
    /// </summary>
    private void OnActiveTabPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (sender is DocumentTabViewModel)
        {
            if (e.PropertyName == nameof(DocumentTabViewModel.SearchResults))
            {
                RefreshCommandStates();
                return;
            }

            // Caret位置変更
            if (e.PropertyName is nameof(DocumentTabViewModel.CaretLine)
                or nameof(DocumentTabViewModel.CaretColumn))
            {
                UpdateStatusBarInfo();
                return;
            }

            // Document状態変更
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
        else if (sender is DiffTabViewModel)
        {
            // 現在の差分位置が変わった場合
            if (e.PropertyName == nameof(DiffTabViewModel.CurrentDiffIndex))
            {
                UpdateStatusBarInfo();
            }
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
        CompareWithSavedCommand.NotifyCanExecuteChanged();
        NextDiffCommand.NotifyCanExecuteChanged();
        PreviousDiffCommand.NotifyCanExecuteChanged();
        FindNextCommand.NotifyCanExecuteChanged();
        FindPreviousCommand.NotifyCanExecuteChanged();
        ReplaceNextCommand.NotifyCanExecuteChanged();
        ReplaceAllCommand.NotifyCanExecuteChanged();
        TextInfoCommand.NotifyCanExecuteChanged();
    }
}
