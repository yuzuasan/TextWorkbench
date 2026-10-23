using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Presentation.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContextChanged += MainWindow_DataContextChanged;
            Closing += MainWindow_Closing;
            SearchReplacePanelView.IsVisibleChanged += SearchReplacePanel_IsVisibleChanged;
        }

        /// <summary>
        /// DataContextの変更に応じて終了要求イベントを購読します。
        /// </summary>
        private void MainWindow_DataContextChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MainWindowViewModel oldViewModel)
            {
                oldViewModel.ExitRequested -= MainWindow_ExitRequested;
            }

            if (e.NewValue is MainWindowViewModel newViewModel)
            {
                newViewModel.ExitRequested += MainWindow_ExitRequested;
            }
        }

        /// <summary>
        /// タブ選択変更後、選択中のドキュメントエディタにフォーカスを設定します。
        /// </summary>
        private void MainTabControl_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            // 子要素からバブルアップしたSelectionChangedは対象外にします。
            if (!ReferenceEquals(e.OriginalSource, MainTabControl))
            {
                return;
            }

            Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(FocusActiveDocumentEditor));
        }

        /// <summary>
        /// 検索パネルが閉じられたとき、アクティブなエディタにフォーカスを戻します。
        /// </summary>
        private void SearchReplacePanel_IsVisibleChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not false)
            {
                return;
            }

            Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(FocusActiveDocumentEditor));
        }

        /// <summary>
        /// 選択中のタブに対応するDocumentViewへフォーカス設定を依頼します。
        /// </summary>
        private void FocusActiveDocumentEditor()
        {
            if (!IsLoaded || !IsActive)
            {
                return;
            }

            if (MainTabControl.SelectedItem is not DocumentTabViewModel)
            {
                return;
            }

            var documentView = FindVisualChild<DocumentView>(MainTabControl);

            documentView?.FocusEditor();
        }

        /// <summary>
        /// 指定された型のビジュアル子要素を検索します。
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            var childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T target)
                {
                    return target;
                }

                var descendant = FindVisualChild<T>(child);

                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null;
        }

        /// <summary>
        /// ViewModelからの終了要求を受けてウィンドウを閉じます。
        /// 終了可否はClosingイベントで確認します。
        /// </summary>
        private void MainWindow_ExitRequested(object? sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// ウィンドウ終了時にViewModelへ終了可否を確認します。
        /// </summary>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is not MainWindowViewModel viewModel)
            {
                e.Cancel = true;
                return;
            }

            e.Cancel = !viewModel.CanExit();
        }
    }
}
