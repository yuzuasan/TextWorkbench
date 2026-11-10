using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Tests.Presentation.ViewModels;

/// <summary>
/// <see cref="Workspace"/> の単体テストです。
/// </summary>
public class WorkspaceTests
{
    // AddTab

    [Fact]
    public void AddTab_Tabを追加すると_Tabsに追加されActiveTabに設定される()
    {
        var workspace = new Workspace();
        var tab = CreateDocumentTab("sample.txt");

        workspace.AddTab(tab);

        Assert.Single(workspace.Tabs);
        Assert.Same(tab, workspace.Tabs[0]);
        Assert.Same(tab, workspace.ActiveTab);
    }

    [Fact]
    public void AddTab_無題Documentを連続追加すると_DisplayTitleが連番になる()
    {
        var workspace = new Workspace();
        var tab1 = CreateUntitledDocumentTab();
        var tab2 = CreateUntitledDocumentTab();

        workspace.AddTab(tab1);
        workspace.AddTab(tab2);

        Assert.Equal("無題1", tab1.DisplayTitle);
        Assert.Equal("無題2", tab2.DisplayTitle);
    }

    [Fact]
    public void AddTab_ファイルパスを持つDocumentは_連番対象外になる()
    {
        var workspace = new Workspace();
        var fileTab = CreateDocumentTab("sample.txt");
        var untitledTab = CreateUntitledDocumentTab();

        workspace.AddTab(fileTab);
        workspace.AddTab(untitledTab);

        Assert.Equal("sample.txt", fileTab.DisplayTitle);
        Assert.Equal("無題1", untitledTab.DisplayTitle);
    }

    [Fact]
    public void AddTab_DiffTabを追加すると_連番対象外でActiveTabに設定される()
    {
        var workspace = new Workspace();
        var untitledTab = CreateUntitledDocumentTab();
        workspace.AddTab(untitledTab);
        
        var diffTab = CreateDiffTab();
        workspace.AddTab(diffTab);

        Assert.Equal("無題1", untitledTab.DisplayTitle);
        Assert.Equal(2, workspace.Tabs.Count);
        Assert.Same(diffTab, workspace.ActiveTab);
    }

    [Fact]
    public void AddTab_nullを渡すと_ArgumentNullExceptionが発生する()
    {
        var workspace = new Workspace();

        Assert.Throws<ArgumentNullException>(() => workspace.AddTab(null!));
    }

    // RemoveTab

    [Fact]
    public void RemoveTab_非アクティブなTabを削除しても_ActiveTabは変わらない()
    {
        var workspace = new Workspace();
        var tab1 = CreateDocumentTab("a.txt");
        var tab2 = CreateDocumentTab("b.txt");
        workspace.AddTab(tab1);
        workspace.AddTab(tab2);
        workspace.SetActiveTab(tab1);

        workspace.RemoveTab(tab2);

        Assert.Single(workspace.Tabs);
        Assert.Same(tab1, workspace.ActiveTab);
    }

    [Fact]
    public void RemoveTab_アクティブなTabを削除すると_同じ位置の次のTabがActiveになる()
    {
        var workspace = new Workspace();
        var tab1 = CreateDocumentTab("a.txt");
        var tab2 = CreateDocumentTab("b.txt");
        var tab3 = CreateDocumentTab("c.txt");
        workspace.AddTab(tab1);
        workspace.AddTab(tab2);
        workspace.AddTab(tab3);
        workspace.SetActiveTab(tab1);

        workspace.RemoveTab(tab1);

        Assert.Equal(2, workspace.Tabs.Count);
        Assert.Same(tab2, workspace.ActiveTab);
    }

    [Fact]
    public void RemoveTab_末尾のアクティブなTabを削除すると_1つ前のTabがActiveになる()
    {
        var workspace = new Workspace();
        var tab1 = CreateDocumentTab("a.txt");
        var tab2 = CreateDocumentTab("b.txt");
        workspace.AddTab(tab1);
        workspace.AddTab(tab2);

        workspace.RemoveTab(tab2);

        Assert.Single(workspace.Tabs);
        Assert.Same(tab1, workspace.ActiveTab);
    }

    [Fact]
    public void RemoveTab_最後の1つのTabを削除すると_ActiveTabがnullになる()
    {
        var workspace = new Workspace();
        var tab = CreateDocumentTab("a.txt");
        workspace.AddTab(tab);

        workspace.RemoveTab(tab);

        Assert.Empty(workspace.Tabs);
        Assert.Null(workspace.ActiveTab);
    }

    [Fact]
    public void RemoveTab_Tabsに存在しないTabを削除しても_何も起きない()
    {
        var workspace = new Workspace();
        var tab1 = CreateDocumentTab("a.txt");
        var tab2 = CreateDocumentTab("b.txt");
        workspace.AddTab(tab1);
        workspace.SetActiveTab(tab1);

        workspace.RemoveTab(tab2);

        Assert.Single(workspace.Tabs);
        Assert.Same(tab1, workspace.ActiveTab);
    }

    [Fact]
    public void RemoveTab_nullを渡すと_ArgumentNullExceptionが発生する()
    {
        var workspace = new Workspace();

        Assert.Throws<ArgumentNullException>(() => workspace.RemoveTab(null!));
    }

    // SetActiveTab

    [Fact]
    public void SetActiveTab_指定したTabがActiveTabになる()
    {
        var workspace = new Workspace();
        var tab1 = CreateDocumentTab("a.txt");
        var tab2 = CreateDocumentTab("b.txt");
        workspace.AddTab(tab1);
        workspace.AddTab(tab2);

        workspace.SetActiveTab(tab1);

        Assert.Same(tab1, workspace.ActiveTab);
    }

    [Fact]
    public void SetActiveTab_nullを渡すと_ActiveTabがnullになる()
    {
        var workspace = new Workspace();
        var tab = CreateDocumentTab("a.txt");
        workspace.AddTab(tab);

        workspace.SetActiveTab(null);

        Assert.Null(workspace.ActiveTab);
    }

    // FindDocumentByFilePath

    [Fact]
    public void FindDocumentByFilePath_nullの場合_nullが返る()
    {
        var workspace = new Workspace();

        Assert.Null(workspace.FindDocumentByFilePath(null!));
    }

    [Fact]
    public void FindDocumentByFilePath_空文字の場合_nullが返る()
    {
        var workspace = new Workspace();

        Assert.Null(workspace.FindDocumentByFilePath(string.Empty));
    }

    [Fact]
    public void FindDocumentByFilePath_一致するDocumentTabを返す()
    {
        var workspace = new Workspace();
        var tab = CreateDocumentTab("sample.txt");
        workspace.AddTab(tab);

        var result = workspace.FindDocumentByFilePath(@"C:\work\SAMPLE.TXT");

        Assert.Same(tab, result);
    }

    [Fact]
    public void FindDocumentByFilePath_一致しない場合_nullが返る()
    {
        var workspace = new Workspace();
        workspace.AddTab(CreateDocumentTab("sample.txt"));

        var result = workspace.FindDocumentByFilePath(@"C:\work\other.txt");

        Assert.Null(result);
    }

    [Fact]
    public void FindDocumentByFilePath_DiffTabは検索対象外()
    {
        var workspace = new Workspace();
        workspace.AddTab(CreateDiffTab());

        var result = workspace.FindDocumentByFilePath(@"C:\work\sample.txt");

        Assert.Null(result);
    }

    [Fact]
    public void FindDocumentByFilePath_ファイルパスを持たない無題Documentは検索対象外()
    {
        var workspace = new Workspace();
        workspace.AddTab(CreateUntitledDocumentTab());

        var result = workspace.FindDocumentByFilePath(@"C:\work\sample.txt");

        Assert.Null(result);
    }

    // テストデータ作成ヘルパー

    private static DocumentTabViewModel CreateDocumentTab(string fileName)
    {
        var filePath = $@"C:\work\{fileName}";
        var document = new Document(filePath, fileName, "content", TextEncoding.Utf8, LineEnding.CrLf, false);
        return new DocumentTabViewModel(document);
    }

    private static DocumentTabViewModel CreateUntitledDocumentTab()
    {
        return new DocumentTabViewModel(new Document());
    }

    private static DiffTabViewModel CreateDiffTab()
    {
        var leftSource = new DiffSession.DiffSource(
            SourceType.File,
            @"C:\work\left.txt",
            @"C:\work\left.txt",
            "left.txt",
            TextEncoding.Utf8,
            "left");

        var rightSource = new DiffSession.DiffSource(
            SourceType.File,
            @"C:\work\right.txt",
            @"C:\work\right.txt",
            "right.txt",
            TextEncoding.Utf8,
            "right");

        var lines = new[]
        {
            new DiffLine(1, 1, DiffLineType.Changed,
                new[] { new DiffSegment("left", DiffSegmentType.Deleted) },
                new[] { new DiffSegment("right", DiffSegmentType.Added) }),
        };

        var result = new DiffResult("left.txt", "right.txt", lines);
        var session = new DiffSession(leftSource, rightSource, result);

        return new DiffTabViewModel(session);
    }
}
