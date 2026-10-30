using System.Text.RegularExpressions;
using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.Services;

/// <summary>
/// テキスト検索を提供します。
/// </summary>
public sealed class SearchService : ISearchService
{
    /// <inheritdoc />
    public IReadOnlyList<SearchResult> Search(string content, SearchCondition condition)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(condition);

        var searchText = condition.SearchText;
        if (searchText.Length == 0 || content.Length == 0)
            return Array.Empty<SearchResult>();

        if (condition.IsRegex)
        {
            return SearchWithRegex(content, searchText);
        }

        return SearchWithString(content, searchText);
    }

    /// <summary>
    /// 通常の文字列検索を実行します。
    /// </summary>
    private static IReadOnlyList<SearchResult> SearchWithString(string content, string searchText)
    {
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

    /// <summary>
    /// 正規表現検索を実行します。
    /// </summary>
    private static IReadOnlyList<SearchResult> SearchWithRegex(string content, string pattern)
    {
        var results = new List<SearchResult>();
        var searchIndex = 0;

        try
        {
            // 複数行モードと大文字小文字を区別しない検索を常時有効とする
            var regex = new Regex(pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (Match match in regex.Matches(content))
            {
                // Zero-length match防止：検索位置を進める
                if (match.Index < searchIndex)
                    continue;

                results.Add(new SearchResult(match.Index, match.Length));

                // Zero-length matchの場合は1文字進める
                searchIndex = match.Index + Math.Max(match.Length, 1);
            }
        }
        catch (ArgumentException ex)
        {
            throw new InvalidRegexException("正規表現が無効です。", ex);
        }

        return results;
    }
}
