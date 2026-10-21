using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.Services;

/// <summary>
/// 通常の文字列検索を提供します。
/// </summary>
public sealed class SearchService : ISearchService
{
    /// <inheritdoc />
    public IReadOnlyList<SearchResult> Search(string content, SearchCondition condition)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(condition);

        // Step 16では通常検索のみを実装します。
        // Regex条件の処理は後続ステップで追加します。
        if (condition.IsRegex)
            throw new NotSupportedException("正規表現検索は後続ステップで実装します。");

        var searchText = condition.SearchText;
        if (searchText.Length == 0 || content.Length == 0)
            return Array.Empty<SearchResult>();

        var results = new List<SearchResult>();
        var searchIndex = 0;

        while (searchIndex <= content.Length - searchText.Length)
        {
            var matchIndex = content.IndexOf(
                searchText,
                searchIndex,
                StringComparison.OrdinalIgnoreCase);

            if (matchIndex < 0)
                break;

            results.Add(new SearchResult(matchIndex, searchText.Length));

            // 通常検索では一致した文字列の直後から次の検索を続けます。
            searchIndex = matchIndex + searchText.Length;
        }

        return results;
    }
}
