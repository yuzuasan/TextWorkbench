using TextWorkbench.Application.Models;

namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// 開いているDocumentを取得するためのインターフェースです。
/// </summary>
public interface IOpenDocumentProvider
{
    /// <summary>
    /// 開いているDocumentをタブ順で取得します。
    /// </summary>
    /// <returns>開いているDocumentの情報一覧</returns>
    IReadOnlyList<OpenDocumentInfo> GetOpenDocuments();
}
