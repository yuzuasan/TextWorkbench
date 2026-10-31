namespace TextWorkbench.Application.Models;

/// <summary>
/// Diffエンジンによる比較結果を表すApplication独自モデルです。
/// </summary>
public sealed class DiffEngineResult
{
    /// <summary>
    /// 比較結果の行一覧を取得します。
    /// </summary>
    public IReadOnlyList<DiffEngineLine> Lines { get; }

    /// <summary>
    /// <see cref="DiffEngineResult"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="lines">比較結果の行一覧</param>
    public DiffEngineResult(IReadOnlyList<DiffEngineLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        Lines = Array.AsReadOnly(lines.ToArray());
    }
}
