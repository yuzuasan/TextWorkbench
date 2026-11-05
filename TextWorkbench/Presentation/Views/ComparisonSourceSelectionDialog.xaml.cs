using System.Windows;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Presentation.Views;

/// <summary>
/// 比較元選択ダイアログです。
/// </summary>
public partial class ComparisonSourceSelectionDialog : Window
{
    /// <summary>
    /// <see cref="ComparisonSourceSelectionDialog"/> の新しいインスタンスを初期化します。
    /// </summary>
    public ComparisonSourceSelectionDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// DataContextが変更されたときにイベントを購読します。
    /// </summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ComparisonSourceSelectionDialogViewModel oldViewModel)
        {
            oldViewModel.CloseRequested -= OnCloseRequested;
        }

        if (e.NewValue is ComparisonSourceSelectionDialogViewModel newViewModel)
        {
            newViewModel.CloseRequested += OnCloseRequested;
        }
    }

    /// <summary>
    /// ダイアログを閉じるイベントを処理します。
    /// </summary>
    private void OnCloseRequested(object? sender, bool dialogResult)
    {
        DialogResult = dialogResult;
        Close();
    }
}
