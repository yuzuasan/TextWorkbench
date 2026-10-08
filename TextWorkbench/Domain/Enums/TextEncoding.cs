namespace TextWorkbench.Domain.Enums;

/// <summary>
/// テキストファイルの文字コードを表します。
/// </summary>
public enum TextEncoding
{
    /// <summary>
    /// UTF-8（BOMなし）
    /// </summary>
    Utf8,

    /// <summary>
    /// UTF-8 BOM付き
    /// </summary>
    Utf8Bom,

    /// <summary>
    /// Shift-JIS
    /// </summary>
    ShiftJis
}
