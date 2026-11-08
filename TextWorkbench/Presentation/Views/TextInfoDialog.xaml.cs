using System.Windows;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Presentation.Views;

/// <summary>
/// テキスト情報ダイアログのViewです。
/// </summary>
public partial class TextInfoDialog : Window
{
    private TextInfoDialogViewModel? _observedViewModel;

    /// <summary>
    /// <see cref="TextInfoDialog"/> の新しいインスタンスを初期化します。
    /// </summary>
    public TextInfoDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // XAMLで設定されたDataContextはInitializeComponent中に確定するため、
        // ここで現在のViewModelへ直接購読する
        AttachViewModel(DataContext as TextInfoDialogViewModel);
    }

    /// <summary>
    /// DataContextが変更されたときにイベントを購読します。
    /// </summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        AttachViewModel(e.NewValue as TextInfoDialogViewModel);
    }

    /// <summary>
    /// ViewModelのCloseRequestedイベントを購読します。
    /// </summary>
    private void AttachViewModel(TextInfoDialogViewModel? viewModel)
    {
        if (ReferenceEquals(_observedViewModel, viewModel))
        {
            return;
        }

        if (_observedViewModel != null)
        {
            _observedViewModel.CloseRequested -= OnCloseRequested;
        }

        _observedViewModel = viewModel;

        if (_observedViewModel != null)
        {
            _observedViewModel.CloseRequested += OnCloseRequested;
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
