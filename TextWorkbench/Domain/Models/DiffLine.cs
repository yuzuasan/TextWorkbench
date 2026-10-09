using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Domain.Models;

/// <summary>
/// Diffの行を表します。
/// </summary>
public sealed class DiffLine
{
    /// <summary>
    /// 左側の行番号を取得します。存在しない場合は null。
    /// </summary>
    public int? LeftLineNumber { get; }

    /// <summary>
    /// 右側の行番号を取得します。存在しない場合は null。
    /// </summary>
    public int? RightLineNumber { get; }

    /// <summary>
    /// 行の変更タイプを取得します。
    /// </summary>
    public DiffLineType Type { get; }

    /// <summary>
    /// 左側のセグメント一覧を取得します。
    /// </summary>
    public IReadOnlyList<DiffSegment> LeftSegments { get; }

    /// <summary>
    /// 右側のセグメント一覧を取得します。
    /// </summary>
    public IReadOnlyList<DiffSegment> RightSegments { get; }

    /// <summary>
    /// <see cref="DiffLine"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="leftLineNumber">左側の行番号</param>
    /// <param name="rightLineNumber">右側の行番号</param>
    /// <param name="type">行の変更タイプ</param>
    /// <param name="leftSegments">左側のセグメント一覧</param>
    /// <param name="rightSegments">右側のセグメント一覧</param>
    public DiffLine(
        int? leftLineNumber,
        int? rightLineNumber,
        DiffLineType type,
        IReadOnlyList<DiffSegment> leftSegments,
        IReadOnlyList<DiffSegment> rightSegments)
    {
        if (leftLineNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(leftLineNumber), "左側の行番号は0以上である必要があります。");

        if (rightLineNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(rightLineNumber), "右側の行番号は0以上である必要があります。");

        LeftLineNumber = leftLineNumber;
        RightLineNumber = rightLineNumber;
        Type = type;
        LeftSegments = leftSegments ?? throw new ArgumentNullException(nameof(leftSegments));
        RightSegments = rightSegments ?? throw new ArgumentNullException(nameof(rightSegments));
    }
}
