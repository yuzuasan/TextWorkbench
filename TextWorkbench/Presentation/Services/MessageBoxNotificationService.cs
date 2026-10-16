using System.Windows;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Presentation.Constants;

namespace TextWorkbench.Presentation.Services;

/// <summary>
/// MessageBoxを使用した通知サービスの実装です。
/// </summary>
public sealed class MessageBoxNotificationService : IUserNotificationService
{
    /// <summary>
    /// 情報メッセージを表示します。
    /// </summary>
    /// <param name="message">メッセージ</param>
    public void ShowInfo(string message)
    {
        MessageBox.Show(message, ApplicationInfo.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// エラーメッセージを表示します。
    /// </summary>
    /// <param name="message">メッセージ</param>
    public void ShowError(string message)
    {
        MessageBox.Show(message, ApplicationInfo.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// 警告メッセージを表示します。
    /// </summary>
    /// <param name="message">メッセージ</param>
    public void ShowWarning(string message)
    {
        MessageBox.Show(message, ApplicationInfo.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
