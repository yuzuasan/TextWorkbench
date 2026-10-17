using System.Windows;
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
