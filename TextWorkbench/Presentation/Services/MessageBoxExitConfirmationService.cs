using TextWorkbench.Application.Interfaces;
using TextWorkbench.Presentation.Views;

namespace TextWorkbench.Presentation.Services;

/// <summary>
/// WPFの終了確認ダイアログを使用した終了確認サービスです。
/// </summary>
public sealed class MessageBoxExitConfirmationService : IExitConfirmationService
{
    /// <summary>
    /// 未保存Documentの一覧を表示し、終了時の処理方針を確認します。
    /// </summary>
    /// <param name="documentNames">Workspaceのタブ順に並んだ未保存Document名</param>
    /// <returns>保存して終了、保存せず終了、キャンセルのいずれか</returns>
    public ExitConfirmationResult ConfirmExit(IReadOnlyList<string> documentNames)
    {
        ArgumentNullException.ThrowIfNull(documentNames);

        var dialog = new ExitConfirmationWindow(documentNames)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };

        dialog.ShowDialog();
        return dialog.ConfirmationResult;
    }
}
