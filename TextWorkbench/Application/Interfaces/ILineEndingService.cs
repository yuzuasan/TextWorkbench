using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// 改行コードサービスのインターフェースです。
/// </summary>
public interface ILineEndingService
{
    /// <summary>
    /// テキストから改行コードを検出します。
    /// </summary>
    /// <param name="text">テキスト</param>
    /// <returns>検出された改行コード</returns>
    LineEnding Detect(string text);

    /// <summary>
    /// テキストの改行コードをLFに正規化します。
    /// </summary>
    /// <param name="text">テキスト</param>
    /// <returns>LFに正規化されたテキスト</returns>
    string NormalizeToLf(string text);

    /// <summary>
    /// LFのテキストを指定の改行コードへ変換します。
    /// </summary>
    /// <param name="text">LFのテキスト</param>
    /// <param name="lineEnding">改行コード</param>
    /// <returns>変換後のテキスト</returns>
    string ConvertFromLf(string text, LineEnding lineEnding);
}
