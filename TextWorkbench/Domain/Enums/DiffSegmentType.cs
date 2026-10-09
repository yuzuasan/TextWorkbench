namespace TextWorkbench.Domain.Enums;

/// <summary>
/// Diffの文字セグメントの変更タイプを表します。
/// </summary>
public enum DiffSegmentType
{
    /// <summary>
    /// 変更なし
    /// </summary>
    Unchanged,

    /// <summary>
    /// 追加
    /// </summary>
    Added,

    /// <summary>
    /// 削除
    /// </summary>
    Deleted
}
