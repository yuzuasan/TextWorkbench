using TextWorkbench.Application.Models;

namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// ファイルシステムへのアクセスを提供するサービスのインターフェースです。
/// </summary>
public interface IFileService
{
    /// <summary>
    /// ファイルの全バイトを読み込みます。
    /// </summary>
    /// <param name="filePath">ファイルパス</param>
    /// <returns>読み込み結果</returns>
    FileReadResult ReadAllBytes(string filePath);

    /// <summary>
    /// 全バイトをファイルに書き込みます。
    /// </summary>
    /// <param name="filePath">ファイルパス</param>
    /// <param name="bytes">書き込むバイト列</param>
    void WriteAllBytes(string filePath, byte[] bytes);
}
