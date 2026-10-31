using TextWorkbench.Application.Models;

namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// Diffエンジンとの接続を抽象化するインターフェースです。
/// </summary>
public interface IDiffEngineAdapter
{
    /// <summary>
    /// 左右のテキストを比較します。
    /// </summary>
    /// <param name="leftText">左側のテキスト</param>
    /// <param name="rightText">右側のテキスト</param>
    /// <returns>Diffエンジンによる比較結果</returns>
    DiffEngineResult Compare(string leftText, string rightText);
}
