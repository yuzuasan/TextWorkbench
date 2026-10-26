using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Presentation.Helpers;

/// <summary>
/// LineEndingの表示文字列変換ヘルパークラスです。
/// </summary>
public static class LineEndingHelper
{
    /// <summary>
    /// LineEndingを表示文字列に変換します。
    /// </summary>
    /// <param name="lineEnding">改行コード</param>
    /// <returns>表示文字列</returns>
    public static string ToDisplayString(LineEnding lineEnding)
    {
        return lineEnding switch
        {
            LineEnding.CrLf => "CRLF",
            LineEnding.Lf => "LF",
            LineEnding.Cr => "CR",
            _ => "Unknown"
        };
    }
}
