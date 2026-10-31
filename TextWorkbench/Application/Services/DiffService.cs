using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.Services;

/// <summary>
/// Diffエンジンの結果をTextWorkbench独自のDiffモデルへ変換します。
/// </summary>
public sealed class DiffService : IDiffService
{
    private readonly IDiffEngineAdapter _diffEngineAdapter;

    /// <summary>
    /// <see cref="DiffService"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="diffEngineAdapter">DiffエンジンAdapter</param>
    public DiffService(IDiffEngineAdapter diffEngineAdapter)
    {
        _diffEngineAdapter = diffEngineAdapter
            ?? throw new ArgumentNullException(nameof(diffEngineAdapter));
    }

    /// <inheritdoc />
    public DiffResult Compare(
        string leftFileName,
        string rightFileName,
        string leftText,
        string rightText)
    {
        ArgumentNullException.ThrowIfNull(leftFileName);
        ArgumentNullException.ThrowIfNull(rightFileName);
        ArgumentNullException.ThrowIfNull(leftText);
        ArgumentNullException.ThrowIfNull(rightText);

        var engineResult = _diffEngineAdapter.Compare(leftText, rightText)
            ?? throw new InvalidOperationException("Diffエンジンから結果が返されませんでした。");

        var lines = engineResult.Lines
            .Select(line => new DiffLine(
                line.LeftLineNumber,
                line.RightLineNumber,
                line.Type,
                line.LeftSegments
                    .Select(segment => new DiffSegment(segment.Text, segment.Type))
                    .ToArray(),
                line.RightSegments
                    .Select(segment => new DiffSegment(segment.Text, segment.Type))
                    .ToArray()))
            .ToArray();

        // DiffCountはDiffResult側で、Unchanged以外のDiffLine数として算出します。
        return new DiffResult(leftFileName, rightFileName, lines);
    }
}
