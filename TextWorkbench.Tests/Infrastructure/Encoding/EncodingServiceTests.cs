using TextWorkbench.Application.Exceptions;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Infrastructure.Encoding;

namespace TextWorkbench.Tests.Infrastructure.Encoding;

/// <summary>
/// <see cref="EncodingService"/> の単体テストです。
/// </summary>
public class EncodingServiceTests
{
    private readonly EncodingService _service = new();

    static EncodingServiceTests()
    {
        // App.xaml.csの起動時登録と同等の処理をテスト側でも行う
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    // Decode

    [Fact]
    public void Decode_bytesがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Decode(null!));
    }

    [Fact]
    public void Decode_空のbyte列の場合_Utf8の空文字を返す()
    {
        var result = _service.Decode(Array.Empty<byte>());

        Assert.Equal(string.Empty, result.Text);
        Assert.Equal(TextEncoding.Utf8, result.Encoding);
    }

    [Fact]
    public void Decode_UTF8BOM付きの場合_BOMを除いてUtf8Bomでデコードされる()
    {
        var contentBytes = System.Text.Encoding.UTF8.GetBytes("hello");
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(contentBytes).ToArray();

        var result = _service.Decode(bytes);

        Assert.Equal("hello", result.Text);
        Assert.Equal(TextEncoding.Utf8Bom, result.Encoding);
    }

    [Fact]
    public void Decode_BOMなしUTF8の場合_Utf8でデコードされる()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("こんにちは世界");

        var result = _service.Decode(bytes);

        Assert.Equal("こんにちは世界", result.Text);
        Assert.Equal(TextEncoding.Utf8, result.Encoding);
    }

    [Fact]
    public void Decode_ShiftJisのbyte列の場合_ShiftJisでデコードされる()
    {
        var shiftJis = System.Text.Encoding.GetEncoding("Shift-JIS");
        var bytes = shiftJis.GetBytes("日本語テスト");

        var result = _service.Decode(bytes);

        Assert.Equal("日本語テスト", result.Text);
        Assert.Equal(TextEncoding.ShiftJis, result.Encoding);
    }

    [Fact]
    public void Decode_UTF8とShiftJis両方で不正なbyte列の場合_EncodingOperationExceptionが発生する()
    {
        // 0x82はShift-JISのリードバイトだが、後続の0x20はトレールバイトとして無効
        var bytes = new byte[] { 0x82, 0x20 };

        var ex = Assert.Throws<EncodingOperationException>(() => _service.Decode(bytes));
        Assert.Equal("対応していない文字コードまたは形式です。", ex.Message);
    }

    [Fact]
    public void Decode_UTF8BOMの後に不正なbyteが続く場合_EncodingOperationExceptionが発生する()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF, 0xFF, 0xFF };

        var ex = Assert.Throws<EncodingOperationException>(() => _service.Decode(bytes));
        Assert.Equal("対応していない文字コードまたは形式です。", ex.Message);
    }

    // Encode

    [Fact]
    public void Encode_textがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Encode(null!, TextEncoding.Utf8));
    }

    [Fact]
    public void Encode_Utf8の場合_BOMなしのUTF8byte列が返る()
    {
        var result = _service.Encode("こんにちは😀", TextEncoding.Utf8);

        Assert.Equal(System.Text.Encoding.UTF8.GetBytes("こんにちは😀"), result);
        Assert.False(result.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
    }

    [Fact]
    public void Encode_Utf8Bomの場合_プレアンブル付きのbyte列が返る()
    {
        var result = _service.Encode("hello", TextEncoding.Utf8Bom);

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, result.Take(3).ToArray());
        Assert.Equal("hello", System.Text.Encoding.UTF8.GetString(result, 3, result.Length - 3));
    }

    [Fact]
    public void Encode_ShiftJisの場合_ShiftJisのbyte列が返る()
    {
        var result = _service.Encode("日本語テスト", TextEncoding.ShiftJis);

        var shiftJis = System.Text.Encoding.GetEncoding("Shift-JIS");
        Assert.Equal(shiftJis.GetBytes("日本語テスト"), result);
    }

    [Fact]
    public void Encode_ShiftJisで変換不能な文字を含む場合_EncodingOperationExceptionが発生する()
    {
        var ex = Assert.Throws<EncodingOperationException>(() =>
            _service.Encode("絵文字😀", TextEncoding.ShiftJis));
        Assert.Equal("現在の文字コードでは保存できない文字が含まれています。", ex.Message);
    }

    [Fact]
    public void Encode_未定義のTextEncodingの場合_EncodingOperationExceptionが発生する()
    {
        var ex = Assert.Throws<EncodingOperationException>(() =>
            _service.Encode("text", (TextEncoding)999));
        Assert.Equal("対応していない文字コードです。", ex.Message);
    }

    // ラウンドトリップ

    [Theory]
    [InlineData(TextEncoding.Utf8)]
    [InlineData(TextEncoding.Utf8Bom)]
    [InlineData(TextEncoding.ShiftJis)]
    public void EncodeしてDecodeすると_元のテキストと文字コードが復元される(TextEncoding encoding)
    {
        const string text = "こんにちは世界";

        var bytes = _service.Encode(text, encoding);
        var result = _service.Decode(bytes);

        Assert.Equal(text, result.Text);
        Assert.Equal(encoding, result.Encoding);
    }
}
