using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.Services;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Tests.Presentation.Services;

/// <summary>
/// <see cref="WorkspaceOpenDocumentProvider"/> の単体テストです。
/// </summary>
public class WorkspaceOpenDocumentProviderTests
{
    // コンストラクタ

    [Fact]
    public void コンストラクタ_workspaceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkspaceOpenDocumentProvider(null!));
    }

    // 基本取得

    [Fact]
    public void GetOpenDocuments_Workspaceが空の場合_空のリストが返る()
    {
        var provider = new WorkspaceOpenDocumentProvider(new Workspace());

        var result = provider.GetOpenDocuments();

        Assert.Empty(result);
    }

    [Fact]
    public void GetOpenDocuments_DocumentTabのみ存在する場合_全て取得できる()
    {
        var workspace = new Workspace();
        var tab1 = new DocumentTabViewModel(new Document());
        var tab2 = CreateDocumentTab(@"C:\work\a.txt");
        workspace.AddTab(tab1);
        workspace.AddTab(tab2);
        var provider = new WorkspaceOpenDocumentProvider(workspace);

        var result = provider.GetOpenDocuments();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetOpenDocuments_ファイルありと無題が混在する場合_両方取得できる()
    {
        var workspace = new Workspace();
        var untitled = new DocumentTabViewModel(new Document());
        var fileTab = CreateDocumentTab(@"C:\work\a.txt");
        workspace.AddTab(untitled);
        workspace.AddTab(fileTab);
        var provider = new WorkspaceOpenDocumentProvider(workspace);

        var result = provider.GetOpenDocuments();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, info => ReferenceEquals(info.Document, untitled.Document));
        Assert.Contains(result, info => ReferenceEquals(info.Document, fileTab.Document));
    }

    // タブ順

    [Fact]
    public void GetOpenDocuments_追加順どおりに返る()
    {
        var workspace = new Workspace();
        var tab1 = CreateDocumentTab(@"C:\work\a.txt");
        var tab2 = CreateDocumentTab(@"C:\work\b.txt");
        var tab3 = CreateDocumentTab(@"C:\work\c.txt");
        workspace.AddTab(tab1);
        workspace.AddTab(tab2);
        workspace.AddTab(tab3);
        var provider = new WorkspaceOpenDocumentProvider(workspace);

        var result = provider.GetOpenDocuments();

        Assert.Equal(3, result.Count);
        Assert.Same(tab1.Document, result[0].Document);
        Assert.Same(tab2.Document, result[1].Document);
        Assert.Same(tab3.Document, result[2].Document);
    }

    [Fact]
    public void GetOpenDocuments_DiffTabが挟まってもDocumentTabの順序が維持される()
    {
        var workspace = new Workspace();
        var tab1 = CreateDocumentTab(@"C:\work\a.txt");
        var tab2 = CreateDocumentTab(@"C:\work\b.txt");
        workspace.AddTab(tab1);
        workspace.AddTab(CreateDiffTab());
        workspace.AddTab(tab2);
        var provider = new WorkspaceOpenDocumentProvider(workspace);

        var result = provider.GetOpenDocuments();

        Assert.Equal(2, result.Count);
        Assert.Same(tab1.Document, result[0].Document);
        Assert.Same(tab2.Document, result[1].Document);
    }

    // DiffTab除外

    [Fact]
    public void GetOpenDocuments_DiffTabのみ存在する場合_空のリストが返る()
    {
        var workspace = new Workspace();
        workspace.AddTab(CreateDiffTab());
        var provider = new WorkspaceOpenDocumentProvider(workspace);

        var result = provider.GetOpenDocuments();

        Assert.Empty(result);
    }

    [Fact]
    public void GetOpenDocuments_DocumentTabとDiffTabが混在する場合_DocumentTabのみ返る()
    {
        var workspace = new Workspace();
        var tab = CreateDocumentTab(@"C:\work\a.txt");
        workspace.AddTab(tab);
        workspace.AddTab(CreateDiffTab());
        var provider = new WorkspaceOpenDocumentProvider(workspace);

        var result = provider.GetOpenDocuments();

        Assert.Single(result);
        Assert.Same(tab.Document, result[0].Document);
    }

    // 対応関係

    [Fact]
    public void GetOpenDocuments_各要素が対応タブのDocumentとDisplayTitleを保持する()
    {
        var workspace = new Workspace();
        var tab = CreateDocumentTab(@"C:\work\a.txt");
        workspace.AddTab(tab);
        var provider = new WorkspaceOpenDocumentProvider(workspace);

        var result = provider.GetOpenDocuments();

        Assert.Same(tab.Document, result[0].Document);
        Assert.Equal(tab.DisplayTitle, result[0].DisplayTitle);
    }

    // 動的変化

    [Fact]
    public void GetOpenDocuments_タブ追加削除後の再呼び出しで結果が更新される()
    {
        var workspace = new Workspace();
        var tab1 = CreateDocumentTab(@"C:\work\a.txt");
        var tab2 = CreateDocumentTab(@"C:\work\b.txt");
        workspace.AddTab(tab1);
        var provider = new WorkspaceOpenDocumentProvider(workspace);

        Assert.Single(provider.GetOpenDocuments());

        workspace.AddTab(tab2);
        Assert.Equal(2, provider.GetOpenDocuments().Count);

        workspace.RemoveTab(tab1);
        var result = provider.GetOpenDocuments();
        Assert.Single(result);
        Assert.Same(tab2.Document, result[0].Document);
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
