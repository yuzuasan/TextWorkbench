namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// ファイルダイアログサービスのインターフェースです。
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// ファイルを開くダイアログを表示します。
    /// </summary>
    /// <returns>選択されたファイルパス、キャンセル時は null</returns>
    string? ShowOpenFileDialog();

    /// <summary>
    /// ファイルを保存するダイアログを表示します。
    /// </summary>
    /// <param name="initialFilePath">初期ファイルパス</param>
    /// <returns>選択されたファイルパス、キャンセル時は null</returns>
    string? ShowSaveFileDialog(string? initialFilePath);
}
