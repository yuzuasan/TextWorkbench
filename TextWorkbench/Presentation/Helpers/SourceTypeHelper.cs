using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Presentation.Helpers;

/// <summary>
/// SourceTypeに関するヘルパーメソッドを提供します。
/// </summary>
public static class SourceTypeHelper
{
    /// <summary>
    /// SourceTypeと表示名からヘッダー表示テキストを生成します。
    /// </summary>
    /// <param name="sourceType">比較元の種類</param>
    /// <param name="displayName">表示名</param>
    /// <returns>ヘッダー表示テキスト</returns>
    public static string ToHeaderText(SourceType sourceType, string displayName)
    {
        return sourceType switch
        {
            SourceType.File => $"保存済みファイル：{displayName}",
            SourceType.OpenDocument => $"開いているドキュメント：{displayName}",
            _ => displayName
        };
    }
}
