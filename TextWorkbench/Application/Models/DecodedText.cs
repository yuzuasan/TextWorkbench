using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Application.Models;

/// <summary>
/// デコードしたテキストと、そのテキストの文字コードを表します。
/// </summary>
public sealed class DecodedText
{
    /// <summary>
    /// デコードしたテキストを取得します。
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// デコードに使用した文字コードを取得します。
    /// </summary>
    public TextEncoding Encoding { get; }

    /// <summary>
    /// <see cref="DecodedText"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="text">デコードしたテキスト</param>
    /// <param name="encoding">デコードに使用した文字コード</param>
    public DecodedText(string text, TextEncoding encoding)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Encoding = encoding;
    }
}
