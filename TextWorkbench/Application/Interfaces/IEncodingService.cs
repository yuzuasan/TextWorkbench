using TextWorkbench.Application.Models;
using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// テキストとバイト列の間の文字コード変換を提供するサービスのインターフェースです。
/// </summary>
public interface IEncodingService
{
    /// <summary>
    /// バイト列をテキストへデコードします。
    /// </summary>
    /// <param name="bytes">文字コードを判定してデコードするバイト列</param>
    /// <returns>デコードしたテキストと判定した文字コード</returns>
    DecodedText Decode(byte[] bytes);

    /// <summary>
    /// テキストを指定された文字コードのバイト列へエンコードします。
    /// </summary>
    /// <param name="text">エンコードするテキスト</param>
    /// <param name="encoding">エンコードに使用する文字コード</param>
    /// <returns>エンコードしたバイト列</returns>
    byte[] Encode(string text, TextEncoding encoding);
}
