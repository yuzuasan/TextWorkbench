using TextWorkbench.Application.Exceptions;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.Services;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Tests.Presentation.Services;

/// <summary>
/// <see cref="DocumentContentProvider"/> の単体テストです。
/// </summary>
public class DocumentContentProviderTests
{
    // コンストラクタ

    [Fact]
    public void コンストラクタ_workspaceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentContentProvider(null!));
    }

    // GetDocument

    [Fact]
    public void GetDocument_IDが一致するタブのDocumentが返る()
    {
        var workspace = new Workspace();
        var tab = new DocumentTabViewModel(new Document());
        workspace.AddTab(tab);
        var provider = new DocumentContentProvider(workspace);

        var result = provider.GetDocument(tab.Document.Id);

        Assert.Same(tab.Document, result);
    }

    [Fact]
    public void GetDocument_複数タブ混在でIDが一致するタブのDocumentが返る()
    {
        var workspace = new Workspace();
        workspace.AddTab(new DocumentTabViewModel(new Document()));
        var target = new DocumentTabViewModel(new Document());
        workspace.AddTab(target);
        workspace.AddTab(CreateDiffTab());
        var provider = new DocumentContentProvider(workspace);

        var result = provider.GetDocument(target.Document.Id);

        Assert.Same(target.Document, result);
    }

    [Fact]
    public void GetDocument_無題Documentも取得できる()
    {
        var workspace = new Workspace();
        var tab = new DocumentTabViewModel(new Document());
        workspace.AddTab(tab);
        var provider = new DocumentContentProvider(workspace);

        var result = provider.GetDocument(tab.Document.Id);

        Assert.Null(result.FilePath);
        Assert.Equal(tab.Document, result);
    }

    [Fact]
    public void GetDocument_タブ削除後はDocumentNotFoundExceptionが発生する()
    {
        var workspace = new Workspace();
        var tab = new DocumentTabViewModel(new Document());
        workspace.AddTab(tab);
        workspace.RemoveTab(tab);
        var provider = new DocumentContentProvider(workspace);

        Assert.Throws<DocumentNotFoundException>(() => provider.GetDocument(tab.Document.Id));
    }

    // GetDisplayTitle

    [Fact]
    public void GetDisplayTitle_ファイルありタブの表示タイトルが返る()
    {
        var workspace = new Workspace();
        var tab = CreateDocumentTab(@"C:\work\a.txt");
        workspace.AddTab(tab);
        var provider = new DocumentContentProvider(workspace);

        var result = provider.GetDisplayTitle(tab.Document.Id);

        Assert.Equal(tab.DisplayTitle, result);
    }

    [Fact]
    public void GetDisplayTitle_無題タブの表示タイトルが返る()
    {
        var workspace = new Workspace();
        var tab = new DocumentTabViewModel(new Document());
        workspace.AddTab(tab);
        var provider = new DocumentContentProvider(workspace);

        var result = provider.GetDisplayTitle(tab.Document.Id);

        Assert.Equal(tab.DisplayTitle, result);
    }

    // 見つからない場合

    [Fact]
    public void GetDocument_Workspaceが空の場合_DocumentNotFoundExceptionが発生する()
    {
        var provider = new DocumentContentProvider(new Workspace());

        Assert.Throws<DocumentNotFoundException>(() => provider.GetDocument(Guid.NewGuid()));
    }

    [Fact]
    public void GetDisplayTitle_Workspaceが空の場合_DocumentNotFoundExceptionが発生する()
    {
        var provider = new DocumentContentProvider(new Workspace());

        Assert.Throws<DocumentNotFoundException>(() => provider.GetDisplayTitle(Guid.NewGuid()));
    }

    [Fact]
    public void GetDocument_存在しないIDの場合_DocumentNotFoundExceptionが発生する()
    {
        var workspace = new Workspace();
        workspace.AddTab(new DocumentTabViewModel(new Document()));
        var provider = new DocumentContentProvider(workspace);

        Assert.Throws<DocumentNotFoundException>(() => provider.GetDocument(Guid.NewGuid()));
    }

    [Fact]
    public void GetDocument_DiffTabのみ存在する場合_DocumentNotFoundExceptionが発生する()
    {
        var workspace = new Workspace();
        workspace.AddTab(CreateDiffTab());
        var provider = new DocumentContentProvider(workspace);

        Assert.Throws<DocumentNotFoundException>(() => provider.GetDocument(Guid.NewGuid()));
        Assert.Throws<DocumentNotFoundException>(() => provider.GetDisplayTitle(Guid.NewGuid()));
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
