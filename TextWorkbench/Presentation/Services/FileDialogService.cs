using Microsoft.Win32;
using TextWorkbench.Application.Interfaces;

namespace TextWorkbench.Presentation.Services;

/// <summary>
/// WPFのFileDialogを使用したファイルダイアログサービスの実装です。
/// </summary>
public sealed class FileDialogService : IFileDialogService
{
    /// <summary>
    /// ファイルを開くダイアログを表示します。
    /// </summary>
    /// <returns>選択されたファイルパス、キャンセル時は null</returns>
    public string? ShowOpenFileDialog()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "*.*|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            return dialog.FileName;
        }

        return null;
    }

    /// <summary>
    /// ファイルを保存するダイアログを表示します。
    /// </summary>
    /// <param name="initialFilePath">初期ファイルパス</param>
    /// <returns>選択されたファイルパス、キャンセル時は null</returns>
    public string? ShowSaveFileDialog(string? initialFilePath)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "*.*|*.*"
        };

        if (!string.IsNullOrEmpty(initialFilePath))
        {
            dialog.FileName = initialFilePath;
        }

        if (dialog.ShowDialog() == true)
        {
            return dialog.FileName;
        }

        return null;
    }
}
