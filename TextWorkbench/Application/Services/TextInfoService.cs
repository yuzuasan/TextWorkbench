using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.Services;

/// <summary>
/// テキスト情報の取得を提供するサービスの実装です。
/// </summary>
public sealed class TextInfoService : ITextInfoService
{
    private readonly IEncodingService _encodingService;
    private readonly ILineEndingService _lineEndingService;

    /// <summary>
    /// <see cref="TextInfoService"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="encodingService">エンコーディングサービス</param>
    /// <param name="lineEndingService">改行コードサービス</param>
    public TextInfoService(
        IEncodingService encodingService,
        ILineEndingService lineEndingService)
    {
        _encodingService = encodingService ?? throw new ArgumentNullException(nameof(encodingService));
        _lineEndingService = lineEndingService ?? throw new ArgumentNullException(nameof(lineEndingService));
    }

    /// <inheritdoc />
    public TextInfo GetTextInfo(string content, TextEncoding encoding, LineEnding lineEnding)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        // LineCount計算（LF基準、論理行数）
        var lineCount = CalculateLineCount(content);

        // CharacterCount計算
        var characterCount = CalculateCharacterCount(content);

        // ByteCount計算（EncodingとLineEndingを考慮）
        var byteCount = CalculateByteCount(content, encoding, lineEnding);

        return new TextInfo(lineCount, characterCount, byteCount, encoding, lineEnding);
    }

    /// <summary>
    /// 論理行数を計算します（LF基準）。
    /// </summary>
    private static int CalculateLineCount(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 1;

        var count = 1;
        foreach (var c in content)
        {
            if (c == '\n')
                count++;
        }

        return count;
    }

    /// <summary>
    /// 文字列のテキスト要素数を計算します。
    /// </summary>
    private static int CalculateCharacterCount(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        return new System.Globalization.StringInfo(content).LengthInTextElements;
    }

    /// <summary>
    /// バイト数を計算します（EncodingとLineEndingを考慮）。
    /// </summary>
    private int CalculateByteCount(string content, TextEncoding encoding, LineEnding lineEnding)
    {
        // LF → Document.LineEnding変換
        var contentForSave = _lineEndingService.ConvertFromLf(content, lineEnding);

        // Encodingでバイト列に変換
        var bytes = _encodingService.Encode(contentForSave, encoding);

        return bytes.Length;
    }
}
