using LE = TextWorkbench.Domain.Enums.LineEnding;
using TextWorkbench.Infrastructure.LineEnding;

namespace TextWorkbench.Tests.Infrastructure.LineEnding;

/// <summary>
/// <see cref="LineEndingService"/> の単体テストです。
/// </summary>
public class LineEndingServiceTests
{
    private readonly LineEndingService _service = new();

    // Detect

    [Fact]
    public void Detect_textがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Detect(null!));
    }

    [Fact]
    public void Detect_CRLFのみの場合_CrLfが返る()
    {
        var result = _service.Detect("line1\r\nline2\r\n");

        Assert.Equal(LE.CrLf, result);
    }

    [Fact]
    public void Detect_LFのみの場合_Lfが返る()
    {
        var result = _service.Detect("line1\nline2\n");

        Assert.Equal(LE.Lf, result);
    }

    [Fact]
    public void Detect_CRのみの場合_Crが返る()
    {
        var result = _service.Detect("line1\rline2\r");

        Assert.Equal(LE.Cr, result);
    }

    [Fact]
    public void Detect_混在でCRLFが最初の場合_CrLfが返る()
    {
        var result = _service.Detect("a\r\nb\nc\rd");

        Assert.Equal(LE.CrLf, result);
    }

    [Fact]
    public void Detect_混在でLFが最初の場合_Lfが返る()
    {
        var result = _service.Detect("\na\r\nb\rc");

        Assert.Equal(LE.Lf, result);
    }

    [Fact]
    public void Detect_混在でCRが最初の場合_Crが返る()
    {
        var result = _service.Detect("\ra\nb\r\nc");

        Assert.Equal(LE.Cr, result);
    }

    [Fact]
    public void Detect_改行が存在しない場合_CrLfが返る()
    {
        var result = _service.Detect("single line");

        Assert.Equal(LE.CrLf, result);
    }

    [Fact]
    public void Detect_空文字の場合_CrLfが返る()
    {
        var result = _service.Detect(string.Empty);

        Assert.Equal(LE.CrLf, result);
    }

    // NormalizeToLf

    [Fact]
    public void NormalizeToLf_textがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => _service.NormalizeToLf(null!));
    }

    [Fact]
    public void NormalizeToLf_CRLFを含む場合_LFに変換される()
    {
        var result = _service.NormalizeToLf("a\r\nb\r\nc");

        Assert.Equal("a\nb\nc", result);
    }

    [Fact]
    public void NormalizeToLf_CRを含む場合_LFに変換される()
    {
        var result = _service.NormalizeToLf("a\rb\rc");

        Assert.Equal("a\nb\nc", result);
    }

    [Fact]
    public void NormalizeToLf_混在した改行の場合_すべてLFに変換される()
    {
        var result = _service.NormalizeToLf("a\r\nb\rc\nd");

        Assert.Equal("a\nb\nc\nd", result);
    }

    [Fact]
    public void NormalizeToLf_改行がない場合_そのまま返る()
    {
        var result = _service.NormalizeToLf("single line");

        Assert.Equal("single line", result);
    }

    // ConvertFromLf

    [Fact]
    public void ConvertFromLf_textがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => _service.ConvertFromLf(null!, LE.CrLf));
    }

    [Fact]
    public void ConvertFromLf_CrLfの場合_LFがCRLFに変換される()
    {
        var result = _service.ConvertFromLf("a\nb\nc", LE.CrLf);

        Assert.Equal("a\r\nb\r\nc", result);
    }

    [Fact]
    public void ConvertFromLf_Lfの場合_そのまま返る()
    {
        var result = _service.ConvertFromLf("a\nb\nc", LE.Lf);

        Assert.Equal("a\nb\nc", result);
    }

    [Fact]
    public void ConvertFromLf_Crの場合_LFがCRに変換される()
    {
        var result = _service.ConvertFromLf("a\nb\nc", LE.Cr);

        Assert.Equal("a\rb\rc", result);
    }

    [Fact]
    public void ConvertFromLf_未定義のLineEndingの場合_ArgumentExceptionが発生する()
    {
        Assert.Throws<ArgumentException>(() =>
            _service.ConvertFromLf("a\nb", (LE)999));
    }
}
