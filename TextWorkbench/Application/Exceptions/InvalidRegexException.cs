namespace TextWorkbench.Application.Exceptions;

/// <summary>
/// 正規表現が無効であることを示す例外です。
/// </summary>
public sealed class InvalidRegexException : TextWorkbenchException
{
    /// <summary>
    /// <see cref="InvalidRegexException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">ユーザー向けメッセージ</param>
    public InvalidRegexException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// <see cref="InvalidRegexException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">ユーザー向けメッセージ</param>
    /// <param name="innerException">内部例外</param>
    public InvalidRegexException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
