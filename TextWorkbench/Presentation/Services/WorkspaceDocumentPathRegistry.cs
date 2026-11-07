using TextWorkbench.Application.Interfaces;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Presentation.Services;

/// <summary>
/// IDocumentPathRegistryの実装です。
/// Workspace内のDocumentが使用しているファイルパスを確認します。
/// </summary>
public sealed class WorkspaceDocumentPathRegistry : IDocumentPathRegistry
{
    private readonly Workspace _workspace;

    /// <summary>
    /// <see cref="WorkspaceDocumentPathRegistry"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="workspace">Workspace</param>
    public WorkspaceDocumentPathRegistry(Workspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    /// <inheritdoc />
    public bool IsPathInUse(string filePath, Guid excludingDocumentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        foreach (var tab in _workspace.Tabs)
        {
            if (tab is not DocumentTabViewModel documentTab)
            {
                continue;
            }

            var document = documentTab.Document;

            // 除外対象のDocumentは重複確認しない
            if (document.Id == excludingDocumentId)
            {
                continue;
            }

            if (document.FilePath != null
                && string.Equals(
                    document.FilePath,
                    filePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
