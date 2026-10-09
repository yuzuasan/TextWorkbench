using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Domain.Models;

/// <summary>
/// Diffの文字セグメントを表します。
/// </summary>
public sealed class DiffSegment
{
    /// <summary>
    /// セグメントのテキストを取得します。
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// セグメントの変更タイプを取得します。
    /// </summary>
    public DiffSegmentType Type { get; }

    /// <summary>
    /// <see cref="DiffSegment"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="text">セグメントのテキスト</param>
    /// <param name="type">セグメントの変更タイプ</param>
    public DiffSegment(string text, DiffSegmentType type)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Type = type;
    }
}
