using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Presentation.Services;

/// <summary>
/// IDocumentActivationServiceの実装です。
/// Document IDに対応するタブをWorkspaceのアクティブタブに設定します。
/// </summary>
public sealed class WorkspaceDocumentActivationService : IDocumentActivationService
{
    private readonly Workspace _workspace;

    /// <summary>
    /// <see cref="WorkspaceDocumentActivationService"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="workspace">Workspace</param>
    public WorkspaceDocumentActivationService(Workspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    /// <inheritdoc />
    public void ActivateDocument(Guid documentId)
    {
        foreach (var tab in _workspace.Tabs)
        {
            if (tab is DocumentTabViewModel documentTab
                && documentTab.Document.Id == documentId)
            {
                _workspace.SetActiveTab(documentTab);
                return;
            }
        }

        throw new DocumentNotFoundException(
            $"ID {documentId} のドキュメントが見つかりませんでした。");
    }
}
