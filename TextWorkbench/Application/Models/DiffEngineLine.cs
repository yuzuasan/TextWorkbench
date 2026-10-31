using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Application.Models;

/// <summary>
/// Diffエンジンによる比較結果の1行を表すApplication独自モデルです。
/// </summary>
public sealed class DiffEngineLine
{
    /// <summary>
    /// 左側の行番号を取得します。存在しない場合はnullです。
    /// </summary>
    public int? LeftLineNumber { get; }

    /// <summary>
    /// 右側の行番号を取得します。存在しない場合はnullです。
    /// </summary>
    public int? RightLineNumber { get; }

    /// <summary>
    /// 行の変更タイプを取得します。
    /// </summary>
    public DiffLineType Type { get; }

    /// <summary>
    /// 左側の文字セグメント一覧を取得します。
    /// </summary>
    public IReadOnlyList<DiffEngineSegment> LeftSegments { get; }

    /// <summary>
    /// 右側の文字セグメント一覧を取得します。
    /// </summary>
    public IReadOnlyList<DiffEngineSegment> RightSegments { get; }

    /// <summary>
    /// <see cref="DiffEngineLine"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="leftLineNumber">左側の行番号</param>
    /// <param name="rightLineNumber">右側の行番号</param>
    /// <param name="type">行の変更タイプ</param>
    /// <param name="leftSegments">左側の文字セグメント一覧</param>
    /// <param name="rightSegments">右側の文字セグメント一覧</param>
    public DiffEngineLine(
        int? leftLineNumber,
        int? rightLineNumber,
        DiffLineType type,
        IReadOnlyList<DiffEngineSegment> leftSegments,
        IReadOnlyList<DiffEngineSegment> rightSegments)
    {
        if (leftLineNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(leftLineNumber));

        if (rightLineNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(rightLineNumber));

        ArgumentNullException.ThrowIfNull(leftSegments);
        ArgumentNullException.ThrowIfNull(rightSegments);

        LeftLineNumber = leftLineNumber;
        RightLineNumber = rightLineNumber;
        Type = type;
        LeftSegments = Array.AsReadOnly(leftSegments.ToArray());
        RightSegments = Array.AsReadOnly(rightSegments.ToArray());
    }
}
