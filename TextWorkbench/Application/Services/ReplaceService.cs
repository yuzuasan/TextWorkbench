using System.Text;
using System.Text.RegularExpressions;
using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;

namespace TextWorkbench.Application.Services;

/// <summary>
/// テキスト置換を提供します。
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

        var searchText = condition.SearchText;
        if (searchText.Length == 0 || startIndex < 0 || startIndex > content.Length)
            return CreateNoReplacementResult(content);

        if (condition.IsRegex)
        {
            return ReplaceFirstWithRegex(content, searchText, replacement, startIndex);
        }

        return ReplaceFirstWithString(content, searchText, replacement, startIndex);
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

        var searchText = condition.SearchText;
        if (searchText.Length == 0 || content.Length == 0)
            return CreateNoReplacementResult(content);

        if (condition.IsRegex)
        {
            return ReplaceAllWithRegex(content, searchText, replacement);
        }

        return ReplaceAllWithString(content, searchText, replacement);
    }

    /// <summary>
    /// 通常の文字列置換で最初の一致を置換します。
    /// </summary>
    private static ReplaceResult ReplaceFirstWithString(
        string content,
        string searchText,
        string replacement,
        int startIndex)
    {
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

    /// <summary>
    /// 正規表現置換で最初の一致を置換します。
    /// </summary>
    private static ReplaceResult ReplaceFirstWithRegex(
        string content,
        string pattern,
        string replacement,
        int startIndex)
    {
        try
        {
            // RegexOptions.Multilineを常時有効とする
            var regex = new Regex(pattern, RegexOptions.Multiline);

            // startIndex以降で最初の一致を検索
            var match = regex.Match(content, startIndex);
            if (!match.Success)
                return CreateNoReplacementResult(content);

            var replacedContent = content[..match.Index]
                + match.Result(replacement)
                + content[(match.Index + match.Length)..];

            return new ReplaceResult(
                replacedContent,
                replacementCount: 1,
                firstMatchStartIndex: match.Index,
                firstMatchLength: match.Length,
                replacementLength: match.Result(replacement).Length);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidRegexException("正規表現が無効です。", ex);
        }
    }

    /// <summary>
    /// 通常の文字列置換ですべての一致を置換します。
    /// </summary>
    private static ReplaceResult ReplaceAllWithString(
        string content,
        string searchText,
        string replacement)
    {
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

    /// <summary>
    /// 正規表現置換ですべての一致を置換します。
    /// </summary>
    private static ReplaceResult ReplaceAllWithRegex(
        string content,
        string pattern,
        string replacement)
    {
        try
        {
            // RegexOptions.Multilineを常時有効とする
            var regex = new Regex(pattern, RegexOptions.Multiline);

            var matches = regex.Matches(content);
            if (matches.Count == 0)
                return CreateNoReplacementResult(content);

            var replacedContent = regex.Replace(content, replacement);

            return new ReplaceResult(
                replacedContent,
                replacementCount: matches.Count,
                firstMatchStartIndex: matches[0].Index,
                firstMatchLength: matches[0].Length,
                replacementLength: replacement.Length);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidRegexException("正規表現が無効です。", ex);
        }
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
