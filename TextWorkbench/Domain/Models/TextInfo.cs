using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Domain.Models;

/// <summary>
/// テキストファイルの情報を表します。
/// </summary>
public sealed class TextInfo
{
    /// <summary>
    /// 行数を取得します。
    /// </summary>
    public int LineCount { get; }

    /// <summary>
    /// 文字数を取得します。
    /// </summary>
    public int CharacterCount { get; }

    /// <summary>
    /// バイト数を取得します。
    /// </summary>
    public int ByteCount { get; }

    /// <summary>
    /// 文字コードを取得します。
    /// </summary>
    public TextEncoding Encoding { get; }

    /// <summary>
    /// 改行コードを取得します。
    /// </summary>
    public LineEnding LineEnding { get; }

    /// <summary>
    /// <see cref="TextInfo"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="lineCount">行数</param>
    /// <param name="characterCount">文字数</param>
    /// <param name="byteCount">バイト数</param>
    /// <param name="encoding">文字コード</param>
    /// <param name="lineEnding">改行コード</param>
    public TextInfo(
        int lineCount,
        int characterCount,
        int byteCount,
        TextEncoding encoding,
        LineEnding lineEnding)
    {
        if (lineCount < 0)
            throw new ArgumentOutOfRangeException(nameof(lineCount), "行数は0以上である必要があります。");

        if (characterCount < 0)
            throw new ArgumentOutOfRangeException(nameof(characterCount), "文字数は0以上である必要があります。");

        if (byteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount), "バイト数は0以上である必要があります。");

        LineCount = lineCount;
        CharacterCount = characterCount;
        ByteCount = byteCount;
        Encoding = encoding;
        LineEnding = lineEnding;
    }
}
