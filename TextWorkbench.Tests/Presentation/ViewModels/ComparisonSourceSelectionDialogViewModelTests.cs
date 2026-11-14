using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Tests.Presentation.ViewModels;

/// <summary>
/// <see cref="ComparisonSourceSelectionDialogViewModel"/> の単体テストです。
/// </summary>
public class ComparisonSourceSelectionDialogViewModelTests
{
    // コンストラクタ・候補抽出

    [Fact]
    public void コンストラクタ_fileDialogServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new ComparisonSourceSelectionDialogViewModel(
            new Workspace(), null!));
    }

    [Fact]
    public void コンストラクタ_workspaceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new ComparisonSourceSelectionDialogViewModel(
            null!, new FakeFileDialogService()));
    }

    [Fact]
    public void コンストラクタ_DocumentTabのみが候補に含まれDiffTabは除外される()
    {
        var workspace = new Workspace();
        var docTab = new DocumentTabViewModel(new Document());
        workspace.AddTab(docTab);
        workspace.AddTab(CreateDiffTab());

        var viewModel = new ComparisonSourceSelectionDialogViewModel(
            workspace, new FakeFileDialogService());

        Assert.Single(viewModel.DocumentCandidates);
        Assert.Same(docTab, viewModel.DocumentCandidates[0]);
        Assert.True(viewModel.HasDocumentCandidates);
    }

    [Fact]
    public void コンストラクタ_読み込み中と保存中のDocumentTabは候補から除外される()
    {
        var workspace = new Workspace();
        var loadingTab = new DocumentTabViewModel(new Document());
        loadingTab.Document.StartLoading();
        var savingTab = new DocumentTabViewModel(new Document());
        savingTab.Document.StartSaving();
        var normalTab = new DocumentTabViewModel(new Document());
        workspace.AddTab(loadingTab);
        workspace.AddTab(savingTab);
        workspace.AddTab(normalTab);

        var viewModel = new ComparisonSourceSelectionDialogViewModel(
            workspace, new FakeFileDialogService());

        Assert.Single(viewModel.DocumentCandidates);
        Assert.Same(normalTab, viewModel.DocumentCandidates[0]);
    }

    [Fact]
    public void コンストラクタ_候補が0件の場合_HasDocumentCandidatesがfalseになる()
    {
        var viewModel = new ComparisonSourceSelectionDialogViewModel(
            new Workspace(), new FakeFileDialogService());

        Assert.Empty(viewModel.DocumentCandidates);
        Assert.False(viewModel.HasDocumentCandidates);
    }

    [Fact]
    public void コンストラクタ_Document候補は構築時のスナップショットである()
    {
        var workspace = new Workspace();
        var viewModel = new ComparisonSourceSelectionDialogViewModel(
            workspace, new FakeFileDialogService());

        workspace.AddTab(new DocumentTabViewModel(new Document()));

        Assert.Empty(viewModel.DocumentCandidates);
    }

    // ファイル選択・相互排他

    [Fact]
    public void SelectLeftFile_ファイル選択でパスが設定されDocument選択がクリアされる()
    {
        var harness = new Harness();
        var docTab = harness.AddDocumentTab();
        harness.ViewModel.LeftSelection.SetDocumentTab(docTab);
        harness.FileDialogService.OpenPath = @"C:\work\a.txt";

        harness.ViewModel.SelectLeftFileCommand.Execute(null);

        Assert.Equal(@"C:\work\a.txt", harness.ViewModel.LeftSelection.SelectedFilePath);
        Assert.Null(harness.ViewModel.LeftSelection.SelectedDocumentTab);
    }

    [Fact]
    public void SelectRightFile_左右の選択状態は独立している()
    {
        var harness = new Harness();
        harness.FileDialogService.OpenPath = @"C:\work\right.txt";

        harness.ViewModel.SelectRightFileCommand.Execute(null);

        Assert.Equal(@"C:\work\right.txt", harness.ViewModel.RightSelection.SelectedFilePath);
        Assert.Null(harness.ViewModel.LeftSelection.SelectedFilePath);
        Assert.Null(harness.ViewModel.LeftSelection.SelectedDocumentTab);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SelectLeftFile_ダイアログがキャンセルまたは空の場合_選択状態は変化しない(string? path)
    {
        var harness = new Harness();
        harness.FileDialogService.OpenPath = path;
        harness.ViewModel.LeftSelection.SetDocumentTab(harness.AddDocumentTab());

        harness.ViewModel.SelectLeftFileCommand.Execute(null);

        Assert.Null(harness.ViewModel.LeftSelection.SelectedFilePath);
        Assert.NotNull(harness.ViewModel.LeftSelection.SelectedDocumentTab);
    }

    [Fact]
    public void DocumentTabSelection_設定でファイルパスがクリアされる()
    {
        var harness = new Harness();
        var docTab = harness.AddDocumentTab();
        harness.ViewModel.LeftSelection.SetFilePath(@"C:\work\a.txt");

        harness.ViewModel.LeftSelection.DocumentTabSelection = docTab;

        Assert.Same(docTab, harness.ViewModel.LeftSelection.SelectedDocumentTab);
        Assert.Null(harness.ViewModel.LeftSelection.SelectedFilePath);
    }

    // 表示名・ツールチップ

    [Fact]
    public void SourceDisplayName_Document選択時は開いているドキュメント表記になる()
    {
        var harness = new Harness();
        var docTab = harness.AddDocumentTab(filePath: @"C:\work\a.txt", fileName: "a.txt");
        harness.ViewModel.LeftSelection.SetDocumentTab(docTab);

        Assert.Equal("開いているドキュメント：a.txt", harness.ViewModel.LeftSelection.SourceDisplayName);
    }

    [Fact]
    public void SourceDisplayName_ファイル選択時は保存済みファイル表記とファイル名になる()
    {
        var harness = new Harness();
        harness.ViewModel.LeftSelection.SetFilePath(@"C:\work\dir\a.txt");

        Assert.Equal("保存済みファイル：a.txt", harness.ViewModel.LeftSelection.SourceDisplayName);
    }

    [Fact]
    public void SourceDisplayName_未選択の場合_空文字になる()
    {
        var harness = new Harness();

        Assert.Equal(string.Empty, harness.ViewModel.LeftSelection.SourceDisplayName);
        Assert.Equal(string.Empty, harness.ViewModel.LeftSelection.ToolTipText);
    }

    [Fact]
    public void ToolTipText_Document選択時はFilePath_無題ならDisplayTitleになる()
    {
        var harness = new Harness();
        var fileTab = harness.AddDocumentTab(filePath: @"C:\work\a.txt", fileName: "a.txt");
        harness.ViewModel.LeftSelection.SetDocumentTab(fileTab);
        Assert.Equal(@"C:\work\a.txt", harness.ViewModel.LeftSelection.ToolTipText);

        var untitledTab = harness.AddDocumentTab();
        harness.ViewModel.LeftSelection.SetDocumentTab(untitledTab);
        Assert.Equal(untitledTab.DisplayTitle, harness.ViewModel.LeftSelection.ToolTipText);
    }

    [Fact]
    public void ToolTipText_ファイル選択時はフルパスが返る()
    {
        var harness = new Harness();
        harness.ViewModel.LeftSelection.SetFilePath(@"C:\work\dir\a.txt");

        Assert.Equal(@"C:\work\dir\a.txt", harness.ViewModel.LeftSelection.ToolTipText);
    }

    // 比較実行可否・実行

    [Fact]
    public void CanExecuteCompare_左右両方の選択が必要になる()
    {
        var harness = new Harness();

        Assert.False(harness.ViewModel.CanExecuteCompare);
        Assert.False(harness.ViewModel.ExecuteCompareCommand.CanExecute(null));

        harness.ViewModel.LeftSelection.SetFilePath(@"C:\work\a.txt");
        Assert.False(harness.ViewModel.CanExecuteCompare);
        Assert.False(harness.ViewModel.ExecuteCompareCommand.CanExecute(null));

        harness.ViewModel.RightSelection.SetFilePath(@"C:\work\b.txt");
        Assert.True(harness.ViewModel.CanExecuteCompare);
        Assert.True(harness.ViewModel.ExecuteCompareCommand.CanExecute(null));
    }

    [Fact]
    public void ExecuteCompare_Documentとファイルの組み合わせでComparisonSourceが生成される()
    {
        var harness = new Harness();
        var docTab = harness.AddDocumentTab(filePath: @"C:\work\a.txt", fileName: "a.txt");
        bool? closeResult = null;
        harness.ViewModel.CloseRequested += (_, result) => closeResult = result;

        harness.ViewModel.LeftSelection.SetDocumentTab(docTab);
        harness.ViewModel.RightSelection.SetFilePath(@"C:\work\b.txt");
        harness.ViewModel.ExecuteCompareCommand.Execute(null);

        Assert.True(closeResult);
        Assert.Equal(SourceType.OpenDocument, harness.ViewModel.LeftComparisonSource!.SourceType);
        Assert.Equal(docTab.Document.Id.ToString(), harness.ViewModel.LeftComparisonSource.SourceIdentifier);
        Assert.Equal(SourceType.File, harness.ViewModel.RightComparisonSource!.SourceType);
        Assert.Equal(@"C:\work\b.txt", harness.ViewModel.RightComparisonSource.SourceIdentifier);
    }

    [Fact]
    public void ExecuteCompare_左右を入れ替えても正しいComparisonSourceが生成される()
    {
        var harness = new Harness();
        var docTab = harness.AddDocumentTab();

        harness.ViewModel.LeftSelection.SetFilePath(@"C:\work\a.txt");
        harness.ViewModel.RightSelection.SetDocumentTab(docTab);
        harness.ViewModel.ExecuteCompareCommand.Execute(null);

        Assert.Equal(SourceType.File, harness.ViewModel.LeftComparisonSource!.SourceType);
        Assert.Equal(SourceType.OpenDocument, harness.ViewModel.RightComparisonSource!.SourceType);
        Assert.Equal(docTab.Document.Id.ToString(), harness.ViewModel.RightComparisonSource.SourceIdentifier);
    }

    [Fact]
    public void CancelCommand_CloseRequestedにfalseが通知されComparisonSourceは生成されない()
    {
        var harness = new Harness();
        bool? closeResult = null;
        harness.ViewModel.CloseRequested += (_, result) => closeResult = result;
        harness.ViewModel.LeftSelection.SetFilePath(@"C:\work\a.txt");
        harness.ViewModel.RightSelection.SetFilePath(@"C:\work\b.txt");

        harness.ViewModel.CancelCommand.Execute(null);

        Assert.False(closeResult);
        Assert.Null(harness.ViewModel.LeftComparisonSource);
        Assert.Null(harness.ViewModel.RightComparisonSource);
    }

    // テストデータ作成ヘルパー

    private static DiffTabViewModel CreateDiffTab()
    {
        var result = new DiffResult("l", "r", Array.Empty<DiffLine>());
        var source = new DiffSession.DiffSource(
            SourceType.File, "id", @"C:\work\a.txt", "a.txt",
            TextEncoding.Utf8, "content");
        return new DiffTabViewModel(new DiffSession(source, source, result));
    }

    private static DocumentTabViewModel CreateDocumentTab(
        string? filePath = null, string? fileName = null)
    {
        Document document;
        if (filePath == null)
        {
            document = new Document();
        }
        else
        {
            document = new Document(
                filePath, fileName!, "content",
                TextEncoding.Utf8, LineEnding.CrLf, false);
        }

        return new DocumentTabViewModel(document);
    }

    /// <summary>
    /// ダイアログViewModelとその依存Fakeをまとめて生成します。
    /// </summary>
    private sealed class Harness
    {
        public FakeFileDialogService FileDialogService { get; } = new();
        public Workspace Workspace { get; } = new();
        public ComparisonSourceSelectionDialogViewModel ViewModel { get; }

        public Harness()
        {
            ViewModel = new ComparisonSourceSelectionDialogViewModel(
                Workspace, FileDialogService);
        }

        public DocumentTabViewModel AddDocumentTab(
            string? filePath = null, string? fileName = null)
        {
            var tab = CreateDocumentTab(filePath, fileName);
            Workspace.AddTab(tab);
            return tab;
        }
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string? OpenPath { get; set; }

        public string? ShowOpenFileDialog() => OpenPath;

        public string? ShowSaveFileDialog(string? initialFilePath) => null;
    }
}
