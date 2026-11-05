using CommunityToolkit.Mvvm.ComponentModel;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.Helpers;

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
    public int _currentDiffIndex = -1;

    /// <summary>
    /// 差分数を取得します。
    /// </summary>
    public int DiffCount => DiffSession.Result.DiffCount;

    /// <summary>
    /// 左側のヘッダー表示テキストを取得します。
    /// </summary>
    public string LeftHeaderText => GetHeaderText(DiffSession.LeftSource);

    /// <summary>
    /// 右側のヘッダー表示テキストを取得します。
    /// </summary>
    public string RightHeaderText => GetHeaderText(DiffSession.RightSource);

    /// <summary>
    /// 左側のツールチップテキストを取得します。
    /// </summary>
    public string LeftToolTipText => DiffSession.LeftSource.FilePath ?? DiffSession.LeftSource.DisplayName;

    /// <summary>
    /// 右側のツールチップテキストを取得します。
    /// </summary>
    public string RightToolTipText => DiffSession.RightSource.FilePath ?? DiffSession.RightSource.DisplayName;

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

    /// <summary>
    /// ヘッダー表示テキストを取得します。
    /// </summary>
    /// <param name="source">比較元</param>
    /// <returns>ヘッダー表示テキスト</returns>
    private static string GetHeaderText(DiffSession.DiffSource source)
    {
        return SourceTypeHelper.ToHeaderText(source.SourceType, source.DisplayName);
    }

    /// <summary>
    /// 次の差分に移動します。
    /// </summary>
    public void NextDiff()
    {
        if (DiffCount == 0)
            return;

        if (CurrentDiffIndex == -1)
        {
            CurrentDiffIndex = 0;
        }
        else if (CurrentDiffIndex < DiffCount - 1)
        {
            CurrentDiffIndex++;
        }
        // 最後に到達した後はno-op
    }

    /// <summary>
    /// 前の差分に移動します。
    /// </summary>
    public void PreviousDiff()
    {
        if (DiffCount == 0)
            return;

        if (CurrentDiffIndex == -1)
        {
            CurrentDiffIndex = DiffCount - 1;
        }
        else if (CurrentDiffIndex > 0)
        {
            CurrentDiffIndex--;
        }
        // 最初に到達した後はno-op
    }

    /// <summary>
    /// 現在の差分に対応するDiffLineを取得します。
    /// </summary>
    /// <returns>現在の差分DiffLine。未選択の場合はnull。</returns>
    public DiffLine? GetCurrentDiffLine()
    {
        if (CurrentDiffIndex == -1)
            return null;

        // 差分が存在するDiffLineのみを対象とする
        var diffLines = DiffSession.Result.Lines
            .Where(line => line.Type != DiffLineType.Unchanged)
            .ToList();

        if (CurrentDiffIndex < 0 || CurrentDiffIndex >= diffLines.Count)
            return null;

        return diffLines[CurrentDiffIndex];
    }
}
