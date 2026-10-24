using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;

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

        // ReplaceAllはCommit 19-3で実装します。
        throw new NotSupportedException("全件置換はCommit 19-3で実装します。");
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
