namespace TextWorkbench.Application.Exceptions;

/// <summary>
/// エンコーディング操作で発生した例外を表します。
/// </summary>
public sealed class EncodingOperationException : TextWorkbenchException
{
    /// <summary>
    /// <see cref="EncodingOperationException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">ユーザー向けメッセージ</param>
    public EncodingOperationException(string message)
        : base(message)
    {
    }
}
