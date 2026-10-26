using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// テキスト情報の取得を提供するサービスのインターフェースです。
/// </summary>
public interface ITextInfoService
{
    /// <summary>
    /// Documentの内容からTextInfoを生成します。
    /// </summary>
    /// <param name="content">テキスト内容</param>
    /// <param name="encoding">文字コード</param>
    /// <param name="lineEnding">改行コード</param>
    /// <returns>テキスト情報</returns>
    TextInfo GetTextInfo(string content, TextEncoding encoding, LineEnding lineEnding);
}
