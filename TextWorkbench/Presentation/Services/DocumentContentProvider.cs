using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Presentation.Services;

/// <summary>
/// IDocumentContentProviderの実装です。
/// </summary>
public sealed class DocumentContentProvider : IDocumentContentProvider
{
    private readonly Workspace _workspace;

    /// <summary>
    /// <see cref="DocumentContentProvider"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="workspace">Workspace</param>
    public DocumentContentProvider(Workspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    /// <inheritdoc />
    public Document GetDocument(Guid documentId)
    {
        var documentTab = FindDocumentTab(documentId);
        return documentTab.Document;
    }

    /// <inheritdoc />
    public string GetDisplayTitle(Guid documentId)
    {
        var documentTab = FindDocumentTab(documentId);
        return documentTab.DisplayTitle;
    }

    /// <summary>
    /// 指定したDocument.Idに一致するDocumentTabViewModelを検索します。
    /// </summary>
    /// <param name="documentId">Document.Id</param>
    /// <returns>見つかったDocumentTabViewModel</returns>
    /// <exception cref="InvalidOperationException">指定したIDのDocumentが見つからない場合</exception>
    private DocumentTabViewModel FindDocumentTab(Guid documentId)
    {
        foreach (var tab in _workspace.Tabs)
        {
            if (tab is DocumentTabViewModel documentTab &&
                documentTab.Document.Id == documentId)
            {
                return documentTab;
            }
        }

        throw new InvalidOperationException($"Document with Id {documentId} not found.");
    }
}
