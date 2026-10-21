namespace TextWorkbench.Application.Exceptions;

/// <summary>
/// 検証または操作の妥当性チェックで発生した例外を表します。
/// </summary>
public sealed class ValidationOperationException : TextWorkbenchException
{
    /// <summary>
    /// <see cref="ValidationOperationException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">ユーザー向けメッセージ</param>
    public ValidationOperationException(string message)
        : base(message)
    {
    }
}
