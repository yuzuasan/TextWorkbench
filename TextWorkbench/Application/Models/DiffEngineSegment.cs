using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Application.Models;

/// <summary>
/// Diffエンジンによる文字セグメントを表すApplication独自モデルです。
/// </summary>
public sealed class DiffEngineSegment
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
    /// <see cref="DiffEngineSegment"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="text">セグメントのテキスト</param>
    /// <param name="type">セグメントの変更タイプ</param>
    public DiffEngineSegment(string text, DiffSegmentType type)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Type = type;
    }
}
