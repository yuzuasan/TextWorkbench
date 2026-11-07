using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.Models;

/// <summary>
/// 開いているDocumentと表示タイトルを表すデータ型です。
/// </summary>
public sealed class OpenDocumentInfo
{
    /// <summary>
    /// Documentを取得します。
    /// </summary>
    public Document Document { get; }

    /// <summary>
    /// 表示タイトルを取得します。
    /// </summary>
    public string DisplayTitle { get; }

    /// <summary>
    /// <see cref="OpenDocumentInfo"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="document">Document</param>
    /// <param name="displayTitle">表示タイトル</param>
    public OpenDocumentInfo(Document document, string displayTitle)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        DisplayTitle = displayTitle ?? throw new ArgumentNullException(nameof(displayTitle));
    }
}
