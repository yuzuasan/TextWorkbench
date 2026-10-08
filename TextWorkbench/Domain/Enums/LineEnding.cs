namespace TextWorkbench.Domain.Enums;

/// <summary>
/// テキストファイルの改行コードを表します。
/// </summary>
public enum LineEnding
{
    /// <summary>
    /// CRLF（Windows標準）
    /// </summary>
    CrLf,

    /// <summary>
    /// LF（Unix/Linux標準）
    /// </summary>
    Lf,

    /// <summary>
    /// CR（古いMac標準）
    /// </summary>
    Cr
}
