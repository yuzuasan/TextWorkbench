using CommunityToolkit.Mvvm.ComponentModel;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// Diff比較結果をUIへ公開するViewModelです。
/// </summary>
public sealed partial class DiffTabViewModel : TabViewModel
{
    /// <summary>
    /// Diffセッションを取得します。
    /// </summary>
    public DiffSession DiffSession { get; }

    /// <summary>
    /// 現在の差分インデックスを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private int _currentDiffIndex = -1;

    /// <summary>
    /// 差分数を取得します。
    /// </summary>
    public int DiffCount => DiffSession.Result.DiffCount;

    /// <summary>
    /// <see cref="DiffTabViewModel"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="diffSession">Diffセッション</param>
    public DiffTabViewModel(DiffSession diffSession)
        : base(CalculateDisplayTitle(diffSession))
    {
        DiffSession = diffSession ?? throw new ArgumentNullException(nameof(diffSession));
    }

    /// <summary>
    /// DisplayTitleを算出します。
    /// </summary>
    private static string CalculateDisplayTitle(DiffSession diffSession)
    {
        var leftName = diffSession.LeftSource.DisplayName;
        var rightName = diffSession.RightSource.DisplayName;
        return $"{leftName} ↔ {rightName}";
    }
}
