namespace TextWorkbench.Application.Exceptions;

/// <summary>
/// 指定したIDのDocumentが見つからない場合にスローされる例外です。
/// </summary>
public sealed class DocumentNotFoundException : TextWorkbenchException
{
    /// <summary>
    /// <see cref="DocumentNotFoundException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">ユーザー向けメッセージ</param>
    public DocumentNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// <see cref="DocumentNotFoundException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">ユーザー向けメッセージ</param>
    /// <param name="innerException">内部例外</param>
    public DocumentNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
