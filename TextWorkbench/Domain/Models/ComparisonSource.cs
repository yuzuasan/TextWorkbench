using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Domain.Models;

/// <summary>
/// 比較元を表します。
/// </summary>
public sealed class ComparisonSource
{
    /// <summary>
    /// 比較元の種類を取得します。
    /// </summary>
    public SourceType SourceType { get; }

    /// <summary>
    /// 比較元の識別子（ファイルパス等）を取得します。
    /// </summary>
    public string SourceIdentifier { get; }

    /// <summary>
    /// <see cref="ComparisonSource"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="sourceType">比較元の種類</param>
    /// <param name="sourceIdentifier">比較元の識別子</param>
    public ComparisonSource(SourceType sourceType, string sourceIdentifier)
    {
        SourceType = sourceType;
        SourceIdentifier = sourceIdentifier ?? throw new ArgumentNullException(nameof(sourceIdentifier));
    }
}
