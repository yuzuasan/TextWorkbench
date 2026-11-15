using TextWorkbench.Application.Exceptions;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.Services;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Tests.Presentation.Services;

/// <summary>
/// <see cref="WorkspaceDocumentActivationService"/> の単体テストです。
/// </summary>
public class WorkspaceDocumentActivationServiceTests
{
    // コンストラクタ

    [Fact]
    public void コンストラクタ_workspaceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkspaceDocumentActivationService(null!));
    }

    // アクティブ切替

    [Fact]
    public void ActivateDocument_IDが一致するタブがアクティブになる()
    {
        var workspace = new Workspace();
        var tab1 = new DocumentTabViewModel(new Document());
        var tab2 = new DocumentTabViewModel(new Document());
        workspace.AddTab(tab1);
        workspace.AddTab(tab2);
        var service = new WorkspaceDocumentActivationService(workspace);

        service.ActivateDocument(tab1.Document.Id);

        Assert.Same(tab1, workspace.ActiveTab);
    }

    [Fact]
    public void ActivateDocument_複数タブ混在でIDが一致するタブがアクティブになる()
    {
        var workspace = new Workspace();
        var tab1 = new DocumentTabViewModel(new Document());
        var target = CreateDocumentTab(@"C:\work\a.txt");
        var tab3 = new DocumentTabViewModel(new Document());
        workspace.AddTab(tab1);
        workspace.AddTab(target);
        workspace.AddTab(tab3);
        workspace.AddTab(CreateDiffTab());
        var service = new WorkspaceDocumentActivationService(workspace);

        service.ActivateDocument(target.Document.Id);

        Assert.Same(target, workspace.ActiveTab);
    }

    [Fact]
    public void ActivateDocument_無題Documentのタブもアクティブにできる()
    {
        var workspace = new Workspace();
        var tab = new DocumentTabViewModel(new Document());
        workspace.AddTab(tab);
        var service = new WorkspaceDocumentActivationService(workspace);

        service.ActivateDocument(tab.Document.Id);

        Assert.Same(tab, workspace.ActiveTab);
    }

    [Fact]
    public void ActivateDocument_既にアクティブなタブを指定しても変化しない()
    {
        var workspace = new Workspace();
        var tab = new DocumentTabViewModel(new Document());
        workspace.AddTab(tab);
        var service = new WorkspaceDocumentActivationService(workspace);

        service.ActivateDocument(tab.Document.Id);

        Assert.Same(tab, workspace.ActiveTab);
    }

    // 見つからない場合

    [Fact]
    public void ActivateDocument_Workspaceが空の場合_DocumentNotFoundExceptionが発生する()
    {
        var service = new WorkspaceDocumentActivationService(new Workspace());

        Assert.Throws<DocumentNotFoundException>(() => service.ActivateDocument(Guid.NewGuid()));
    }

    [Fact]
    public void ActivateDocument_存在しないIDの場合_例外が発生しアクティブタブは維持される()
    {
        var workspace = new Workspace();
        var tab = new DocumentTabViewModel(new Document());
        workspace.AddTab(tab);
        var service = new WorkspaceDocumentActivationService(workspace);

        Assert.Throws<DocumentNotFoundException>(() => service.ActivateDocument(Guid.NewGuid()));
        Assert.Same(tab, workspace.ActiveTab);
    }

    [Fact]
    public void ActivateDocument_DiffTabのみ存在する場合_例外が発生しアクティブタブは維持される()
    {
        var workspace = new Workspace();
        var diffTab = CreateDiffTab();
        workspace.AddTab(diffTab);
        var service = new WorkspaceDocumentActivationService(workspace);

        Assert.Throws<DocumentNotFoundException>(() => service.ActivateDocument(Guid.NewGuid()));
        Assert.Same(diffTab, workspace.ActiveTab);
    }

    [Fact]
    public void ActivateDocument_DocumentTabとDiffTab混在で不明IDの場合_例外が発生しアクティブタブは維持される()
    {
        var workspace = new Workspace();
        var tab = new DocumentTabViewModel(new Document());
        workspace.AddTab(tab);
        workspace.AddTab(CreateDiffTab());
        workspace.SetActiveTab(tab);
        var service = new WorkspaceDocumentActivationService(workspace);

        Assert.Throws<DocumentNotFoundException>(() => service.ActivateDocument(Guid.NewGuid()));
        Assert.Same(tab, workspace.ActiveTab);
    }

    // テストデータ作成ヘルパー

    private static DocumentTabViewModel CreateDocumentTab(string filePath)
    {
        var document = new Document(
            filePath, Path.GetFileName(filePath), "content",
            TextEncoding.Utf8, LineEnding.CrLf, false);
        return new DocumentTabViewModel(document);
    }

    private static DiffTabViewModel CreateDiffTab()
    {
        var result = new DiffResult("l", "r", Array.Empty<DiffLine>());
        var source = new DiffSession.DiffSource(
            SourceType.File, "id", @"C:\work\a.txt", "a.txt",
            TextEncoding.Utf8, "content");
        return new DiffTabViewModel(new DiffSession(source, source, result));
    }
}
