namespace TextWorkbench.Application.Exceptions;

/// <summary>
/// TextWorkbenchで発生する例外の基底クラスです。
/// </summary>
public abstract class TextWorkbenchException : Exception
{
    /// <summary>
    /// <see cref="TextWorkbenchException"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="message">ユーザー向けメッセージ</param>
    protected TextWorkbenchException(string message)
        : base(message)
    {
    }
}
