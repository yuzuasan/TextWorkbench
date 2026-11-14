using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Tests.Presentation.ViewModels;

/// <summary>
/// <see cref="DocumentTabViewModel"/> の単体テストです。
/// </summary>
public class DocumentTabViewModelTests
{
    // コンストラクタ・基本プロパティ

    [Fact]
    public void コンストラクタ_documentがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new DocumentTabViewModel(null!));
    }

    [Fact]
    public void コンストラクタ_FileNameがある場合_DisplayTitleにFileNameが設定される()
    {
        var viewModel = CreateViewModel();

        Assert.Equal("a.txt", viewModel.DisplayTitle);
    }

    [Fact]
    public void コンストラクタ_無題Documentの場合_DisplayTitleが無題になる()
    {
        var document = new Document();
        var viewModel = new DocumentTabViewModel(document);

        Assert.Equal("無題", viewModel.DisplayTitle);
    }

    [Fact]
    public void ToolTipText_FilePathがあればフルパス_無題ならDisplayTitleが返る()
    {
        var fileViewModel = CreateViewModel(filePath: @"C:\work\a.txt");
        var untitledViewModel = new DocumentTabViewModel(new Document());

        Assert.Equal(@"C:\work\a.txt", fileViewModel.ToolTipText);
        Assert.Equal("無題", untitledViewModel.ToolTipText);
    }

    [Fact]
    public void EditorState_初期内容がDocumentのCurrentContentと一致する()
    {
        var viewModel = CreateViewModel(content: "line1\nline2");

        Assert.Equal("line1\nline2", viewModel.EditorState.TextDocument.Text);
    }

    [Fact]
    public void 状態プロパティ_Documentの状態が反映される()
    {
        var viewModel = CreateViewModel(isReadOnly: true);

        Assert.False(viewModel.IsModified);
        Assert.True(viewModel.IsReadOnly);
        Assert.False(viewModel.IsEditable);

        viewModel.Document.UpdateContent("changed");
        Assert.True(viewModel.IsModified);
    }

    // Caret

    [Fact]
    public void UpdateCaretPosition_行と列が更新される()
    {
        var viewModel = CreateViewModel();

        Assert.Equal(1, viewModel.CaretLine);
        Assert.Equal(1, viewModel.CaretColumn);

        viewModel.UpdateCaretPosition(3, 5);

        Assert.Equal(3, viewModel.CaretLine);
        Assert.Equal(5, viewModel.CaretColumn);
    }

    // 検索

    [Fact]
    public void SetSearchResults_resultsがnullの場合_ArgumentNullExceptionが発生する()
    {
        var viewModel = CreateViewModel();

        Assert.Throws<ArgumentNullException>(() => viewModel.SetSearchResults(null!));
    }

    [Fact]
    public void SetSearchResults_設定時に現在位置が未選択へリセットされる()
    {
        var viewModel = CreateViewModel();
        viewModel.CurrentSearchIndex = 2;
        var results = new[] { new SearchResult(0, 1), new SearchResult(5, 1) };

        viewModel.SetSearchResults(results);

        Assert.Same(results, viewModel.SearchResults);
        Assert.Equal(-1, viewModel.CurrentSearchIndex);
    }

    [Fact]
    public void ClearSearchResults_検索結果と現在位置が初期化される()
    {
        var viewModel = CreateViewModel();
        viewModel.SetSearchResults(new[] { new SearchResult(0, 1) });
        viewModel.CurrentSearchIndex = 0;

        viewModel.ClearSearchResults();

        Assert.Empty(viewModel.SearchResults);
        Assert.Equal(-1, viewModel.CurrentSearchIndex);
    }

    [Fact]
    public void FindNext_検索結果が0件の場合_インデックスは変化しない()
    {
        var viewModel = CreateViewModel();

        viewModel.FindNext();

        Assert.Equal(-1, viewModel.CurrentSearchIndex);
    }

    [Fact]
    public void FindNext_末尾の次は先頭へ循環する()
    {
        var viewModel = CreateViewModel();
        viewModel.SetSearchResults(new[]
        {
            new SearchResult(0, 1), new SearchResult(5, 1), new SearchResult(10, 1),
        });

        viewModel.FindNext();
        Assert.Equal(0, viewModel.CurrentSearchIndex);
        viewModel.FindNext();
        Assert.Equal(1, viewModel.CurrentSearchIndex);
        viewModel.FindNext();
        Assert.Equal(2, viewModel.CurrentSearchIndex);
        viewModel.FindNext();
        Assert.Equal(0, viewModel.CurrentSearchIndex);
    }

    [Fact]
    public void FindPrevious_先頭の前は末尾へ循環する()
    {
        var viewModel = CreateViewModel();
        viewModel.SetSearchResults(new[]
        {
            new SearchResult(0, 1), new SearchResult(5, 1), new SearchResult(10, 1),
        });

        viewModel.FindPrevious();
        Assert.Equal(2, viewModel.CurrentSearchIndex);
        viewModel.FindPrevious();
        Assert.Equal(1, viewModel.CurrentSearchIndex);
        viewModel.FindPrevious();
        Assert.Equal(0, viewModel.CurrentSearchIndex);
        viewModel.FindPrevious();
        Assert.Equal(2, viewModel.CurrentSearchIndex);
    }

    [Fact]
    public void FindNextPrevious_検索結果が0件の場合_インデックスは変化しない()
    {
        var viewModel = CreateViewModel();
        viewModel.SetSearchResults(Array.Empty<SearchResult>());

        viewModel.FindPrevious();

        Assert.Equal(-1, viewModel.CurrentSearchIndex);
    }

    // 内容変更・リセット

    [Fact]
    public void OnDocumentContentChanged_検索結果と置換開始位置がリセットされContentVersionが増加する()
    {
        var viewModel = CreateViewModel();
        viewModel.SetSearchResults(new[] { new SearchResult(0, 1) });
        viewModel.CurrentSearchIndex = 0;
        viewModel.ReplaceNextStartIndex = 10;
        var version = viewModel.ContentVersion;

        viewModel.OnDocumentContentChanged();

        Assert.Empty(viewModel.SearchResults);
        Assert.Equal(-1, viewModel.CurrentSearchIndex);
        Assert.Equal(0, viewModel.ReplaceNextStartIndex);
        Assert.Equal(version + 1, viewModel.ContentVersion);
    }

    [Fact]
    public void OnDocumentContentChanged_syncFromDocumentがfalseでも同様にリセットされる()
    {
        var viewModel = CreateViewModel();
        viewModel.SetSearchResults(new[] { new SearchResult(0, 1) });
        var version = viewModel.ContentVersion;

        viewModel.OnDocumentContentChanged(syncFromDocument: false);

        Assert.Empty(viewModel.SearchResults);
        Assert.Equal(version + 1, viewModel.ContentVersion);
    }

    [Fact]
    public void OnSearchTextChanged_置換開始位置がリセットされる()
    {
        var viewModel = CreateViewModel();
        viewModel.ReplaceNextStartIndex = 7;

        viewModel.SearchText = "keyword";

        Assert.Equal(0, viewModel.ReplaceNextStartIndex);
    }

    [Fact]
    public void OnIsRegexChanged_置換開始位置がリセットされる()
    {
        var viewModel = CreateViewModel();
        viewModel.ReplaceNextStartIndex = 7;

        viewModel.IsRegex = true;

        Assert.Equal(0, viewModel.ReplaceNextStartIndex);
    }

    [Fact]
    public void OnDocumentSavedAs_ファイルパス更新後にDisplayTitleとToolTipTextが更新される()
    {
        var viewModel = CreateViewModel(filePath: @"C:\work\a.txt", fileName: "a.txt");
        var notifiedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => notifiedProperties.Add(e.PropertyName);

        viewModel.Document.UpdateFilePath(@"C:\work\b.txt", "b.txt", false);
        viewModel.Document.MarkAsSaved();
        viewModel.OnDocumentSavedAs();

        Assert.Equal("b.txt", viewModel.DisplayTitle);
        Assert.Equal(@"C:\work\b.txt", viewModel.ToolTipText);
        Assert.Contains("DisplayTitle", notifiedProperties);
        Assert.Contains("ToolTipText", notifiedProperties);
    }

    [Fact]
    public void OnDocumentStateChanged_状態系プロパティの変更が通知される()
    {
        var viewModel = CreateViewModel();
        var notifiedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => notifiedProperties.Add(e.PropertyName);

        viewModel.OnDocumentStateChanged();

        Assert.Contains("IsModified", notifiedProperties);
        Assert.Contains("IsReadOnly", notifiedProperties);
        Assert.Contains("IsEditable", notifiedProperties);
    }

    [Fact]
    public void OnDocumentSaved_保存関連プロパティの変更が通知される()
    {
        var viewModel = CreateViewModel();
        var notifiedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => notifiedProperties.Add(e.PropertyName);

        viewModel.OnDocumentSaved();

        Assert.Contains("IsModified", notifiedProperties);
        Assert.Contains("IsSaving", notifiedProperties);
        Assert.Contains("IsEditable", notifiedProperties);
    }

    // テストデータ作成ヘルパー

    private static DocumentTabViewModel CreateViewModel(
        string? filePath = @"C:\work\a.txt",
        string? fileName = "a.txt",
        string content = "content",
        bool isReadOnly = false)
    {
        Document document;
        if (filePath == null)
        {
            document = new Document();
            document.UpdateContent(content);
        }
        else
        {
            document = new Document(
                filePath, fileName!, content,
                TextEncoding.Utf8, LineEnding.CrLf, isReadOnly);
        }

        return new DocumentTabViewModel(document);
    }
}
