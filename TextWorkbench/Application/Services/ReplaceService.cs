using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using System.Text;

namespace TextWorkbench.Application.Services;

/// <summary>
/// 通常の文字列置換を提供します。
/// </summary>
public sealed class ReplaceService : IReplaceService
{
    /// <inheritdoc />
    public ReplaceResult ReplaceFirst(
        string content,
        SearchCondition condition,
        string replacement,
        int startIndex)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(replacement);

        // Regex置換はStep 22で実装します。通常文字列として誤処理しないよう明示的に拒否します。
        if (condition.IsRegex)
            throw new NotSupportedException("正規表現置換は後続ステップで実装します。");

        var searchText = condition.SearchText;
        if (searchText.Length == 0 || startIndex < 0 || startIndex > content.Length)
            return CreateNoReplacementResult(content);

        var matchIndex = content.IndexOf(
            searchText,
            startIndex,
            StringComparison.OrdinalIgnoreCase);

        if (matchIndex < 0)
            return CreateNoReplacementResult(content);

        var replacedContent =
            content[..matchIndex]
            + replacement
            + content[(matchIndex + searchText.Length)..];

        return new ReplaceResult(
            replacedContent,
            replacementCount: 1,
            firstMatchStartIndex: matchIndex,
            firstMatchLength: searchText.Length,
            replacementLength: replacement.Length);
    }

    /// <inheritdoc />
    public ReplaceResult ReplaceAll(
        string content,
        SearchCondition condition,
        string replacement)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(replacement);

        // Regex置換はStep 22で実装します。通常文字列として誤処理しないよう明示的に拒否します。
        if (condition.IsRegex)
            throw new NotSupportedException("正規表現置換は後続ステップで実装します。");

        var searchText = condition.SearchText;
        if (searchText.Length == 0 || content.Length == 0)
            return CreateNoReplacementResult(content);

        var result = new StringBuilder(content.Length);
        var searchIndex = 0;
        var copiedUntil = 0;
        var replacementCount = 0;
        int? firstMatchStartIndex = null;

        while (searchIndex <= content.Length - searchText.Length)
        {
            var matchIndex = content.IndexOf(
                searchText,
                searchIndex,
                StringComparison.OrdinalIgnoreCase);

            if (matchIndex < 0)
                break;

            result.Append(content, copiedUntil, matchIndex - copiedUntil);
            result.Append(replacement);

            if (replacementCount == 0)
                firstMatchStartIndex = matchIndex;

            replacementCount++;

            // 通常検索と同じく、一致した文字列の直後から次を探します。
            // replacement内に検索文字列が含まれていても、置換した箇所を再検索しません。
            searchIndex = matchIndex + searchText.Length;
            copiedUntil = searchIndex;
        }

        if (replacementCount == 0)
            return CreateNoReplacementResult(content);

        result.Append(content, copiedUntil, content.Length - copiedUntil);

        return new ReplaceResult(
            result.ToString(),
            replacementCount,
            firstMatchStartIndex,
            firstMatchLength: searchText.Length,
            replacementLength: replacement.Length);
    }

    private static ReplaceResult CreateNoReplacementResult(string content)
    {
        return new ReplaceResult(
            content,
            replacementCount: 0,
            firstMatchStartIndex: null,
            firstMatchLength: null,
            replacementLength: null);
    }
}
