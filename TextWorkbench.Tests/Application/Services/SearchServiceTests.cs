using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Models;
using TextWorkbench.Application.Services;

namespace TextWorkbench.Tests.Application.Services;

/// <summary>
/// <see cref="SearchService"/> の単体テストです。
/// </summary>
public class SearchServiceTests
{
    private readonly SearchService _service = new();

    // 共通ガード

    [Fact]
    public void Search_contentがnullの場合_ArgumentNullExceptionが発生する()
    {
        var condition = new SearchCondition("a", false);

        Assert.Throws<ArgumentNullException>(() => _service.Search(null!, condition));
    }

    [Fact]
    public void Search_conditionがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Search("content", null!));
    }

    [Fact]
    public void Search_検索文字列が空の場合_空の結果が返る()
    {
        var condition = new SearchCondition(string.Empty, false);

        var result = _service.Search("content", condition);

        Assert.Empty(result);
    }

    [Fact]
    public void Search_コンテンツが空の場合_空の結果が返る()
    {
        var condition = new SearchCondition("a", false);

        var result = _service.Search(string.Empty, condition);

        Assert.Empty(result);
    }

    // 文字列検索

    [Fact]
    public void Search_文字列検索で1件マッチする場合_開始位置と長さが正しい()
    {
        var condition = new SearchCondition("hello", false);

        var result = _service.Search("say hello world", condition);

        var match = Assert.Single(result);
        Assert.Equal(4, match.StartIndex);
        Assert.Equal(5, match.Length);
    }

    [Fact]
    public void Search_文字列検索で複数マッチする場合_すべての位置が返る()
    {
        var condition = new SearchCondition("ab", false);

        var result = _service.Search("ab x ab", condition);

        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].StartIndex);
        Assert.Equal(5, result[1].StartIndex);
    }

    [Fact]
    public void Search_文字列検索でマッチしない場合_空の結果が返る()
    {
        var condition = new SearchCondition("xyz", false);

        var result = _service.Search("abc def", condition);

        Assert.Empty(result);
    }

    [Fact]
    public void Search_文字列検索は大小文字を区別しない()
    {
        var condition = new SearchCondition("ABC", false);

        var result = _service.Search("xxabcxx", condition);

        var match = Assert.Single(result);
        Assert.Equal(2, match.StartIndex);
        Assert.Equal(3, match.Length);
    }

    [Fact]
    public void Search_文字列検索は重複マッチしない()
    {
        var condition = new SearchCondition("aa", false);

        var result = _service.Search("aaa", condition);

        var match = Assert.Single(result);
        Assert.Equal(0, match.StartIndex);
        Assert.Equal(2, match.Length);
    }

    [Fact]
    public void Search_文字列検索で末尾にマッチする場合_正しい位置が返る()
    {
        var condition = new SearchCondition("end", false);

        var result = _service.Search("the end", condition);

        var match = Assert.Single(result);
        Assert.Equal(4, match.StartIndex);
        Assert.Equal(3, match.Length);
    }

    // 正規表現検索

    [Fact]
    public void Search_正規表現検索でマッチする場合_開始位置と長さが正しい()
    {
        var condition = new SearchCondition(@"\d+", true);

        var result = _service.Search("abc123def456", condition);

        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].StartIndex);
        Assert.Equal(3, result[0].Length);
        Assert.Equal(9, result[1].StartIndex);
        Assert.Equal(3, result[1].Length);
    }

    [Fact]
    public void Search_正規表現検索でマッチしない場合_空の結果が返る()
    {
        var condition = new SearchCondition(@"\d+", true);

        var result = _service.Search("abcdef", condition);

        Assert.Empty(result);
    }

    [Fact]
    public void Search_無効な正規表現の場合_InvalidRegexExceptionが発生する()
    {
        var condition = new SearchCondition("[", true);

        var ex = Assert.Throws<InvalidRegexException>(() => _service.Search("content", condition));
        Assert.Equal("正規表現が無効です。", ex.Message);
    }

    [Fact]
    public void Search_正規表現検索はMultilineが有効で行頭にマッチする()
    {
        var condition = new SearchCondition(@"^b", true);

        var result = _service.Search("a\nb\nc", condition);

        var match = Assert.Single(result);
        Assert.Equal(2, match.StartIndex);
    }

    [Fact]
    public void Search_正規表現検索は大文字小文字を区別しない()
    {
        var condition = new SearchCondition("abc", true);

        var result = _service.Search("xxABCxx", condition);

        var match = Assert.Single(result);
        Assert.Equal(2, match.StartIndex);
        Assert.Equal(3, match.Length);
    }

    [Fact]
    public void Search_キャプチャグループを含む正規表現が動作する()
    {
        var condition = new SearchCondition(@"(a+)(b+)", true);

        var result = _service.Search("xaabby", condition);

        var match = Assert.Single(result);
        Assert.Equal(1, match.StartIndex);
        Assert.Equal(4, match.Length);
    }

    [Fact]
    public void Search_ゼロ幅マッチの場合_空のマッチが各位置に返る()
    {
        var condition = new SearchCondition("x*", true);

        var result = _service.Search("ab", condition);

        Assert.Equal(3, result.Count);
        Assert.All(result, r => Assert.Equal(0, r.Length));
        Assert.Equal(0, result[0].StartIndex);
        Assert.Equal(1, result[1].StartIndex);
        Assert.Equal(2, result[2].StartIndex);
    }

    [Fact]
    public void Search_ゼロ幅マッチを含む場合_検索位置が正しく進む()
    {
        var condition = new SearchCondition("a*", true);

        var result = _service.Search("aab", condition);

        Assert.Equal(3, result.Count);
        Assert.Equal(0, result[0].StartIndex);
        Assert.Equal(2, result[0].Length);
        Assert.Equal(2, result[1].StartIndex);
        Assert.Equal(0, result[1].Length);
        Assert.Equal(3, result[2].StartIndex);
        Assert.Equal(0, result[2].Length);
    }

    [Fact]
    public void Search_正規表現がタイムアウトした場合_InvalidRegexExceptionが発生する()
    {
        var condition = new SearchCondition("(a+)+$", true);
        var content = new string('a', 30) + "!";

        var ex = Assert.Throws<InvalidRegexException>(() => _service.Search(content, condition));
        Assert.Equal("正規表現の処理がタイムアウトしました。", ex.Message);
    }
}
