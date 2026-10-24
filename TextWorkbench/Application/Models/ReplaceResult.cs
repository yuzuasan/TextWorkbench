namespace TextWorkbench.Application.Models;

/// <summary>
/// テキスト置換の結果を表します。
/// </summary>
public sealed class ReplaceResult
{
    /// <summary>
    /// 置換後のテキストを取得します。
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// 置換した件数を取得します。
    /// </summary>
    public int ReplacementCount { get; }

    /// <summary>
    /// 最初に置換した箇所の開始インデックスを取得します。
    /// </summary>
    public int? FirstMatchStartIndex { get; }

    /// <summary>
    /// 最初に置換した箇所の一致長を取得します。
    /// </summary>
    public int? FirstMatchLength { get; }

    /// <summary>
    /// 最初に置換した箇所に挿入した文字列の長さを取得します。
    /// </summary>
    public int? ReplacementLength { get; }

    /// <summary>
    /// <see cref="ReplaceResult"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="content">置換後のテキスト</param>
    /// <param name="replacementCount">置換した件数</param>
    /// <param name="firstMatchStartIndex">最初に置換した箇所の開始インデックス。置換がない場合は null</param>
    /// <param name="firstMatchLength">最初に置換した箇所の一致長。置換がない場合は null</param>
    /// <param name="replacementLength">最初に置換した箇所に挿入した文字列の長さ。置換がない場合は null</param>
    public ReplaceResult(
        string content,
        int replacementCount,
        int? firstMatchStartIndex,
        int? firstMatchLength,
        int? replacementLength)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));

        if (replacementCount < 0)
            throw new ArgumentOutOfRangeException(nameof(replacementCount), "置換件数は0以上である必要があります。");

        if (firstMatchStartIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(firstMatchStartIndex), "開始インデックスは0以上である必要があります。");

        if (firstMatchLength < 0)
            throw new ArgumentOutOfRangeException(nameof(firstMatchLength), "一致長は0以上である必要があります。");

        if (replacementLength < 0)
            throw new ArgumentOutOfRangeException(nameof(replacementLength), "置換後の長さは0以上である必要があります。");

        ReplacementCount = replacementCount;
        FirstMatchStartIndex = firstMatchStartIndex;
        FirstMatchLength = firstMatchLength;
        ReplacementLength = replacementLength;
    }
}
