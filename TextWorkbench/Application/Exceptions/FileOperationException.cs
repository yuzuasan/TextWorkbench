namespace TextWorkbench.Application.Exceptions;

/// <summary>
/// ファイル操作で発生した例外を表します。
/// </summary>
public sealed class FileOperationException : Exception
{
    /// <summary>
    /// <see cref="FileOperationException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">ユーザー向けメッセージ</param>
    public FileOperationException(string message)
        : base(message)
    {
    }
}
