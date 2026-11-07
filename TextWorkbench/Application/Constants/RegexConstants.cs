namespace TextWorkbench.Application.Constants;

/// <summary>
/// 正規表現に関する共通設定を提供します。
/// </summary>
public static class RegexConstants
{
    /// <summary>
    /// 正規表現マッチのタイムアウトです。
    /// 壊滅的バックトラッキングによるUIフリーズを防ぎます。
    /// </summary>
    public static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(3);
}
