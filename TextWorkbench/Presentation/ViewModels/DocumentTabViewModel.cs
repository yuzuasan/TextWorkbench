using CommunityToolkit.Mvvm.ComponentModel;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.Behaviors;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// DocumentをUIへ公開するViewModelです。
/// </summary>
public sealed partial class DocumentTabViewModel : TabViewModel
{
    private TextEditorDocumentBehavior? _documentBehavior;
    private EditorStateBehavior? _editorStateBehavior;

    /// <summary>
    /// Documentを取得します。
    /// </summary>
    public Document Document { get; }

    /// <summary>
    /// ツールチップ用のテキストを取得します。
    /// </summary>
    public string ToolTipText => Document.FilePath ?? DisplayTitle;

    /// <summary>
    /// 保存後から編集が行われたかどうかを取得します。
    /// </summary>
    public bool IsModified => Document.IsModified;

    /// <summary>
    /// ファイルが読み取り専用として扱われるかどうかを取得します。
    /// </summary>
    public bool IsReadOnly => Document.IsReadOnly;

    /// <summary>
    /// Documentの読み込み処理中かどうかを取得します。
    /// </summary>
    public bool IsLoading => Document.IsLoading;

    /// <summary>
    /// Documentの保存処理中かどうかを取得します。
    /// </summary>
    public bool IsSaving => Document.IsSaving;

    /// <summary>
    /// Documentが編集可能かどうかを取得します。
    /// </summary>
    public bool IsEditable => Document.IsEditable;

    /// <summary>
    /// Caretの行番号を取得します。行番号は1始まりです。
    /// </summary>
    [ObservableProperty]
    private int _caretLine = 1;

    /// <summary>
    /// Caretの列番号を取得します。列番号は1始まりです。
    /// </summary>
    [ObservableProperty]
    private int _caretColumn = 1;

    /// <summary>
    /// 検索テキストを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// 正規表現を使用するかどうかを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private bool _isRegex;

    /// <summary>
    /// 検索結果一覧を取得または設定します。
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<SearchResult> _searchResults = Array.Empty<SearchResult>();

    /// <summary>
    /// 現在の検索結果インデックスを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private int _currentSearchIndex = -1;

    /// <summary>
    /// Replace Nextの開始インデックスを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private int _replaceNextStartIndex = 0;

    /// <summary>
    /// <see cref="DocumentTabViewModel"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="document">Document</param>
    public DocumentTabViewModel(Document document)
        : base(CalculateDisplayTitle(document))
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }

    /// <summary>
    /// TextEditorDocumentBehaviorを設定します。
    /// </summary>
    public void SetDocumentBehavior(TextEditorDocumentBehavior behavior)
    {
        _documentBehavior = behavior;
        _documentBehavior.Document = Document;
        _documentBehavior.SetOnDocumentContentChanged(syncFromEditor => OnDocumentContentChanged(syncFromEditor));
    }

    /// <summary>
    /// EditorStateBehaviorを設定します。
    /// </summary>
    public void SetEditorStateBehavior(EditorStateBehavior behavior)
    {
        _editorStateBehavior = behavior;
        _editorStateBehavior.Document = Document;
    }

    /// <summary>
    /// AvalonEditからCaret位置を受け取り、UI状態を更新します。
    /// </summary>
    /// <param name="line">1始まりの行番号</param>
    /// <param name="column">1始まりの列番号</param>
    public void UpdateCaretPosition(int line, int column)
    {
        CaretLine = line;
        CaretColumn = column;
    }

    /// <summary>
    /// 検索結果を設定し、現在位置を未選択状態へ戻します。
    /// </summary>
    /// <param name="results">検索結果一覧</param>
    public void SetSearchResults(IReadOnlyList<SearchResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        SearchResults = results;
        CurrentSearchIndex = -1;
    }

    /// <summary>
    /// 検索結果と現在位置を初期状態へ戻します。
    /// </summary>
    public void ClearSearchResults()
    {
        SearchResults = Array.Empty<SearchResult>();
        CurrentSearchIndex = -1;
    }

    /// <summary>
    /// Documentの状態変更時に呼び出します。
    /// </summary>
    public void OnDocumentStateChanged()
    {
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsSaving));
        OnPropertyChanged(nameof(IsEditable));

        _editorStateBehavior?.UpdateEditorReadOnly();
    }

    /// <summary>
    /// Documentの内容変更時に呼び出します。
    /// </summary>
    /// <param name="syncFromDocument">DocumentからEditorへ同期するかどうか（プログラム変更時はtrue、ユーザー編集時はfalse）</param>
    public void OnDocumentContentChanged(bool syncFromDocument = true)
    {
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(IsEditable));

        // 検索結果はDocument.CurrentContentの文字位置を保持するため、
        // 内容が変更された時点で古い検索結果を破棄します。
        ClearSearchResults();

        if (syncFromDocument)
        {
            _documentBehavior?.SyncFromDocument();
        }
    }

    /// <summary>
    /// Documentの保存成功時に呼び出します。
    /// </summary>
    public void OnDocumentSaved()
    {
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(IsSaving));
        OnPropertyChanged(nameof(IsEditable));

        _editorStateBehavior?.UpdateEditorReadOnly();
    }

    /// <summary>
    /// Documentの名前を付けて保存成功時に呼び出します。
    /// </summary>
    public void OnDocumentSavedAs()
    {
        // Save AsでDocument.FileNameが更新されるため、Tabヘッダーの値自体も更新する。
        DisplayTitle = CalculateDisplayTitle(Document);

        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(IsSaving));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(IsEditable));

        _editorStateBehavior?.UpdateEditorReadOnly();
    }

    /// <summary>
    /// DisplayTitleを算出します。
    /// </summary>
    private static string CalculateDisplayTitle(Document document)
    {
        if (!string.IsNullOrEmpty(document.FileName))
        {
            return document.FileName;
        }

        return "無題";
    }
}
