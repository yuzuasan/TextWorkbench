using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// テキスト検索を実行するUseCaseです。
/// </summary>
public sealed class SearchTextUseCase
{
    private readonly ISearchService _searchService;

    /// <summary>
    /// <see cref="SearchTextUseCase"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="searchService">検索サービス</param>
    public SearchTextUseCase(ISearchService searchService)
    {
        _searchService = searchService
            ?? throw new ArgumentNullException(nameof(searchService));
    }

    /// <summary>
    /// 指定された内容と条件で検索します。
    /// </summary>
    /// <param name="content">検索対象のテキスト</param>
    /// <param name="searchText">検索する文字列</param>
    /// <param name="isRegex">正規表現を使用するかどうか</param>
    /// <returns>検索結果一覧</returns>
    public IReadOnlyList<SearchResult> Execute(
        string content,
        string searchText,
        bool isRegex)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(searchText);

        var condition = new SearchCondition(searchText, isRegex);

        return _searchService.Search(content, condition);
    }
}
