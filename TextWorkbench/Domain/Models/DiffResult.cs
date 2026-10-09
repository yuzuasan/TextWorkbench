namespace TextWorkbench.Domain.Models;

/// <summary>
/// Diffの結果を表します。
/// </summary>
public sealed class DiffResult
{
    /// <summary>
    /// 左側のファイル名を取得します。
    /// </summary>
    public string LeftFileName { get; }

    /// <summary>
    /// 右側のファイル名を取得します。
    /// </summary>
    public string RightFileName { get; }

    /// <summary>
    /// Diffの行一覧を取得します。
    /// </summary>
    public IReadOnlyList<DiffLine> Lines { get; }

    /// <summary>
    /// 差分行グループ数を取得します。
    /// </summary>
    public int DiffCount { get; }

    /// <summary>
    /// <see cref="DiffResult"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="leftFileName">左側のファイル名</param>
    /// <param name="rightFileName">右側のファイル名</param>
    /// <param name="lines">Diffの行一覧</param>
    public DiffResult(string leftFileName, string rightFileName, IReadOnlyList<DiffLine> lines)
    {
        LeftFileName = leftFileName ?? throw new ArgumentNullException(nameof(leftFileName));
        RightFileName = rightFileName ?? throw new ArgumentNullException(nameof(rightFileName));
        Lines = lines ?? throw new ArgumentNullException(nameof(lines));

        DiffCount = lines.Count(line => line.Type != Enums.DiffLineType.Unchanged);
    }
}
