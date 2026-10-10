namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// ユーザーへの通知サービスのインターフェースです。
/// </summary>
public interface IUserNotificationService
{
    /// <summary>
    /// 情報メッセージを表示します。
    /// </summary>
    /// <param name="message">メッセージ</param>
    void ShowInfo(string message);

    /// <summary>
    /// エラーメッセージを表示します。
    /// </summary>
    /// <param name="message">メッセージ</param>
    void ShowError(string message);

    /// <summary>
    /// 警告メッセージを表示します。
    /// </summary>
    /// <param name="message">メッセージ</param>
    void ShowWarning(string message);
}
