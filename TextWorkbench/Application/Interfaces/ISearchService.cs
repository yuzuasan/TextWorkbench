using TextWorkbench.Application.Models;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// テキスト検索機能を提供するサービスのインターフェースです。
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// 指定された条件でテキストを検索します。
    /// </summary>
    /// <param name="content">検索対象のテキスト</param>
    /// <param name="condition">検索条件</param>
    /// <returns>一致した位置の一覧</returns>
    IReadOnlyList<SearchResult> Search(string content, SearchCondition condition);
}
