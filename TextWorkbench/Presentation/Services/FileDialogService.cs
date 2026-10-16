using Microsoft.Win32;
using System.IO;
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
        var initialDirectory = string.IsNullOrWhiteSpace(initialFilePath)
            ? null
            : Path.GetDirectoryName(initialFilePath);

        var initialExtension = string.IsNullOrWhiteSpace(initialFilePath)
            ? string.Empty
            : Path.GetExtension(initialFilePath);

        var hasNonTextExtension = !string.IsNullOrEmpty(initialExtension)
            && !string.Equals(initialExtension, ".txt", StringComparison.OrdinalIgnoreCase);

        var dialog = new SaveFileDialog
        {
            Filter = hasNonTextExtension
                ? "すべてのファイル (*.*)|*.*"
                : "テキスト ファイル (*.txt)|*.txt|すべてのファイル (*.*)|*.*",
            FilterIndex = 1,
            DefaultExt = string.IsNullOrEmpty(initialExtension) ? ".txt" : initialExtension,
            AddExtension = true
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        if (!string.IsNullOrWhiteSpace(initialFilePath))
        {
            // フルパスではなくファイル名のみを初期表示する
            dialog.FileName = Path.GetFileName(initialFilePath);
        }

        if (dialog.ShowDialog() == true)
        {
            return dialog.FileName;
        }

        return null;
    }
}
