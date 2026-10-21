namespace TextWorkbench.Application.Models;

/// <summary>
/// 検索文字列と検索方法を表す検索条件です。
/// </summary>
public sealed class SearchCondition
{
    /// <summary>
    /// 検索する文字列を取得します。
    /// </summary>
    public string SearchText { get; }

    /// <summary>
    /// 正規表現を使用するかどうかを取得します。
    /// </summary>
    public bool IsRegex { get; }

    /// <summary>
    /// 検索条件を初期化します。
    /// </summary>
    /// <param name="searchText">検索する文字列</param>
    /// <param name="isRegex">正規表現を使用するかどうか</param>
    public SearchCondition(string searchText, bool isRegex)
    {
        SearchText = searchText ?? throw new ArgumentNullException(nameof(searchText));
        IsRegex = isRegex;
    }
}
