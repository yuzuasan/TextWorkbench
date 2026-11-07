using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Presentation.Services;

/// <summary>
/// IOpenDocumentProviderの実装です。
/// Workspace内のDocumentTabをタブ順で返します。
/// </summary>
public sealed class WorkspaceOpenDocumentProvider : IOpenDocumentProvider
{
    private readonly Workspace _workspace;

    /// <summary>
    /// <see cref="WorkspaceOpenDocumentProvider"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="workspace">Workspace</param>
    public WorkspaceOpenDocumentProvider(Workspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    /// <inheritdoc />
    public IReadOnlyList<OpenDocumentInfo> GetOpenDocuments()
    {
        var documents = new List<OpenDocumentInfo>();

        foreach (var tab in _workspace.Tabs)
        {
            if (tab is DocumentTabViewModel documentTab)
            {
                documents.Add(new OpenDocumentInfo(
                    documentTab.Document,
                    documentTab.DisplayTitle));
            }
        }

        return documents;
    }
}
