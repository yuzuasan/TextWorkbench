using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.Services;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Tests.Presentation.Services;

/// <summary>
/// <see cref="WorkspaceDocumentPathRegistry"/> の単体テストです。
/// </summary>
public class WorkspaceDocumentPathRegistryTests
{
    // コンストラクタ・ガード

    [Fact]
    public void コンストラクタ_workspaceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkspaceDocumentPathRegistry(null!));
    }

    [Fact]
    public void IsPathInUse_filePathがnullの場合_ArgumentNullExceptionが発生する()
    {
        var registry = new WorkspaceDocumentPathRegistry(new Workspace());

        Assert.Throws<ArgumentNullException>(() => registry.IsPathInUse(null!, Guid.NewGuid()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsPathInUse_filePathが空または空白の場合_ArgumentExceptionが発生する(string path)
    {
        var registry = new WorkspaceDocumentPathRegistry(new Workspace());

        Assert.Throws<ArgumentException>(() => registry.IsPathInUse(path, Guid.NewGuid()));
    }

    // 重複判定

    [Fact]
    public void IsPathInUse_同一パスのDocumentが存在する場合_trueが返る()
    {
        var workspace = new Workspace();
        workspace.AddTab(CreateDocumentTab(@"C:\work\a.txt"));
        var registry = new WorkspaceDocumentPathRegistry(workspace);

        Assert.True(registry.IsPathInUse(@"C:\work\a.txt", Guid.NewGuid()));
    }

    [Fact]
    public void IsPathInUse_異なるパスの場合_falseが返る()
    {
        var workspace = new Workspace();
        workspace.AddTab(CreateDocumentTab(@"C:\work\a.txt"));
        var registry = new WorkspaceDocumentPathRegistry(workspace);

        Assert.False(registry.IsPathInUse(@"C:\work\b.txt", Guid.NewGuid()));
    }

    [Fact]
    public void IsPathInUse_大文字小文字が異なっても一致と判定される()
    {
        var workspace = new Workspace();
        workspace.AddTab(CreateDocumentTab(@"C:\work\a.txt"));
        var registry = new WorkspaceDocumentPathRegistry(workspace);

        Assert.True(registry.IsPathInUse(@"C:\WORK\A.TXT", Guid.NewGuid()));
    }

    [Fact]
    public void IsPathInUse_無題Documentのみの場合_falseが返る()
    {
        var workspace = new Workspace();
        workspace.AddTab(new DocumentTabViewModel(new Document()));
        var registry = new WorkspaceDocumentPathRegistry(workspace);

        Assert.False(registry.IsPathInUse(@"C:\work\a.txt", Guid.NewGuid()));
    }

    // 除外・タブ種別

    [Fact]
    public void IsPathInUse_除外するDocumentIdのDocumentは重複とみなされない()
    {
        var workspace = new Workspace();
        var tab = CreateDocumentTab(@"C:\work\a.txt");
        workspace.AddTab(tab);
        var registry = new WorkspaceDocumentPathRegistry(workspace);

        Assert.False(registry.IsPathInUse(@"C:\work\a.txt", tab.Document.Id));
        Assert.True(registry.IsPathInUse(@"C:\work\a.txt", Guid.NewGuid()));
    }

    [Fact]
    public void IsPathInUse_DiffTabは対象外となる()
    {
        var workspace = new Workspace();
        workspace.AddTab(CreateDiffTab());
        var registry = new WorkspaceDocumentPathRegistry(workspace);

        Assert.False(registry.IsPathInUse(@"C:\work\a.txt", Guid.NewGuid()));
    }

    [Fact]
    public void IsPathInUse_複数Document混在で1件一致すればtrueが返る()
    {
        var workspace = new Workspace();
        workspace.AddTab(CreateDocumentTab(@"C:\work\a.txt"));
        workspace.AddTab(CreateDocumentTab(@"C:\work\b.txt"));
        workspace.AddTab(new DocumentTabViewModel(new Document()));
        var registry = new WorkspaceDocumentPathRegistry(workspace);

        Assert.True(registry.IsPathInUse(@"C:\work\b.txt", Guid.NewGuid()));
        Assert.False(registry.IsPathInUse(@"C:\work\c.txt", Guid.NewGuid()));
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
