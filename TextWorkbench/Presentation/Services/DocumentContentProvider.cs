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
        foreach (var tab in _workspace.Tabs)
        {
            if (tab is DocumentTabViewModel documentTab &&
                documentTab.Document.Id == documentId)
            {
                return documentTab.Document;
            }
        }

        throw new InvalidOperationException($"Document with Id {documentId} not found.");
    }
}
