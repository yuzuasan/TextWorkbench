using CommunityToolkit.Mvvm.ComponentModel;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// MainWindowのViewModelです。
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    /// <summary>
    /// Workspaceを取得します。
    /// </summary>
    public Workspace Workspace { get; }

    /// <summary>
    /// <see cref="MainWindowViewModel"/> の新しいインスタンスを初期化します。
    /// </summary>
    public MainWindowViewModel()
    {
        Workspace = new Workspace();
    }
}
