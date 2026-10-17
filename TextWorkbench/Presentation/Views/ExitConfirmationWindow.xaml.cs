using System.Windows;
using TextWorkbench.Application.Interfaces;

namespace TextWorkbench.Presentation.Views;

/// <summary>
/// アプリケーション終了時に未保存Documentの処理方針を確認するDialogです。
/// </summary>
public partial class ExitConfirmationWindow : Window
{
    /// <summary>
    /// ユーザーが選択した終了確認結果を取得します。
    /// 初期値はキャンセルです。
    /// </summary>
    public ExitConfirmationResult ConfirmationResult { get; private set; }
        = ExitConfirmationResult.Cancel;

    /// <summary>
    /// <see cref="ExitConfirmationWindow"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="documentNames">Workspaceのタブ順に並んだ未保存Document名</param>
    public ExitConfirmationWindow(IReadOnlyList<string> documentNames)
    {
        ArgumentNullException.ThrowIfNull(documentNames);

        InitializeComponent();
        DocumentList.ItemsSource = documentNames.ToArray();
    }

    private void SaveAndExit_Click(object sender, RoutedEventArgs e)
    {
        ConfirmationResult = ExitConfirmationResult.SaveAndExit;
        DialogResult = true;
    }

    private void ExitWithoutSaving_Click(object sender, RoutedEventArgs e)
    {
        ConfirmationResult = ExitConfirmationResult.ExitWithoutSaving;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        ConfirmationResult = ExitConfirmationResult.Cancel;
        DialogResult = false;
    }
}
