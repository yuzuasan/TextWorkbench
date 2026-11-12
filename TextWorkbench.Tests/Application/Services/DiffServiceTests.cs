using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Application.Services;
using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Tests.Application.Services;

/// <summary>
/// <see cref="DiffService"/> の単体テストです。
/// </summary>
public class DiffServiceTests
{
    // コンストラクタ

    [Fact]
    public void コンストラクタ_adapterがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new DiffService(null!));
    }

    // Compare：ガード

    [Fact]
    public void Compare_leftFileNameがnullの場合_ArgumentNullExceptionが発生する()
    {
        var service = CreateService();

        Assert.Throws<ArgumentNullException>(() =>
            service.Compare(null!, "right.txt", "left", "right"));
    }

    [Fact]
    public void Compare_rightFileNameがnullの場合_ArgumentNullExceptionが発生する()
    {
        var service = CreateService();

        Assert.Throws<ArgumentNullException>(() =>
            service.Compare("left.txt", null!, "left", "right"));
    }

    [Fact]
    public void Compare_leftTextがnullの場合_ArgumentNullExceptionが発生する()
    {
        var service = CreateService();

        Assert.Throws<ArgumentNullException>(() =>
            service.Compare("left.txt", "right.txt", null!, "right"));
    }

    [Fact]
    public void Compare_rightTextがnullの場合_ArgumentNullExceptionが発生する()
    {
        var service = CreateService();

        Assert.Throws<ArgumentNullException>(() =>
            service.Compare("left.txt", "right.txt", "left", null!));
    }

    [Fact]
    public void Compare_adapterがnullを返す場合_InvalidOperationExceptionが発生する()
    {
        var service = new DiffService(new FakeDiffEngineAdapter(null));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.Compare("left.txt", "right.txt", "left", "right"));
        Assert.Equal("Diffエンジンから結果が返されませんでした。", ex.Message);
    }

    // Compare：変換

    [Fact]
    public void Compare_adapterに左右のテキストがそのまま渡される()
    {
        var adapter = new FakeDiffEngineAdapter(new DiffEngineResult(Array.Empty<DiffEngineLine>()));
        var service = new DiffService(adapter);

        service.Compare("left.txt", "right.txt", "left content", "right content");

        Assert.Equal("left content", adapter.LastLeftText);
        Assert.Equal("right content", adapter.LastRightText);
    }

    [Fact]
    public void Compare_ファイル名がDiffResultに設定される()
    {
        var service = CreateService(new DiffEngineResult(Array.Empty<DiffEngineLine>()));

        var result = service.Compare("left.txt", "right.txt", "left", "right");

        Assert.Equal("left.txt", result.LeftFileName);
        Assert.Equal("right.txt", result.RightFileName);
    }

    [Fact]
    public void Compare_DiffEngineLineの行番号とタイプがDiffLineに変換される()
    {
        var engineResult = new DiffEngineResult(new[]
        {
            CreateEngineLine(1, 1, DiffLineType.Changed,
                new[] { CreateSegment("old", DiffSegmentType.Deleted) },
                new[] { CreateSegment("new", DiffSegmentType.Added) }),
        });
        var service = CreateService(engineResult);

        var result = service.Compare("l", "r", "l", "r");

        var line = Assert.Single(result.Lines);
        Assert.Equal(1, line.LeftLineNumber);
        Assert.Equal(1, line.RightLineNumber);
        Assert.Equal(DiffLineType.Changed, line.Type);
    }

    [Fact]
    public void Compare_行番号がnullの行がそのまま引き継がれる()
    {
        var engineResult = new DiffEngineResult(new[]
        {
            CreateEngineLine(null, 1, DiffLineType.Added, null, null),
            CreateEngineLine(2, null, DiffLineType.Deleted, null, null),
        });
        var service = CreateService(engineResult);

        var result = service.Compare("l", "r", "l", "r");

        Assert.Null(result.Lines[0].LeftLineNumber);
        Assert.Equal(1, result.Lines[0].RightLineNumber);
        Assert.Equal(2, result.Lines[1].LeftLineNumber);
        Assert.Null(result.Lines[1].RightLineNumber);
    }

    [Fact]
    public void Compare_DiffEngineSegmentがDiffSegmentに変換される()
    {
        var engineResult = new DiffEngineResult(new[]
        {
            CreateEngineLine(1, 1, DiffLineType.Changed,
                new[]
                {
                    CreateSegment("keep", DiffSegmentType.Unchanged),
                    CreateSegment("old", DiffSegmentType.Deleted),
                },
                new[]
                {
                    CreateSegment("keep", DiffSegmentType.Unchanged),
                    CreateSegment("new", DiffSegmentType.Added),
                }),
        });
        var service = CreateService(engineResult);

        var result = service.Compare("l", "r", "l", "r");

        var line = Assert.Single(result.Lines);
        Assert.Equal(2, line.LeftSegments.Count);
        Assert.Equal("keep", line.LeftSegments[0].Text);
        Assert.Equal(DiffSegmentType.Unchanged, line.LeftSegments[0].Type);
        Assert.Equal("old", line.LeftSegments[1].Text);
        Assert.Equal(DiffSegmentType.Deleted, line.LeftSegments[1].Type);
        Assert.Equal(2, line.RightSegments.Count);
        Assert.Equal("new", line.RightSegments[1].Text);
        Assert.Equal(DiffSegmentType.Added, line.RightSegments[1].Type);
    }

    [Fact]
    public void Compare_DiffCountがUnchanged以外の行数になる()
    {
        var engineResult = new DiffEngineResult(new[]
        {
            CreateEngineLine(1, 1, DiffLineType.Unchanged, null, null),
            CreateEngineLine(2, 2, DiffLineType.Changed, null, null),
            CreateEngineLine(null, 3, DiffLineType.Added, null, null),
            CreateEngineLine(4, null, DiffLineType.Deleted, null, null),
            CreateEngineLine(5, 5, DiffLineType.Unchanged, null, null),
        });
        var service = CreateService(engineResult);

        var result = service.Compare("l", "r", "l", "r");

        Assert.Equal(5, result.Lines.Count);
        Assert.Equal(3, result.DiffCount);
    }

    [Fact]
    public void Compare_空の結果の場合_DiffCountが0になる()
    {
        var service = CreateService(new DiffEngineResult(Array.Empty<DiffEngineLine>()));

        var result = service.Compare("l", "r", "l", "r");

        Assert.Empty(result.Lines);
        Assert.Equal(0, result.DiffCount);
    }

    // テストデータ作成ヘルパー

    private static DiffService CreateService(DiffEngineResult? engineResult = null)
    {
        var adapter = new FakeDiffEngineAdapter(engineResult ?? new DiffEngineResult(Array.Empty<DiffEngineLine>()));
        return new DiffService(adapter);
    }

    private static DiffEngineSegment CreateSegment(string text, DiffSegmentType type)
    {
        return new DiffEngineSegment(text, type);
    }

    private static DiffEngineLine CreateEngineLine(
        int? leftLineNumber,
        int? rightLineNumber,
        DiffLineType type,
        IReadOnlyList<DiffEngineSegment>? leftSegments,
        IReadOnlyList<DiffEngineSegment>? rightSegments)
    {
        return new DiffEngineLine(
            leftLineNumber,
            rightLineNumber,
            type,
            leftSegments ?? Array.Empty<DiffEngineSegment>(),
            rightSegments ?? Array.Empty<DiffEngineSegment>());
    }

    private sealed class FakeDiffEngineAdapter : IDiffEngineAdapter
    {
        private readonly DiffEngineResult? _result;

        public string? LastLeftText { get; private set; }
        public string? LastRightText { get; private set; }

        public FakeDiffEngineAdapter(DiffEngineResult? result)
        {
            _result = result;
        }

        public DiffEngineResult Compare(string leftText, string rightText)
        {
            LastLeftText = leftText;
            LastRightText = rightText;
            return _result!;
        }
    }
}
