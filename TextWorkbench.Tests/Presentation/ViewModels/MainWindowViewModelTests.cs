using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Tests.Presentation.ViewModels;

/// <summary>
/// <see cref="MainWindowViewModel"/> の単体テストです。
/// </summary>
public class MainWindowViewModelTests
{
    // コンストラクタ・初期状態

    [Fact]
    public void コンストラクタ_notificationServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        var harness = new Harness();

        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
            null!, harness.FileDialogService, harness.NewDocumentUseCase,
            harness.OpenDocumentUseCase, harness.SaveDocumentUseCase,
            harness.SaveDocumentAsUseCase, harness.CloseDocumentUseCase,
            harness.ExitApplicationUseCase, harness.GetTextInfoUseCase,
            harness.SearchTextUseCase, harness.ReplaceTextUseCase,
            harness.CompareDocumentsUseCase, harness.Workspace));
    }

    [Fact]
    public void コンストラクタ_起動時に初期Documentタブが追加されアクティブになる()
    {
        var harness = new Harness();

        var tab = Assert.IsType<DocumentTabViewModel>(harness.Workspace.ActiveTab);
        Assert.Single(harness.Workspace.Tabs);
        Assert.Same(tab, harness.Workspace.Tabs[0]);
        Assert.Equal("無題1", tab.DisplayTitle);
    }

    // CanExecute：IsProcessing

    [Fact]
    public void CanExecute_IsProcessing中は主要コマンドが無効になる()
    {
        var harness = new Harness();
        harness.ViewModel.IsProcessing = true;

        Assert.False(harness.ViewModel.NewCommand.CanExecute(null));
        Assert.False(harness.ViewModel.OpenCommand.CanExecute(null));
        Assert.False(harness.ViewModel.SaveCommand.CanExecute(null));
        Assert.False(harness.ViewModel.SaveAsCommand.CanExecute(null));
        Assert.False(harness.ViewModel.CloseCommand.CanExecute(null));
        Assert.False(harness.ViewModel.ExitCommand.CanExecute(null));
        Assert.False(harness.ViewModel.CompareCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_IsProcessing中でも検索結果があればFindNextは有効のまま()
    {
        var harness = new Harness();
        var tab = (DocumentTabViewModel)harness.Workspace.ActiveTab!;
        tab.SetSearchResults(new[] { new SearchResult(0, 1) });
        harness.ViewModel.IsProcessing = true;

        Assert.True(harness.ViewModel.FindNextCommand.CanExecute(null));
    }

    // CanExecute：タブ種別

    [Fact]
    public void CanExecute_DiffTabがアクティブの場合_Document系コマンドが無効になる()
    {
        var harness = new Harness();
        harness.AddDiffTab(diffCount: 2);

        Assert.False(harness.ViewModel.SaveCommand.CanExecute(null));
        Assert.False(harness.ViewModel.SaveAsCommand.CanExecute(null));
        Assert.False(harness.ViewModel.TextInfoCommand.CanExecute(null));
        Assert.False(harness.ViewModel.OpenSearchReplacePanelCommand.CanExecute(null));
        Assert.False(harness.ViewModel.CompareWithSavedCommand.CanExecute(null));
        Assert.False(harness.ViewModel.FindNextCommand.CanExecute(null));
        Assert.False(harness.ViewModel.ReplaceNextCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_DiffTabがアクティブの場合_Closeと差分移動が有効になる()
    {
        var harness = new Harness();
        harness.AddDiffTab(diffCount: 2);

        Assert.True(harness.ViewModel.CloseCommand.CanExecute(null));
        Assert.True(harness.ViewModel.NextDiffCommand.CanExecute(null));
        Assert.True(harness.ViewModel.PreviousDiffCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_DiffTabがアクティブでもNewとCompareはIsProcessingのみで判定される()
    {
        var harness = new Harness();
        harness.AddDiffTab(diffCount: 1);

        Assert.True(harness.ViewModel.NewCommand.CanExecute(null));
        Assert.True(harness.ViewModel.CompareCommand.CanExecute(null));
        Assert.True(harness.ViewModel.ExitCommand.CanExecute(null));
    }

    // CanExecute：Document状態

    [Fact]
    public void CanExecute_読み取り専用Documentの場合_Saveは無効でSaveAsとCloseは有効()
    {
        var harness = new Harness();
        harness.SetActiveDocument(isReadOnly: true);

        Assert.False(harness.ViewModel.SaveCommand.CanExecute(null));
        Assert.True(harness.ViewModel.SaveAsCommand.CanExecute(null));
        Assert.True(harness.ViewModel.CloseCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_読み込み中Documentの場合_SaveとSaveAsとCloseが無効になる()
    {
        var harness = new Harness();
        var tab = (DocumentTabViewModel)harness.Workspace.ActiveTab!;
        tab.Document.StartLoading();

        Assert.False(harness.ViewModel.SaveCommand.CanExecute(null));
        Assert.False(harness.ViewModel.SaveAsCommand.CanExecute(null));
        Assert.False(harness.ViewModel.CloseCommand.CanExecute(null));

        tab.Document.EndLoading();
        Assert.True(harness.ViewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_保存中Documentの場合_SaveとSaveAsとCloseが無効になる()
    {
        var harness = new Harness();
        var tab = (DocumentTabViewModel)harness.Workspace.ActiveTab!;
        tab.Document.StartSaving();

        Assert.False(harness.ViewModel.SaveCommand.CanExecute(null));
        Assert.False(harness.ViewModel.SaveAsCommand.CanExecute(null));
        Assert.False(harness.ViewModel.CloseCommand.CanExecute(null));

        tab.Document.EndSaving();
        Assert.True(harness.ViewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_無題Documentの場合_CompareWithSavedは無効になる()
    {
        var harness = new Harness();
        // 初期タブは無題かつ未変更
        Assert.False(harness.ViewModel.CompareWithSavedCommand.CanExecute(null));

        // 変更してもFilePathがなければ無効のまま
        var tab = (DocumentTabViewModel)harness.Workspace.ActiveTab!;
        tab.Document.UpdateContent("changed");
        harness.ViewModel.RefreshCommandStates();

        Assert.False(harness.ViewModel.CompareWithSavedCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_ファイルあり変更済みDocumentの場合_CompareWithSavedが有効になる()
    {
        var harness = new Harness();
        harness.SetActiveDocument(filePath: @"C:\work\a.txt", fileName: "a.txt");
        var tab = (DocumentTabViewModel)harness.Workspace.ActiveTab!;
        tab.Document.UpdateContent("changed");
        harness.ViewModel.RefreshCommandStates();

        Assert.True(harness.ViewModel.CompareWithSavedCommand.CanExecute(null));
    }

    // CanExecute：検索・置換

    [Fact]
    public void CanExecute_検索結果が0件の場合_FindNextとReplaceNextが無効になる()
    {
        var harness = new Harness();

        Assert.False(harness.ViewModel.FindNextCommand.CanExecute(null));
        Assert.False(harness.ViewModel.FindPreviousCommand.CanExecute(null));
        Assert.False(harness.ViewModel.ReplaceNextCommand.CanExecute(null));
        Assert.False(harness.ViewModel.ReplaceAllCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_検索結果がある場合_FindNextとReplaceNextが有効になる()
    {
        var harness = new Harness();
        var tab = (DocumentTabViewModel)harness.Workspace.ActiveTab!;
        tab.SetSearchResults(new[] { new SearchResult(0, 1) });

        Assert.True(harness.ViewModel.FindNextCommand.CanExecute(null));
        Assert.True(harness.ViewModel.FindPreviousCommand.CanExecute(null));
        Assert.True(harness.ViewModel.ReplaceNextCommand.CanExecute(null));
        Assert.True(harness.ViewModel.ReplaceAllCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_読み取り専用で検索結果がある場合_ReplaceNextとReplaceAllは無効()
    {
        var harness = new Harness();
        harness.SetActiveDocument(isReadOnly: true);
        var tab = (DocumentTabViewModel)harness.Workspace.ActiveTab!;
        tab.SetSearchResults(new[] { new SearchResult(0, 1) });

        Assert.True(harness.ViewModel.FindNextCommand.CanExecute(null));
        Assert.False(harness.ViewModel.ReplaceNextCommand.CanExecute(null));
        Assert.False(harness.ViewModel.ReplaceAllCommand.CanExecute(null));
    }

    // CanExecute：差分ナビゲーション

    [Fact]
    public void CanExecute_差分0件のDiffTabの場合_NextDiffとPreviousDiffが無効になる()
    {
        var harness = new Harness();
        harness.AddDiffTab(diffCount: 0);

        Assert.False(harness.ViewModel.NextDiffCommand.CanExecute(null));
        Assert.False(harness.ViewModel.PreviousDiffCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_差分未選択の場合_NextDiffとPreviousDiffが有効になる()
    {
        var harness = new Harness();
        var diffTab = harness.AddDiffTab(diffCount: 2);

        Assert.Equal(-1, diffTab.CurrentDiffIndex);
        Assert.True(harness.ViewModel.NextDiffCommand.CanExecute(null));
        Assert.True(harness.ViewModel.PreviousDiffCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_差分位置に応じてNextDiffとPreviousDiffの有効が切り替わる()
    {
        var harness = new Harness();
        var diffTab = harness.AddDiffTab(diffCount: 2);

        // 先頭：PreviousDiffのみ無効
        diffTab.CurrentDiffIndex = 0;
        harness.ViewModel.RefreshCommandStates();
        Assert.False(harness.ViewModel.PreviousDiffCommand.CanExecute(null));
        Assert.True(harness.ViewModel.NextDiffCommand.CanExecute(null));

        // 末尾：NextDiffのみ無効
        diffTab.CurrentDiffIndex = 1;
        harness.ViewModel.RefreshCommandStates();
        Assert.False(harness.ViewModel.NextDiffCommand.CanExecute(null));
        Assert.True(harness.ViewModel.PreviousDiffCommand.CanExecute(null));
    }

    // 状態管理

    [Fact]
    public void StatusVisible_DocumentTabとDiffTabで表示状態が切り替わる()
    {
        var harness = new Harness();

        Assert.True(harness.ViewModel.IsDocumentStatusVisible);
        Assert.False(harness.ViewModel.IsDiffStatusVisible);

        harness.AddDiffTab(diffCount: 1);

        Assert.False(harness.ViewModel.IsDocumentStatusVisible);
        Assert.True(harness.ViewModel.IsDiffStatusVisible);
    }

    [Fact]
    public void ActiveTab切替時_SearchReplacePanelのActiveDocumentTabが追従する()
    {
        var harness = new Harness();
        var documentTab = (DocumentTabViewModel)harness.Workspace.ActiveTab!;
        Assert.Same(documentTab, harness.ViewModel.SearchReplacePanelViewModel.ActiveDocumentTab);

        harness.AddDiffTab(diffCount: 1);

        Assert.Null(harness.ViewModel.SearchReplacePanelViewModel.ActiveDocumentTab);
    }

    // テストデータ作成ヘルパー

    private static Document CreateDocument(
        string? filePath = null,
        string? fileName = null,
        string content = "content",
        bool isReadOnly = false)
    {
        if (filePath == null)
        {
            var untitled = new Document();
            if (!string.IsNullOrEmpty(content))
                untitled.UpdateContent(content);
            untitled.SetReadOnly(isReadOnly);
            return untitled;
        }

        return new Document(
            filePath, fileName!, content,
            TextEncoding.Utf8, LineEnding.CrLf, isReadOnly);
    }

    private static DiffTabViewModel CreateDiffTab(int diffCount)
    {
        var lines = Enumerable.Range(0, diffCount)
            .Select(i => new DiffLine(null, i + 1, DiffLineType.Added,
                Array.Empty<DiffSegment>(), Array.Empty<DiffSegment>()))
            .ToArray();
        var result = new DiffResult("left", "right", lines);
        var leftSource = new DiffSession.DiffSource(
            SourceType.File, "left-id", @"C:\work\left.txt", "left.txt",
            TextEncoding.Utf8, "left");
        var rightSource = new DiffSession.DiffSource(
            SourceType.File, "right-id", @"C:\work\right.txt", "right.txt",
            TextEncoding.Utf8, "right");
        return new DiffTabViewModel(new DiffSession(leftSource, rightSource, result));
    }

    /// <summary>
    /// MainWindowViewModelとその依存Fakeをまとめて生成します。
    /// </summary>
    private sealed class Harness
    {
        public FakeNotificationService NotificationService { get; } = new();
        public FakeFileDialogService FileDialogService { get; } = new();
        public FakeFileService FileService { get; } = new();
        public FakeEncodingService EncodingService { get; } = new();
        public FakeLineEndingService LineEndingService { get; } = new();
        public FakeDocumentPathRegistry DocumentPathRegistry { get; } = new();
        public FakeConfirmationService ConfirmationService { get; } = new();
        public FakeOpenDocumentProvider OpenDocumentProvider { get; } = new();
        public FakeDocumentActivationService DocumentActivationService { get; } = new();
        public FakeExitConfirmationService ExitConfirmationService { get; } = new();
        public FakeTextInfoService TextInfoService { get; } = new();
        public FakeSearchService SearchService { get; } = new();
        public FakeReplaceService ReplaceService { get; } = new();
        public FakeDiffService DiffService { get; } = new();
        public FakeDocumentContentProvider DocumentContentProvider { get; } = new();
        public Workspace Workspace { get; } = new();

        public NewDocumentUseCase NewDocumentUseCase { get; }
        public OpenDocumentUseCase OpenDocumentUseCase { get; }
        public SaveDocumentUseCase SaveDocumentUseCase { get; }
        public SaveDocumentAsUseCase SaveDocumentAsUseCase { get; }
        public CloseDocumentUseCase CloseDocumentUseCase { get; }
        public ExitApplicationUseCase ExitApplicationUseCase { get; }
        public GetTextInfoUseCase GetTextInfoUseCase { get; }
        public SearchTextUseCase SearchTextUseCase { get; }
        public ReplaceTextUseCase ReplaceTextUseCase { get; }
        public CompareDocumentsUseCase CompareDocumentsUseCase { get; }
        public MainWindowViewModel ViewModel { get; }

        public Harness()
        {
            NewDocumentUseCase = new NewDocumentUseCase();
            OpenDocumentUseCase = new OpenDocumentUseCase(
                FileDialogService, FileService, EncodingService, LineEndingService);
            SaveDocumentUseCase = new SaveDocumentUseCase(
                FileService, EncodingService, LineEndingService);
            SaveDocumentAsUseCase = new SaveDocumentAsUseCase(
                FileDialogService, DocumentPathRegistry, FileService,
                EncodingService, LineEndingService);
            CloseDocumentUseCase = new CloseDocumentUseCase(
                ConfirmationService, SaveDocumentUseCase, SaveDocumentAsUseCase);
            ExitApplicationUseCase = new ExitApplicationUseCase(
                OpenDocumentProvider, DocumentActivationService,
                ExitConfirmationService, NotificationService,
                SaveDocumentUseCase, SaveDocumentAsUseCase);
            GetTextInfoUseCase = new GetTextInfoUseCase(TextInfoService);
            SearchTextUseCase = new SearchTextUseCase(SearchService);
            ReplaceTextUseCase = new ReplaceTextUseCase(ReplaceService);
            CompareDocumentsUseCase = new CompareDocumentsUseCase(
                FileService, EncodingService, LineEndingService,
                DiffService, DocumentContentProvider);

            ViewModel = new MainWindowViewModel(
                NotificationService, FileDialogService,
                NewDocumentUseCase, OpenDocumentUseCase,
                SaveDocumentUseCase, SaveDocumentAsUseCase,
                CloseDocumentUseCase, ExitApplicationUseCase,
                GetTextInfoUseCase, SearchTextUseCase,
                ReplaceTextUseCase, CompareDocumentsUseCase,
                Workspace);
        }

        /// <summary>
        /// Documentタブを追加してアクティブにします。
        /// </summary>
        public DocumentTabViewModel SetActiveDocument(
            string? filePath = null,
            string? fileName = null,
            string content = "content",
            bool isReadOnly = false)
        {
            var document = CreateDocument(filePath, fileName, content, isReadOnly);
            var tab = new DocumentTabViewModel(document);
            Workspace.AddTab(tab);
            return tab;
        }

        /// <summary>
        /// Diffタブを追加してアクティブにします。
        /// </summary>
        public DiffTabViewModel AddDiffTab(int diffCount)
        {
            var tab = CreateDiffTab(diffCount);
            Workspace.AddTab(tab);
            return tab;
        }
    }

    private sealed class FakeNotificationService : IUserNotificationService
    {
        public List<string> InfoMessages { get; } = new();
        public List<string> ErrorMessages { get; } = new();
        public List<string> WarningMessages { get; } = new();

        public void ShowInfo(string message) => InfoMessages.Add(message);
        public void ShowError(string message) => ErrorMessages.Add(message);
        public void ShowWarning(string message) => WarningMessages.Add(message);
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string? OpenPath { get; set; }
        public string? SavePath { get; set; }

        public string? ShowOpenFileDialog() => OpenPath;
        public string? ShowSaveFileDialog(string? initialFilePath) => SavePath;
    }

    private sealed class FakeFileService : IFileService
    {
        public List<(string Path, byte[] Bytes)> WrittenFiles { get; } = new();
        public Exception? ReadException { get; set; }
        public Exception? WriteException { get; set; }

        public FileReadResult ReadAllBytes(string filePath)
        {
            if (ReadException != null)
                throw ReadException;
            return new FileReadResult(Array.Empty<byte>(), false);
        }

        public void WriteAllBytes(string filePath, byte[] bytes)
        {
            if (WriteException != null)
                throw WriteException;
            WrittenFiles.Add((filePath, bytes));
        }
    }

    private sealed class FakeEncodingService : IEncodingService
    {
        public DecodedText Decode(byte[] bytes) => new(string.Empty, TextEncoding.Utf8);

        public byte[] Encode(string text, TextEncoding encoding)
        {
            return System.Text.Encoding.UTF8.GetBytes(text);
        }
    }

    private sealed class FakeLineEndingService : ILineEndingService
    {
        public LineEnding Detect(string text) => LineEnding.CrLf;
        public string NormalizeToLf(string text) => text;
        public string ConvertFromLf(string text, LineEnding lineEnding) => text;
    }

    private sealed class FakeDocumentPathRegistry : IDocumentPathRegistry
    {
        public bool IsPathInUseResult { get; set; }

        public bool IsPathInUse(string filePath, Guid excludingDocumentId) => IsPathInUseResult;
    }

    private sealed class FakeConfirmationService : IUserConfirmationService
    {
        public SaveConfirmationResult Result { get; set; } = SaveConfirmationResult.Save;
        public string? LastDocumentTitle { get; private set; }

        public SaveConfirmationResult ConfirmSaveChanges(string documentTitle)
        {
            LastDocumentTitle = documentTitle;
            return Result;
        }
    }

    private sealed class FakeOpenDocumentProvider : IOpenDocumentProvider
    {
        public List<OpenDocumentInfo> Documents { get; } = new();

        public IReadOnlyList<OpenDocumentInfo> GetOpenDocuments() => Documents;
    }

    private sealed class FakeDocumentActivationService : IDocumentActivationService
    {
        public List<Guid> ActivatedIds { get; } = new();

        public void ActivateDocument(Guid documentId) => ActivatedIds.Add(documentId);
    }

    private sealed class FakeExitConfirmationService : IExitConfirmationService
    {
        public ExitConfirmationResult Result { get; set; } = ExitConfirmationResult.ExitWithoutSaving;
        public List<string> LastDocumentNames { get; private set; } = new();

        public ExitConfirmationResult ConfirmExit(IReadOnlyList<string> documentNames)
        {
            LastDocumentNames = documentNames.ToList();
            return Result;
        }
    }

    private sealed class FakeTextInfoService : ITextInfoService
    {
        public TextInfo GetTextInfo(string content, TextEncoding encoding, LineEnding lineEnding)
        {
            return new TextInfo(1, content.Length, 0, encoding, lineEnding);
        }
    }

    private sealed class FakeSearchService : ISearchService
    {
        public List<SearchResult> Results { get; } = new();

        public IReadOnlyList<SearchResult> Search(string content, SearchCondition condition)
        {
            return Results;
        }
    }

    private sealed class FakeReplaceService : IReplaceService
    {
        public ReplaceResult FirstResult { get; set; } = new(string.Empty, 0, null, null, null);
        public ReplaceResult AllResult { get; set; } = new(string.Empty, 0, null, null, null);

        public ReplaceResult ReplaceFirst(
            string content, SearchCondition condition, string replacement, int startIndex)
        {
            return FirstResult;
        }

        public ReplaceResult ReplaceAll(
            string content, SearchCondition condition, string replacement)
        {
            return AllResult;
        }
    }

    private sealed class FakeDiffService : IDiffService
    {
        public DiffResult Result { get; set; } = new DiffResult("l", "r", Array.Empty<DiffLine>());

        public DiffResult Compare(string leftFileName, string rightFileName, string leftText, string rightText)
        {
            return Result;
        }
    }

    private sealed class FakeDocumentContentProvider : IDocumentContentProvider
    {
        public Document GetDocument(Guid documentId) => throw new DocumentNotFoundException("not found");
        public string GetDisplayTitle(Guid documentId) => throw new DocumentNotFoundException("not found");
    }
}
