using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Tests.Presentation.ViewModels;

/// <summary>
/// <see cref="DiffTabViewModel"/> の単体テストです。
/// </summary>
public class DiffTabViewModelTests
{
    // コンストラクタ・プロパティ

    [Fact]
    public void コンストラクタ_diffSessionがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new DiffTabViewModel(null!));
    }

    [Fact]
    public void コンストラクタ_DisplayTitleが左右のDisplayNameで生成される()
    {
        var viewModel = CreateViewModel("left.txt", "right.txt");

        Assert.Equal("left.txt ↔ right.txt", viewModel.DisplayTitle);
    }

    [Fact]
    public void DiffCount_Unchangedを除いた差分数が返る()
    {
        var viewModel = CreateViewModel(lines: new[]
        {
            CreateLine(DiffLineType.Unchanged),
            CreateLine(DiffLineType.Added),
            CreateLine(DiffLineType.Deleted),
        });

        Assert.Equal(2, viewModel.DiffCount);
    }

    [Fact]
    public void HeaderText_SourceTypeに応じたプレフィックスが付く()
    {
        var fileViewModel = CreateViewModel(
            leftSourceType: SourceType.File,
            rightSourceType: SourceType.OpenDocument);

        Assert.Equal("保存済みファイル：left.txt", fileViewModel.LeftHeaderText);
        Assert.Equal("開いているドキュメント：right.txt", fileViewModel.RightHeaderText);
    }

    [Fact]
    public void ToolTipText_FilePathがあればフルパス_なければDisplayNameが返る()
    {
        var viewModel = CreateViewModel(
            leftFilePath: @"C:\work\left.txt",
            rightFilePath: null);

        Assert.Equal(@"C:\work\left.txt", viewModel.LeftToolTipText);
        Assert.Equal("right.txt", viewModel.RightToolTipText);
    }

    // NextDiff / PreviousDiff

    [Fact]
    public void NextDiff_差分が0件の場合_インデックスは変化しない()
    {
        var viewModel = CreateViewModel(lines: new[] { CreateLine(DiffLineType.Unchanged) });

        viewModel.NextDiff();

        Assert.Equal(-1, viewModel.CurrentDiffIndex);
    }

    [Fact]
    public void PreviousDiff_差分が0件の場合_インデックスは変化しない()
    {
        var viewModel = CreateViewModel(lines: new[] { CreateLine(DiffLineType.Unchanged) });

        viewModel.PreviousDiff();

        Assert.Equal(-1, viewModel.CurrentDiffIndex);
    }

    [Fact]
    public void NextDiff_未選択の場合_先頭の差分に移動する()
    {
        var viewModel = CreateViewModel(diffCount:3);

        viewModel.NextDiff();

        Assert.Equal(0, viewModel.CurrentDiffIndex);
    }

    [Fact]
    public void PreviousDiff_未選択の場合_末尾の差分に移動する()
    {
        var viewModel = CreateViewModel(diffCount:3);

        viewModel.PreviousDiff();

        Assert.Equal(2, viewModel.CurrentDiffIndex);
    }

    [Fact]
    public void NextDiff_末尾に到達した後は循環せずそのままになる()
    {
        var viewModel = CreateViewModel(diffCount:2);

        viewModel.NextDiff();
        viewModel.NextDiff();
        viewModel.NextDiff();

        Assert.Equal(1, viewModel.CurrentDiffIndex);
    }

    [Fact]
    public void PreviousDiff_先頭に到達した後は循環せずそのままになる()
    {
        var viewModel = CreateViewModel(diffCount:2);

        viewModel.PreviousDiff();
        viewModel.PreviousDiff();
        viewModel.PreviousDiff();

        Assert.Equal(0, viewModel.CurrentDiffIndex);
    }

    [Fact]
    public void NextPreviousDiff_差分が1件の場合_常にインデックス0になる()
    {
        var viewModel = CreateViewModel(diffCount:1);

        viewModel.NextDiff();
        Assert.Equal(0, viewModel.CurrentDiffIndex);
        viewModel.NextDiff();
        Assert.Equal(0, viewModel.CurrentDiffIndex);

        viewModel.PreviousDiff();
        Assert.Equal(0, viewModel.CurrentDiffIndex);
    }

    // GetCurrentDiffLine

    [Fact]
    public void GetCurrentDiffLine_未選択の場合_nullが返る()
    {
        var viewModel = CreateViewModel(diffCount:1);

        Assert.Null(viewModel.GetCurrentDiffLine());
    }

    [Fact]
    public void GetCurrentDiffLine_Unchanged行はスキップされて差分行が返る()
    {
        var unchangedLine = CreateLine(DiffLineType.Unchanged, 1, 1);
        var addedLine = CreateLine(DiffLineType.Added, null, 2);
        var viewModel = CreateViewModel(lines: new[] { unchangedLine, addedLine });

        viewModel.NextDiff();
        var result = viewModel.GetCurrentDiffLine();

        Assert.Same(addedLine, result);
    }

    [Fact]
    public void GetCurrentDiffLine_インデックスが範囲外の場合_nullが返る()
    {
        var viewModel = CreateViewModel(diffCount:1);
        viewModel.CurrentDiffIndex = 99;

        Assert.Null(viewModel.GetCurrentDiffLine());
    }

    [Fact]
    public void GetCurrentDiffLine_NextDiff後に対応する差分行が取得できる()
    {
        var first = CreateLine(DiffLineType.Added, null, 1);
        var second = CreateLine(DiffLineType.Deleted, 2, null);
        var viewModel = CreateViewModel(lines: new[] { first, second });

        viewModel.NextDiff();
        Assert.Same(first, viewModel.GetCurrentDiffLine());

        viewModel.NextDiff();
        Assert.Same(second, viewModel.GetCurrentDiffLine());
    }

    // テストデータ作成ヘルパー

    private static DiffTabViewModel CreateViewModel(
        string leftName = "left.txt",
        string rightName = "right.txt",
        SourceType leftSourceType = SourceType.File,
        SourceType rightSourceType = SourceType.File,
        string? leftFilePath = @"C:\work\left.txt",
        string? rightFilePath = @"C:\work\right.txt",
        IReadOnlyList<DiffLine>? lines = null,
        int diffCount = 0)
    {
        if (lines == null)
        {
            lines = Enumerable.Range(0, diffCount)
                .Select(_ => CreateLine(DiffLineType.Added, null, _ + 1))
                .ToArray();
        }

        var leftSource = new DiffSession.DiffSource(
            leftSourceType, "left-id", leftFilePath, leftName, TextEncoding.Utf8, "left content");
        var rightSource = new DiffSession.DiffSource(
            rightSourceType, "right-id", rightFilePath, rightName, TextEncoding.Utf8, "right content");
        var result = new DiffResult(leftName, rightName, lines);
        var session = new DiffSession(leftSource, rightSource, result);

        return new DiffTabViewModel(session);
    }

    private static DiffLine CreateLine(DiffLineType type, int? leftLineNumber = null, int? rightLineNumber = null)
    {
        return new DiffLine(
            leftLineNumber, rightLineNumber, type,
            Array.Empty<DiffSegment>(), Array.Empty<DiffSegment>());
    }
}
