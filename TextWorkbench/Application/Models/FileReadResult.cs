namespace TextWorkbench.Application.Models;

/// <summary>
/// ファイル読み込みの結果を表します。
/// </summary>
public sealed class FileReadResult
{
    /// <summary>
    /// 読み込んだファイルのbyte列を取得します。
    /// </summary>
    public byte[] Content { get; }

    /// <summary>
    /// 読み込み対象ファイルのReadOnly状態を取得します。
    /// </summary>
    public bool IsReadOnly { get; }

    /// <summary>
    /// <see cref="FileReadResult"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="content">読み込んだファイルのbyte列</param>
    /// <param name="isReadOnly">読み込み対象ファイルのReadOnly状態</param>
    public FileReadResult(byte[] content, bool isReadOnly)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        IsReadOnly = isReadOnly;
    }
}
