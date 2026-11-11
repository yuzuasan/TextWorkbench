using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Models;
using TextWorkbench.Application.Services;

namespace TextWorkbench.Tests.Application.Services;

/// <summary>
/// <see cref="ReplaceService"/> の単体テストです。
/// </summary>
public class ReplaceServiceTests
{
    private readonly ReplaceService _service = new();

    // 共通ガード：ReplaceFirst

    [Fact]
    public void ReplaceFirst_contentがnullの場合_ArgumentNullExceptionが発生する()
    {
        var condition = new SearchCondition("a", false);

        Assert.Throws<ArgumentNullException>(() => _service.ReplaceFirst(null!, condition, "x", 0));
    }

    [Fact]
    public void ReplaceFirst_conditionがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => _service.ReplaceFirst("content", null!, "x", 0));
    }

    [Fact]
    public void ReplaceFirst_replacementがnullの場合_ArgumentNullExceptionが発生する()
    {
        var condition = new SearchCondition("a", false);

        Assert.Throws<ArgumentNullException>(() => _service.ReplaceFirst("content", condition, null!, 0));
    }

    [Fact]
    public void ReplaceFirst_検索文字列が空の場合_置換なしの結果が返る()
    {
        var condition = new SearchCondition(string.Empty, false);

        var result = _service.ReplaceFirst("content", condition, "x", 0);

        Assert.Equal("content", result.Content);
        Assert.Equal(0, result.ReplacementCount);
        Assert.Null(result.FirstMatchStartIndex);
        Assert.Null(result.FirstMatchLength);
        Assert.Null(result.ReplacementLength);
    }

    // 共通ガード：ReplaceAll

    [Fact]
    public void ReplaceAll_contentがnullの場合_ArgumentNullExceptionが発生する()
    {
        var condition = new SearchCondition("a", false);

        Assert.Throws<ArgumentNullException>(() => _service.ReplaceAll(null!, condition, "x"));
    }

    [Fact]
    public void ReplaceAll_conditionがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => _service.ReplaceAll("content", null!, "x"));
    }

    [Fact]
    public void ReplaceAll_replacementがnullの場合_ArgumentNullExceptionが発生する()
    {
        var condition = new SearchCondition("a", false);

        Assert.Throws<ArgumentNullException>(() => _service.ReplaceAll("content", condition, null!));
    }

    [Fact]
    public void ReplaceAll_検索文字列が空の場合_置換なしの結果が返る()
    {
        var condition = new SearchCondition(string.Empty, false);

        var result = _service.ReplaceAll("content", condition, "x");

        Assert.Equal("content", result.Content);
        Assert.Equal(0, result.ReplacementCount);
        Assert.Null(result.FirstMatchStartIndex);
    }

    [Fact]
    public void ReplaceAll_コンテンツが空の場合_置換なしの結果が返る()
    {
        var condition = new SearchCondition("a", false);

        var result = _service.ReplaceAll(string.Empty, condition, "x");

        Assert.Equal(string.Empty, result.Content);
        Assert.Equal(0, result.ReplacementCount);
    }

    // ReplaceFirst：境界・文字列検索

    [Fact]
    public void ReplaceFirst_startIndexが負の場合_置換なしの結果が返る()
    {
        var condition = new SearchCondition("a", false);

        var result = _service.ReplaceFirst("abc", condition, "x", -1);

        Assert.Equal("abc", result.Content);
        Assert.Equal(0, result.ReplacementCount);
    }

    [Fact]
    public void ReplaceFirst_startIndexがコンテンツ長を超える場合_置換なしの結果が返る()
    {
        var condition = new SearchCondition("a", false);

        var result = _service.ReplaceFirst("abc", condition, "x", 10);

        Assert.Equal("abc", result.Content);
        Assert.Equal(0, result.ReplacementCount);
    }

    [Fact]
    public void ReplaceFirst_startIndexがコンテンツ長と等しい場合_置換なしの結果が返る()
    {
        var condition = new SearchCondition("c", false);

        var result = _service.ReplaceFirst("abc", condition, "x", 3);

        Assert.Equal("abc", result.Content);
        Assert.Equal(0, result.ReplacementCount);
    }

    [Fact]
    public void ReplaceFirst_文字列検索で最初の一致を置換する()
    {
        var condition = new SearchCondition("foo", false);

        var result = _service.ReplaceFirst("foo bar foo", condition, "X", 0);

        Assert.Equal("X bar foo", result.Content);
        Assert.Equal(1, result.ReplacementCount);
        Assert.Equal(0, result.FirstMatchStartIndex);
        Assert.Equal(3, result.FirstMatchLength);
        Assert.Equal(1, result.ReplacementLength);
    }

    [Fact]
    public void ReplaceFirst_文字列検索はstartIndex以降のみ対象にする()
    {
        var condition = new SearchCondition("foo", false);

        var result = _service.ReplaceFirst("foo bar foo", condition, "X", 4);

        Assert.Equal("foo bar X", result.Content);
        Assert.Equal(1, result.ReplacementCount);
        Assert.Equal(8, result.FirstMatchStartIndex);
    }

    [Fact]
    public void ReplaceFirst_文字列検索は大小文字を区別しない()
    {
        var condition = new SearchCondition("abc", false);

        var result = _service.ReplaceFirst("zAbCz", condition, "Z", 0);

        Assert.Equal("zZz", result.Content);
        Assert.Equal(1, result.ReplacementCount);
    }

    [Fact]
    public void ReplaceFirst_文字列検索でマッチしない場合_置換なしの結果が返る()
    {
        var condition = new SearchCondition("xyz", false);

        var result = _service.ReplaceFirst("abc def", condition, "x", 0);

        Assert.Equal("abc def", result.Content);
        Assert.Equal(0, result.ReplacementCount);
        Assert.Null(result.FirstMatchStartIndex);
    }

    // ReplaceFirst：正規表現

    [Fact]
    public void ReplaceFirst_正規表現でキャプチャグループ置換ができる()
    {
        var condition = new SearchCondition(@"(a+)(b+)", true);

        var result = _service.ReplaceFirst("xaabby", condition, "[$2$1]", 0);

        Assert.Equal("x[bbaa]y", result.Content);
        Assert.Equal(1, result.ReplacementCount);
        Assert.Equal(1, result.FirstMatchStartIndex);
        Assert.Equal(4, result.FirstMatchLength);
        Assert.Equal(6, result.ReplacementLength);
    }

    [Fact]
    public void ReplaceFirst_正規表現でマッチしない場合_置換なしの結果が返る()
    {
        var condition = new SearchCondition(@"\d+", true);

        var result = _service.ReplaceFirst("abcdef", condition, "x", 0);

        Assert.Equal("abcdef", result.Content);
        Assert.Equal(0, result.ReplacementCount);
    }

    [Fact]
    public void ReplaceFirst_無効な正規表現の場合_InvalidRegexExceptionが発生する()
    {
        var condition = new SearchCondition("[", true);

        var ex = Assert.Throws<InvalidRegexException>(() =>
            _service.ReplaceFirst("content", condition, "x", 0));
        Assert.Equal("正規表現が無効です。", ex.Message);
    }

    [Fact]
    public void ReplaceFirst_ゼロ幅マッチの場合_先頭に置換文字列が挿入される()
    {
        var condition = new SearchCondition("x*", true);

        var result = _service.ReplaceFirst("ab", condition, "R", 0);

        Assert.Equal("Rab", result.Content);
        Assert.Equal(1, result.ReplacementCount);
        Assert.Equal(0, result.FirstMatchStartIndex);
        Assert.Equal(0, result.FirstMatchLength);
        Assert.Equal(1, result.ReplacementLength);
    }

    // ReplaceAll：文字列検索

    [Fact]
    public void ReplaceAll_文字列検索で複数箇所を一括置換する()
    {
        var condition = new SearchCondition("ab", false);

        var result = _service.ReplaceAll("ab x ab", condition, "Z");

        Assert.Equal("Z x Z", result.Content);
        Assert.Equal(2, result.ReplacementCount);
        Assert.Equal(0, result.FirstMatchStartIndex);
        Assert.Equal(2, result.FirstMatchLength);
        Assert.Equal(1, result.ReplacementLength);
    }

    [Fact]
    public void ReplaceAll_置換結果に検索文字列が含まれても再検索しない()
    {
        var condition = new SearchCondition("a", false);

        var result = _service.ReplaceAll("ab", condition, "aa");

        Assert.Equal("aab", result.Content);
        Assert.Equal(1, result.ReplacementCount);
    }

    [Fact]
    public void ReplaceAll_文字列検索は大小文字を区別しない()
    {
        var condition = new SearchCondition("abc", false);

        var result = _service.ReplaceAll("AbC x aBc", condition, "Z");

        Assert.Equal("Z x Z", result.Content);
        Assert.Equal(2, result.ReplacementCount);
    }

    [Fact]
    public void ReplaceAll_文字列検索でマッチしない場合_置換なしの結果が返る()
    {
        var condition = new SearchCondition("xyz", false);

        var result = _service.ReplaceAll("abc def", condition, "x");

        Assert.Equal("abc def", result.Content);
        Assert.Equal(0, result.ReplacementCount);
        Assert.Null(result.FirstMatchStartIndex);
    }

    // ReplaceAll：正規表現

    [Fact]
    public void ReplaceAll_正規表現でキャプチャグループ置換ができる()
    {
        var condition = new SearchCondition(@"(a+)", true);

        var result = _service.ReplaceAll("a1aa", condition, "<$1>");

        Assert.Equal("<a>1<aa>", result.Content);
        Assert.Equal(2, result.ReplacementCount);
        Assert.Equal(0, result.FirstMatchStartIndex);
        Assert.Equal(1, result.FirstMatchLength);
    }

    [Fact]
    public void ReplaceAll_正規表現でゼロ幅マッチの場合_各位置に挿入される()
    {
        var condition = new SearchCondition("x*", true);

        var result = _service.ReplaceAll("ab", condition, "R");

        Assert.Equal(3, result.ReplacementCount);
        Assert.Equal("RaRbR", result.Content);
    }

    [Fact]
    public void ReplaceAll_正規表現でマッチしない場合_置換なしの結果が返る()
    {
        var condition = new SearchCondition(@"\d+", true);

        var result = _service.ReplaceAll("abcdef", condition, "x");

        Assert.Equal("abcdef", result.Content);
        Assert.Equal(0, result.ReplacementCount);
    }

    [Fact]
    public void ReplaceAll_無効な正規表現の場合_InvalidRegexExceptionが発生する()
    {
        var condition = new SearchCondition("[", true);

        var ex = Assert.Throws<InvalidRegexException>(() =>
            _service.ReplaceAll("content", condition, "x"));
        Assert.Equal("正規表現が無効です。", ex.Message);
    }

    [Fact]
    public void ReplaceAll_正規表現がタイムアウトした場合_InvalidRegexExceptionが発生する()
    {
        var condition = new SearchCondition("(a+)+$", true);
        var content = new string('a', 30) + "!";

        var ex = Assert.Throws<InvalidRegexException>(() =>
            _service.ReplaceAll(content, condition, "x"));
        Assert.Equal("正規表現の処理がタイムアウトしました。", ex.Message);
    }
}
