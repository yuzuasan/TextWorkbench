using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TextWorkbench.Presentation.Views;

/// <summary>
/// SearchReplacePanel.xaml の相互作用ロジック
/// </summary>
public partial class SearchReplacePanel : UserControl
{
    public SearchReplacePanel()
    {
        InitializeComponent();

        IsVisibleChanged += OnIsVisibleChanged;
    }

    /// <summary>
    /// 検索パネルが表示されたとき、検索テキストボックスにフォーカスを設定します。
    /// </summary>
    private void OnIsVisibleChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() =>
            {
                if (!IsVisible || !SearchTextBox.IsVisible)
                {
                    return;
                }

                SearchTextBox.Focus();
                SearchTextBox.SelectAll();
            }));
    }
}
