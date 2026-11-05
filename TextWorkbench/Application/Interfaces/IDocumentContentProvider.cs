using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// 開いているDocumentの内容を取得するためのインターフェースです。
/// </summary>
public interface IDocumentContentProvider
{
    /// <summary>
    /// 指定したDocument.Idに一致するDocumentを取得します。
    /// </summary>
    /// <param name="documentId">Document.Id</param>
    /// <returns>取得したDocument</returns>
    /// <exception cref="DocumentNotFoundException">指定したIDのDocumentが見つからない場合</exception>
    Document GetDocument(Guid documentId);

    /// <summary>
    /// 指定したDocument.Idに一致するDocumentの表示タイトルを取得します。
    /// </summary>
    /// <param name="documentId">Document.Id</param>
    /// <returns>表示タイトル</returns>
    /// <exception cref="DocumentNotFoundException">指定したIDのDocumentが見つからない場合</exception>
    string GetDisplayTitle(Guid documentId);
}
