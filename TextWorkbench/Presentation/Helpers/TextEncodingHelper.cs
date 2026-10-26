using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Presentation.Helpers;

/// <summary>
/// TextEncodingの表示文字列変換ヘルパークラスです。
/// </summary>
public static class TextEncodingHelper
{
    /// <summary>
    /// TextEncodingを表示文字列に変換します。
    /// </summary>
    /// <param name="encoding">文字コード</param>
    /// <returns>表示文字列</returns>
    public static string ToDisplayString(TextEncoding encoding)
    {
        return encoding switch
        {
            TextEncoding.Utf8 => "UTF-8",
            TextEncoding.Utf8Bom => "UTF-8 BOM",
            TextEncoding.ShiftJis => "Shift-JIS",
            _ => "Unknown"
        };
    }
}
