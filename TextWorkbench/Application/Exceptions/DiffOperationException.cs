namespace TextWorkbench.Application.Exceptions;

/// <summary>
/// Diff操作で発生した例外を表します。
/// </summary>
public sealed class DiffOperationException : TextWorkbenchException
{
    /// <summary>
    /// <see cref="DiffOperationException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">ユーザー向けメッセージ</param>
    public DiffOperationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// <see cref="DiffOperationException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">ユーザー向けメッセージ</param>
    /// <param name="innerException">内部例外</param>
    public DiffOperationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
