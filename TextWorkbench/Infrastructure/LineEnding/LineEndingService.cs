using TextWorkbench.Application.Interfaces;

namespace TextWorkbench.Infrastructure.LineEnding;

/// <summary>
/// 改行コード検出・変換サービスの実装です。
/// </summary>
public sealed class LineEndingService : ILineEndingService
{
    /// <summary>
    /// テキストから改行コードを検出します。
    /// </summary>
    /// <param name="text">テキスト</param>
    /// <returns>検出された改行コード</returns>
    public Domain.Enums.LineEnding Detect(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        // 最初の改行コードを検出
        var crLfIndex = text.IndexOf("\r\n", StringComparison.Ordinal);
        var lfIndex = text.IndexOf('\n');
        var crIndex = text.IndexOf('\r');

        // 最初の改行コードを決定
        var firstCrLf = crLfIndex >= 0 ? crLfIndex : int.MaxValue;
        var firstLf = lfIndex >= 0 ? lfIndex : int.MaxValue;
        var firstCr = crIndex >= 0 ? crIndex : int.MaxValue;

        // CRはCRLFの一部として扱われるため、CRLFがあれば優先
        if (firstCrLf <= firstLf && firstCrLf <= firstCr)
        {
            return Domain.Enums.LineEnding.CrLf;
        }

        if (firstLf < firstCr)
        {
            return Domain.Enums.LineEnding.Lf;
        }

        if (firstCr < firstLf)
        {
            return Domain.Enums.LineEnding.Cr;
        }

        // 改行が1つも存在しない場合はCRLFとする
        return Domain.Enums.LineEnding.CrLf;
    }

    /// <summary>
    /// テキストの改行コードをLFに正規化します。
    /// </summary>
    /// <param name="text">テキスト</param>
    /// <returns>LFに正規化されたテキスト</returns>
    public string NormalizeToLf(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        // CRLFをLFに変換
        var normalized = text.Replace("\r\n", "\n");
        // CRをLFに変換
        normalized = normalized.Replace("\r", "\n");

        return normalized;
    }

    /// <summary>
    /// LFのテキストを指定の改行コードへ変換します。
    /// </summary>
    /// <param name="text">LFのテキスト</param>
    /// <param name="lineEnding">改行コード</param>
    /// <returns>変換後のテキスト</returns>
    public string ConvertFromLf(string text, Domain.Enums.LineEnding lineEnding)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        return lineEnding switch
        {
            Domain.Enums.LineEnding.CrLf => text.Replace("\n", "\r\n"),
            Domain.Enums.LineEnding.Lf => text,
            Domain.Enums.LineEnding.Cr => text.Replace("\n", "\r"),
            _ => throw new ArgumentException("対応していない改行コードです。", nameof(lineEnding))
        };
    }
}
