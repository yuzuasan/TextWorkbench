using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// TextWorkbenchのDiff機能を提供するインターフェースです。
/// </summary>
public interface IDiffService
{
    /// <summary>
    /// 左右のテキストを比較し、TextWorkbench独自のDiff結果を返します。
    /// </summary>
    /// <param name="leftFileName">左側のファイル名</param>
    /// <param name="rightFileName">右側のファイル名</param>
    /// <param name="leftText">左側のテキスト</param>
    /// <param name="rightText">右側のテキスト</param>
    /// <returns>Diff結果</returns>
    DiffResult Compare(
        string leftFileName,
        string rightFileName,
        string leftText,
        string rightText);
}
