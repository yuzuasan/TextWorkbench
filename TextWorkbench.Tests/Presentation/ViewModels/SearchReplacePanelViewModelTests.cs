using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Tests.Presentation.ViewModels;

/// <summary>
/// <see cref="SearchReplacePanelViewModel"/> の単体テストです。
/// </summary>
public class SearchReplacePanelViewModelTests
{
    // コンストラクタ

    [Fact]
    public void コンストラクタ_searchTextUseCaseがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new SearchReplacePanelViewModel(
            null!, new ReplaceTextUseCase(new FakeReplaceService())));
    }

    [Fact]
    public void コンストラクタ_replaceTextUseCaseがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new SearchReplacePanelViewModel(
            new SearchTextUseCase(new FakeSearchService()), null!));
    }

    // ActiveDocumentTab・表示状態

    [Fact]
    public void ActiveDocumentTab_設定するとタブの検索状態が反映される()
    {
        var harness = new Harness();
        var tab = harness.CreateDocumentTab();
        tab.SearchText = "keyword";
        tab.IsRegex = true;
        tab.ReplaceText = "replacement";

        harness.Panel.ActiveDocumentTab = tab;

        Assert.Same(tab, harness.Panel.ActiveDocumentTab);
        Assert.Equal("keyword", harness.Panel.SearchText);
        Assert.True(harness.Panel.IsRegex);
        Assert.Equal("replacement", harness.Panel.ReplaceText);
    }

    [Fact]
    public void ActiveDocumentTab_nullに設定すると既定値が返る()
    {
        var harness = new Harness();
        harness.Panel.ActiveDocumentTab = harness.CreateDocumentTab();
        harness.Panel.SearchText = "keyword";

        harness.Panel.ActiveDocumentTab = null;

        Assert.Equal(string.Empty, harness.Panel.SearchText);
        Assert.False(harness.Panel.IsRegex);
        Assert.Equal(string.Empty, harness.Panel.ReplaceText);
        Assert.Empty(harness.Panel.SearchResults);
        Assert.Equal(-1, harness.Panel.CurrentSearchIndex);
    }

    [Fact]
    public void IsVisible_IsOpenかつDocumentTabがある場合のみtrueになる()
    {
        var harness = new Harness();

        // タブなし：IsOpenだけでは表示されない
        harness.Panel.IsOpen = true;
        Assert.False(harness.Panel.IsVisible);

        harness.Panel.ActiveDocumentTab = harness.CreateDocumentTab();
        Assert.True(harness.Panel.IsVisible);

        harness.Panel.IsOpen = false;
        Assert.False(harness.Panel.IsVisible);
    }

    [Fact]
    public void CloseCommand_実行するとIsOpenがfalseになる()
    {
        var harness = new Harness();
        harness.Panel.IsOpen = true;

        harness.Panel.CloseCommand.Execute(null);

        Assert.False(harness.Panel.IsOpen);
    }

    [Fact]
    public void ActiveDocumentTab_切替時に新しいタブの値へ更新される()
    {
        var harness = new Harness();
        var tab1 = harness.CreateDocumentTab();
        var tab2 = harness.CreateDocumentTab();
        tab1.SearchText = "first";
        tab2.SearchText = "second";

        harness.Panel.ActiveDocumentTab = tab1;
        Assert.Equal("first", harness.Panel.SearchText);

        harness.Panel.ActiveDocumentTab = tab2;
        Assert.Equal("second", harness.Panel.SearchText);
    }

    [Fact]
    public void ActiveDocumentTab_同一タブの再設定ではPropertyChangedが発火しない()
    {
        var harness = new Harness();
        var tab = harness.CreateDocumentTab();
        harness.Panel.ActiveDocumentTab = tab;

        var notified = false;
        harness.Panel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SearchReplacePanelViewModel.ActiveDocumentTab))
                notified = true;
        };

        harness.Panel.ActiveDocumentTab = tab;

        Assert.False(notified);
    }

    [Fact]
    public void ActiveDocumentTab_切替後は以前のタブの変更がパネルへ通知されない()
    {
        var harness = new Harness();
        var tab1 = harness.CreateDocumentTab();
        var tab2 = harness.CreateDocumentTab();
        harness.Panel.ActiveDocumentTab = tab1;
        harness.Panel.ActiveDocumentTab = tab2;

        var searchCallCount = harness.SearchService.CallCount;
        tab1.SearchText = "after-switch";

        // 旧タブの変更では再検索が実行されない
        Assert.Equal(searchCallCount, harness.SearchService.CallCount);
    }

    // プロパティ委譲

    [Fact]
    public void SearchTextの設定がアクティブタブへ反映される()
    {
        var harness = new Harness();
        var tab = harness.CreateDocumentTab();
        harness.Panel.ActiveDocumentTab = tab;

        harness.Panel.SearchText = "from-panel";

        Assert.Equal("from-panel", tab.SearchText);
    }

    [Fact]
    public void IsRegexとReplaceTextの設定がアクティブタブへ反映される()
    {
        var harness = new Harness();
        var tab = harness.CreateDocumentTab();
        harness.Panel.ActiveDocumentTab = tab;

        harness.Panel.IsRegex = true;
        harness.Panel.ReplaceText = "rep";

        Assert.True(tab.IsRegex);
        Assert.Equal("rep", tab.ReplaceText);
    }

    [Fact]
    public void タブのSearchResults変更がパネルへ通知される()
    {
        var harness = new Harness();
        var tab = harness.CreateDocumentTab();
        harness.Panel.ActiveDocumentTab = tab;

        string? notifiedProperty = null;
        harness.Panel.PropertyChanged += (_, e) => notifiedProperty = e.PropertyName;

        tab.SetSearchResults(new[] { new SearchResult(0, 1) });

        Assert.Equal(nameof(SearchReplacePanelViewModel.SearchResults), notifiedProperty);
        Assert.Single(harness.Panel.SearchResults);
    }

    // 自動再検索

    [Fact]
    public void タブのSearchText変更で検索が実行され結果が反映される()
    {
        var harness = new Harness();
        var tab = harness.CreateDocumentTab(content: "hello world");
        harness.Panel.ActiveDocumentTab = tab;
        harness.SearchService.Results.Add(new SearchResult(0, 5));

        tab.SearchText = "hello";

        Assert.Equal(1, harness.SearchService.CallCount);
        Assert.Equal("hello world", harness.SearchService.LastContent);
        Assert.Equal("hello", harness.SearchService.LastSearchText);
        Assert.False(harness.SearchService.LastIsRegex);
        Assert.Single(tab.SearchResults);
        Assert.Equal(string.Empty, harness.Panel.ErrorMessage);
    }

    [Fact]
    public void タブのIsRegex変更で正規表現フラグ付きで再検索される()
    {
        var harness = new Harness();
        var tab = harness.CreateDocumentTab(content: "abc123");
        harness.Panel.ActiveDocumentTab = tab;
        tab.SearchText = "a.*";

        tab.IsRegex = true;

        Assert.True(harness.SearchService.LastIsRegex);
    }

    [Fact]
    public void タブのContentVersion変更で再検索される()
    {
        var harness = new Harness();
        var tab = harness.CreateDocumentTab(content: "content");
        harness.Panel.ActiveDocumentTab = tab;
        tab.SearchText = "con";
        var callCount = harness.SearchService.CallCount;

        tab.OnDocumentContentChanged();

        Assert.Equal(callCount + 1, harness.SearchService.CallCount);
    }

    [Fact]
    public void 空のSearchTextでは検索サービスが呼ばれず結果がクリアされる()
    {
        var harness = new Harness();
        var tab = harness.CreateDocumentTab(content: "content");
        harness.Panel.ActiveDocumentTab = tab;
        harness.SearchService.Results.Add(new SearchResult(0, 3));
        tab.SearchText = "con";
        Assert.NotEmpty(tab.SearchResults);

        tab.SearchText = string.Empty;

        Assert.Empty(tab.SearchResults);
        Assert.Equal(-1, tab.CurrentSearchIndex);
    }

    [Fact]
    public void 無効な正規表現では結果がクリアされErrorMessageが設定される()
    {
        var harness = new Harness();
        harness.SearchService.Exception = new InvalidRegexException("正規表現が無効です。");
        var tab = harness.CreateDocumentTab(content: "content");
        harness.Panel.ActiveDocumentTab = tab;
        tab.SetSearchResults(new[] { new SearchResult(0, 1) });

        tab.SearchText = "[";
        tab.IsRegex = true;

        Assert.Empty(tab.SearchResults);
        Assert.Equal("正規表現が無効です。", harness.Panel.ErrorMessage);
        Assert.True(harness.Panel.HasError);
    }

    [Fact]
    public void 検索成功時にErrorMessageがクリアされる()
    {
        var harness = new Harness();
        harness.SearchService.Exception = new InvalidRegexException("error");
        var tab = harness.CreateDocumentTab(content: "content");
        harness.Panel.ActiveDocumentTab = tab;
        tab.SearchText = "[";
        tab.IsRegex = true;
        Assert.True(harness.Panel.HasError);

        harness.SearchService.Exception = null;
        tab.IsRegex = false;

        Assert.Equal(string.Empty, harness.Panel.ErrorMessage);
        Assert.False(harness.Panel.HasError);
    }

    [Fact]
    public void InvalidRegexException以外の例外はそのまま伝播する()
    {
        var harness = new Harness();
        harness.SearchService.Exception = new InvalidOperationException("unexpected");
        var tab = harness.CreateDocumentTab(content: "content");
        harness.Panel.ActiveDocumentTab = tab;

        Assert.Throws<InvalidOperationException>(() => tab.SearchText = "x");
    }

    // ReplaceFirst / ReplaceAll

    [Fact]
    public void ReplaceFirst_アクティブタブがない場合_InvalidOperationExceptionが発生する()
    {
        var harness = new Harness();

        Assert.Throws<InvalidOperationException>(() => harness.Panel.ReplaceFirst());
    }

    [Fact]
    public void ReplaceAll_アクティブタブがない場合_InvalidOperationExceptionが発生する()
    {
        var harness = new Harness();

        Assert.Throws<InvalidOperationException>(() => harness.Panel.ReplaceAll());
    }

    [Fact]
    public void ReplaceFirst_検索置換条件と開始位置がUseCaseへ渡される()
    {
        var harness = new Harness();
        var tab = harness.CreateDocumentTab(content: "hello world");
        harness.Panel.ActiveDocumentTab = tab;
        tab.SearchText = "o";
        tab.ReplaceText = "0";
        tab.ReplaceNextStartIndex = 4;
        harness.ReplaceService.FirstResult = new ReplaceResult(
            "hell0 world", 1, 4, 1, 1);

        var result = harness.Panel.ReplaceFirst();

        Assert.Equal("hello world", harness.ReplaceService.LastFirstContent);
        Assert.Equal("o", harness.ReplaceService.LastFirstSearchText);
        Assert.Equal("0", harness.ReplaceService.LastFirstReplacement);
        Assert.False(harness.ReplaceService.LastFirstIsRegex);
        Assert.Equal(4, harness.ReplaceService.LastFirstStartIndex);
        Assert.Same(harness.ReplaceService.FirstResult, result);
    }

    [Fact]
    public void ReplaceAll_検索置換条件がUseCaseへ渡される()
    {
        var harness = new Harness();
        var tab = harness.CreateDocumentTab(content: "aaa");
        harness.Panel.ActiveDocumentTab = tab;
        tab.SearchText = "a";
        tab.ReplaceText = "b";
        tab.IsRegex = true;
        harness.ReplaceService.AllResult = new ReplaceResult("bbb", 3, null, null, null);

        var result = harness.Panel.ReplaceAll();

        Assert.Equal("aaa", harness.ReplaceService.LastAllContent);
        Assert.Equal("a", harness.ReplaceService.LastAllSearchText);
        Assert.Equal("b", harness.ReplaceService.LastAllReplacement);
        Assert.True(harness.ReplaceService.LastAllIsRegex);
        Assert.Same(harness.ReplaceService.AllResult, result);
    }

    // テストデータ作成ヘルパー

    private static DocumentTabViewModel CreateDocumentTab(string content = "content")
    {
        var document = new Document();
        document.UpdateContent(content);
        return new DocumentTabViewModel(document);
    }

    /// <summary>
    /// パネルとその依存Fakeをまとめて生成します。
    /// </summary>
    private sealed class Harness
    {
        public FakeSearchService SearchService { get; } = new();
        public FakeReplaceService ReplaceService { get; } = new();
        public SearchReplacePanelViewModel Panel { get; }

        public Harness()
        {
            Panel = new SearchReplacePanelViewModel(
                new SearchTextUseCase(SearchService),
                new ReplaceTextUseCase(ReplaceService));
        }

        public DocumentTabViewModel CreateDocumentTab(string content = "content")
        {
            return SearchReplacePanelViewModelTests.CreateDocumentTab(content);
        }
    }

    private sealed class FakeSearchService : ISearchService
    {
        public List<SearchResult> Results { get; } = new();
        public Exception? Exception { get; set; }
        public int CallCount { get; private set; }
        public string? LastContent { get; private set; }
        public string? LastSearchText { get; private set; }
        public bool LastIsRegex { get; private set; }

        public IReadOnlyList<SearchResult> Search(string content, SearchCondition condition)
        {
            CallCount++;
            LastContent = content;
            LastSearchText = condition.SearchText;
            LastIsRegex = condition.IsRegex;
            if (Exception != null)
                throw Exception;
            return Results;
        }
    }

    private sealed class FakeReplaceService : IReplaceService
    {
        public ReplaceResult FirstResult { get; set; } = new(string.Empty, 0, null, null, null);
        public ReplaceResult AllResult { get; set; } = new(string.Empty, 0, null, null, null);
        public string? LastFirstContent { get; private set; }
        public string? LastFirstSearchText { get; private set; }
        public string? LastFirstReplacement { get; private set; }
        public bool LastFirstIsRegex { get; private set; }
        public int LastFirstStartIndex { get; private set; }
        public string? LastAllContent { get; private set; }
        public string? LastAllSearchText { get; private set; }
        public string? LastAllReplacement { get; private set; }
        public bool LastAllIsRegex { get; private set; }

        public ReplaceResult ReplaceFirst(
            string content, SearchCondition condition, string replacement, int startIndex)
        {
            LastFirstContent = content;
            LastFirstSearchText = condition.SearchText;
            LastFirstReplacement = replacement;
            LastFirstIsRegex = condition.IsRegex;
            LastFirstStartIndex = startIndex;
            return FirstResult;
        }

        public ReplaceResult ReplaceAll(
            string content, SearchCondition condition, string replacement)
        {
            LastAllContent = content;
            LastAllSearchText = condition.SearchText;
            LastAllReplacement = replacement;
            LastAllIsRegex = condition.IsRegex;
            return AllResult;
        }
    }
}
