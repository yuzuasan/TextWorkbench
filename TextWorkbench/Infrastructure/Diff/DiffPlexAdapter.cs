using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Infrastructure.Diff;

/// <summary>
/// DiffPlexを使用して左右のテキストを比較するAdapterです。
/// DiffPlex固有の型はこのクラスの内部に閉じ込めます。
/// </summary>
public sealed class DiffPlexAdapter : IDiffEngineAdapter
{
    private readonly SideBySideDiffBuilder _diffBuilder;

    /// <summary>
    /// <see cref="DiffPlexAdapter"/> の新しいインスタンスを初期化します。
    /// </summary>
    public DiffPlexAdapter()
    {
        // 文字単位の差分を取得するため、行ChunkerとCharacterChunkerを指定します。
        _diffBuilder = new SideBySideDiffBuilder(
            differ: null,
            lineChunker: new LineChunker(),
            wordChunker: CharacterChunker.Instance);
    }

    /// <inheritdoc />
    public DiffEngineResult Compare(string leftText, string rightText)
    {
        ArgumentNullException.ThrowIfNull(leftText);
        ArgumentNullException.ThrowIfNull(rightText);

        // 空白・タブも差分として扱います。改行コードの正規化は呼び出し側の責務です。
        var diffModel = _diffBuilder.BuildDiffModel(
            leftText,
            rightText,
            ignoreWhitespace: false,
            ignoreCase: false);

        // SideBySideDiffBuilderは左右の行を同じ表示位置に並べます。
        // 片側にしか行がない位置にはImaginaryが設定された行が入ります。
        var leftLines = diffModel.OldText.Lines;
        var rightLines = diffModel.NewText.Lines;
        var lineCount = Math.Max(leftLines.Count, rightLines.Count);
        var resultLines = new List<DiffEngineLine>(lineCount);

        for (var index = 0; index < lineCount; index++)
        {
            var leftLine = index < leftLines.Count ? leftLines[index] : null;
            var rightLine = index < rightLines.Count ? rightLines[index] : null;

            var isLeftImaginary = IsImaginary(leftLine);
            var isRightImaginary = IsImaginary(rightLine);

            // DiffPlexのImaginary行は実データではないため、独自モデルでは行番号をnullにします。
            var lineType = GetLineType(leftLine, rightLine, isLeftImaginary, isRightImaginary);
            var leftLineNumber = isLeftImaginary ? null : leftLine!.Position;
            var rightLineNumber = isRightImaginary ? null : rightLine!.Position;

            var leftSegments = CreateSegments(leftLine, isLeftImaginary, lineType, isLeft: true);
            var rightSegments = CreateSegments(rightLine, isRightImaginary, lineType, isLeft: false);

            resultLines.Add(new DiffEngineLine(
                leftLineNumber,
                rightLineNumber,
                lineType,
                leftSegments,
                rightSegments));
        }

        return new DiffEngineResult(resultLines);
    }

    /// <summary>
    /// 指定された行がDiffPlexのPlaceholder（Imaginary）かどうかを判定します。
    /// </summary>
    private static bool IsImaginary(DiffPiece? piece)
    {
        return piece is null || piece.Type == ChangeType.Imaginary;
    }

    /// <summary>
    /// 左右の行の状態から、TextWorkbench側の行変更タイプを決定します。
    /// </summary>
    private static DiffLineType GetLineType(
        DiffPiece? leftLine,
        DiffPiece? rightLine,
        bool isLeftImaginary,
        bool isRightImaginary)
    {
        // 左側に行がなければ右側への追加、右側に行がなければ左側からの削除です。
        if (isLeftImaginary)
            return DiffLineType.Added;

        if (isRightImaginary)
            return DiffLineType.Deleted;

        if (leftLine!.Type == ChangeType.Unchanged
            && rightLine!.Type == ChangeType.Unchanged)
        {
            return DiffLineType.Unchanged;
        }

        return DiffLineType.Changed;
    }

    /// <summary>
    /// DiffPlexの文字単位のSubPiecesを、独自のセグメント一覧へ変換します。
    /// </summary>
    private static IReadOnlyList<DiffEngineSegment> CreateSegments(
        DiffPiece? line,
        bool isImaginary,
        DiffLineType lineType,
        bool isLeft)
    {
        if (isImaginary || line is null)
            return Array.Empty<DiffEngineSegment>();

        if (lineType == DiffLineType.Added)
        {
            return
            [
                new DiffEngineSegment(line.Text ?? string.Empty, DiffSegmentType.Added)
            ];
        }

        if (lineType == DiffLineType.Deleted)
        {
            return
            [
                new DiffEngineSegment(line.Text ?? string.Empty, DiffSegmentType.Deleted)
            ];
        }

        // 変更のない行ではSubPiecesが空の場合があるため、行全体を1セグメントとして扱います。
        if (line.SubPieces.Count == 0)
        {
            var fallbackType = lineType == DiffLineType.Unchanged
                ? DiffSegmentType.Unchanged
                : isLeft ? DiffSegmentType.Deleted : DiffSegmentType.Added;

            return
            [
                new DiffEngineSegment(line.Text ?? string.Empty, fallbackType)
            ];
        }

        var segments = new List<DiffEngineSegment>(line.SubPieces.Count);

        // 左側の削除文字はDeleted、右側の挿入文字はAddedとして表現します。
        foreach (var piece in line.SubPieces)
        {
            var segmentType = GetSegmentType(piece.Type, isLeft);

            if (segmentType is not null)
            {
                segments.Add(new DiffEngineSegment(
                    piece.Text ?? string.Empty,
                    segmentType.Value));
            }
        }

        return segments;
    }

    /// <summary>
    /// DiffPlexの文字変更タイプをTextWorkbench側のセグメントタイプへ変換します。
    /// </summary>
    private static DiffSegmentType? GetSegmentType(ChangeType changeType, bool isLeft)
    {
        return changeType switch
        {
            ChangeType.Unchanged => DiffSegmentType.Unchanged,
            ChangeType.Deleted => DiffSegmentType.Deleted,
            ChangeType.Inserted => DiffSegmentType.Added,
            // Modifiedは左右の文字列に対して、左を削除・右を追加として表現します。
            ChangeType.Modified => isLeft
                ? DiffSegmentType.Deleted
                : DiffSegmentType.Added,
            ChangeType.Imaginary => null,
            _ => throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null)
        };
    }
}
