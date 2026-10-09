using CommunityToolkit.Mvvm.ComponentModel;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// Tab共通のViewModel基底クラスです。
/// </summary>
public abstract partial class TabViewModel : ObservableObject
{
    /// <summary>
    /// Tabヘッダー表示用の文字列を取得または設定します。
    /// </summary>
    [ObservableProperty]
    private string _displayTitle = string.Empty;

    /// <summary>
    /// <see cref="TabViewModel"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="displayTitle">Tabヘッダー表示用の文字列</param>
    protected TabViewModel(string displayTitle)
    {
        DisplayTitle = displayTitle ?? throw new ArgumentNullException(nameof(displayTitle));
    }
}
