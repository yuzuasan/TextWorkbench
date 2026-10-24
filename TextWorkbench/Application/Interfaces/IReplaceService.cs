using TextWorkbench.Application.Models;

namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// テキスト置換機能を提供するサービスのインターフェースです。
/// </summary>
public interface IReplaceService
{
    /// <summary>
    /// 指定された開始位置以降で最初に一致した箇所を置換します。
    /// </summary>
    /// <param name="content">置換対象のテキスト</param>
    /// <param name="condition">検索条件</param>
    /// <param name="replacement">置換後の文字列</param>
    /// <param name="startIndex">検索開始位置</param>
    /// <returns>置換後のテキストと置換情報</returns>
    ReplaceResult ReplaceFirst(
        string content,
        SearchCondition condition,
        string replacement,
        int startIndex);

    /// <summary>
    /// テキスト全体を対象に一致した箇所をすべて置換します。
    /// </summary>
    /// <param name="content">置換対象のテキスト</param>
    /// <param name="condition">検索条件</param>
    /// <param name="replacement">置換後の文字列</param>
    /// <returns>置換後のテキストと置換情報</returns>
    ReplaceResult ReplaceAll(
        string content,
        SearchCondition condition,
        string replacement);
}
