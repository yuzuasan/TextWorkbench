using System.Windows;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Presentation.Constants;

namespace TextWorkbench.Presentation.Services;

/// <summary>
/// MessageBoxを使用したユーザー確認サービスの実装です。
/// </summary>
public sealed class MessageBoxConfirmationService : IUserConfirmationService
{
    /// <summary>
    /// Documentの変更内容を保存するかどうかを確認します。
    /// </summary>
    /// <param name="documentTitle">確認対象Documentの表示名</param>
    /// <returns>保存、保存せず続行、キャンセルのいずれか</returns>
    public SaveConfirmationResult ConfirmSaveChanges(string documentTitle)
    {
        if (string.IsNullOrWhiteSpace(documentTitle))
        {
            documentTitle = "無題";
        }

        var result = MessageBox.Show(
            $"「{documentTitle}」への変更を保存しますか？",
            ApplicationInfo.AppName,
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question,
            MessageBoxResult.Cancel);

        return result switch
        {
            MessageBoxResult.Yes => SaveConfirmationResult.Save,
            MessageBoxResult.No => SaveConfirmationResult.DontSave,
            _ => SaveConfirmationResult.Cancel
        };
    }
}
