namespace TextWorkbench.Domain.Models;

/// <summary>
/// 検索結果の位置情報を表します。
/// </summary>
public sealed class SearchResult
{
    /// <summary>
    /// 検索結果の開始インデックスを取得します。
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// 検索結果の長さを取得します。
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// <see cref="SearchResult"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="startIndex">検索結果の開始インデックス</param>
    /// <param name="length">検索結果の長さ</param>
    public SearchResult(int startIndex, int length)
    {
        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "開始インデックスは0以上である必要があります。");

        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), "長さは0以上である必要があります。");

        StartIndex = startIndex;
        Length = length;
    }
}
