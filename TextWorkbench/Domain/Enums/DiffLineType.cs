namespace TextWorkbench.Domain.Enums;

/// <summary>
/// Diffの行レベルの変更タイプを表します。
/// </summary>
public enum DiffLineType
{
    /// <summary>
    /// 変更なし
    /// </summary>
    Unchanged,

    /// <summary>
    /// 追加（右側にのみ存在）
    /// </summary>
    Added,

    /// <summary>
    /// 削除（左側にのみ存在）
    /// </summary>
    Deleted,

    /// <summary>
    /// 変更（左右の対応行が存在するが内容が異なる）
    /// </summary>
    Changed
}
