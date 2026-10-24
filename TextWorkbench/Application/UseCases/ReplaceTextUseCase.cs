using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// テキスト置換を実行するUseCaseです。
/// </summary>
public sealed class ReplaceTextUseCase
{
    private readonly IReplaceService _replaceService;

    /// <summary>
    /// <see cref="ReplaceTextUseCase"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="replaceService">置換サービス</param>
    public ReplaceTextUseCase(IReplaceService replaceService)
    {
        _replaceService = replaceService
            ?? throw new ArgumentNullException(nameof(replaceService));
    }

    /// <summary>
    /// 指定された開始位置以降で最初に一致した箇所を置換します。
    /// </summary>
    /// <param name="content">置換対象のテキスト</param>
    /// <param name="searchText">検索する文字列</param>
    /// <param name="replacement">置換後の文字列</param>
    /// <param name="isRegex">正規表現を使用するかどうか</param>
    /// <param name="startIndex">検索開始位置</param>
    /// <returns>置換後のテキストと置換情報</returns>
    public ReplaceResult ExecuteFirst(
        string content,
        string searchText,
        string replacement,
        bool isRegex,
        int startIndex)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(searchText);
        ArgumentNullException.ThrowIfNull(replacement);

        var condition = new SearchCondition(searchText, isRegex);

        return _replaceService.ReplaceFirst(
            content,
            condition,
            replacement,
            startIndex);
    }

    /// <summary>
    /// テキスト全体を対象に一致した箇所をすべて置換します。
    /// </summary>
    /// <param name="content">置換対象のテキスト</param>
    /// <param name="searchText">検索する文字列</param>
    /// <param name="replacement">置換後の文字列</param>
    /// <param name="isRegex">正規表現を使用するかどうか</param>
    /// <returns>置換後のテキストと置換情報</returns>
    public ReplaceResult ExecuteAll(
        string content,
        string searchText,
        string replacement,
        bool isRegex)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(searchText);
        ArgumentNullException.ThrowIfNull(replacement);

        var condition = new SearchCondition(searchText, isRegex);

        return _replaceService.ReplaceAll(
            content,
            condition,
            replacement);
    }
}
