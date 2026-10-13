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
    public void OnDocumentContentChanged()
    {
        OnPropertyChanged(nameof(IsModified));
        OnPropertyChanged(nameof(IsEditable));

        _documentBehavior?.SyncFromDocument();
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
