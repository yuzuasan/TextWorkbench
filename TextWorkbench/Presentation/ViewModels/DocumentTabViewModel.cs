using CommunityToolkit.Mvvm.ComponentModel;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// DocumentをUIへ公開するViewModelです。
/// </summary>
public sealed partial class DocumentTabViewModel : TabViewModel
{
    /// <summary>
    /// Documentを取得します。
    /// </summary>
    public Document Document { get; }

    /// <summary>
    /// 保存後から編集が行われたかどうかを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private bool _isModified;

    /// <summary>
    /// ファイルが読み取り専用として扱われるかどうかを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private bool _isReadOnly;

    /// <summary>
    /// Documentの読み込み処理中かどうかを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Documentの保存処理中かどうかを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private bool _isSaving;

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
        _isModified = document.IsModified;
        _isReadOnly = document.IsReadOnly;
        _isLoading = document.IsLoading;
        _isSaving = document.IsSaving;
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
