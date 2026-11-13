using System.Text;
using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Services;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Infrastructure.Encoding;
using TextWorkbench.Infrastructure.LineEnding;

namespace TextWorkbench.Tests.Application.Services;

/// <summary>
/// <see cref="TextInfoService"/> の単体テストです。
/// </summary>
public class TextInfoServiceTests
{
    static TextInfoServiceTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    // コンストラクタ・ガード

    [Fact]
    public void コンストラクタ_encodingServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new TextInfoService(
            null!, new LineEndingService()));
    }

    [Fact]
    public void コンストラクタ_lineEndingServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new TextInfoService(
            new EncodingService(), null!));
    }

    [Fact]
    public void GetTextInfo_contentがnullの場合_ArgumentNullExceptionが発生する()
    {
        var service = CreateService();

        Assert.Throws<ArgumentNullException>(() => service.GetTextInfo(
            null!, TextEncoding.Utf8, LineEnding.CrLf));
    }

    // LineCount

    [Fact]
    public void GetTextInfo_空文字の行数は1になる()
    {
        var info = CreateService().GetTextInfo(string.Empty, TextEncoding.Utf8, LineEnding.CrLf);

        Assert.Equal(1, info.LineCount);
    }

    [Fact]
    public void GetTextInfo_LF区切りの行数が計算される()
    {
        var service = CreateService();

        Assert.Equal(2, service.GetTextInfo("a\nb", TextEncoding.Utf8, LineEnding.CrLf).LineCount);
        Assert.Equal(3, service.GetTextInfo("a\nb\n", TextEncoding.Utf8, LineEnding.CrLf).LineCount);
    }

    [Fact]
    public void GetTextInfo_CRのみの改行は行数にカウントされない()
    {
        // contentはLF正規化済みが前提のため、CRのみ入力は契約外。現行動作を固定する。
        var service = CreateService();

        Assert.Equal(1, service.GetTextInfo("a\rb", TextEncoding.Utf8, LineEnding.CrLf).LineCount);
    }

    [Fact]
    public void GetTextInfo_CRLF混在の行数が計算される()
    {
        var service = CreateService();

        Assert.Equal(3, service.GetTextInfo("a\r\nb\nc", TextEncoding.Utf8, LineEnding.CrLf).LineCount);
    }

    // CharacterCount

    [Fact]
    public void GetTextInfo_ASCII文字列の文字数が計算される()
    {
        var info = CreateService().GetTextInfo("abc", TextEncoding.Utf8, LineEnding.CrLf);

        Assert.Equal(3, info.CharacterCount);
    }

    [Fact]
    public void GetTextInfo_日本語文字列の文字数が計算される()
    {
        var info = CreateService().GetTextInfo("あいう", TextEncoding.Utf8, LineEnding.CrLf);

        Assert.Equal(3, info.CharacterCount);
    }

    [Fact]
    public void GetTextInfo_サロゲートペアは1文字として数えられる()
    {
        var info = CreateService().GetTextInfo("😀", TextEncoding.Utf8, LineEnding.CrLf);

        Assert.Equal(1, info.CharacterCount);
    }

    [Fact]
    public void GetTextInfo_結合文字は1文字として数えられる()
    {
        // 「か」+結合濁点（U+3099）は1つの書記素クラスタ
        var info = CreateService().GetTextInfo("か\u3099", TextEncoding.Utf8, LineEnding.CrLf);

        Assert.Equal(1, info.CharacterCount);
    }

    [Fact]
    public void GetTextInfo_CRLFを含むcontentの文字数はCRLFが1要素として数えられる()
    {
        // contentはLF正規化済みが前提のため、CRLF入力は契約外。現行動作を固定する。
        // StringInfo.LengthInTextElements はCRLFを1つの書記素クラスタとして扱う。
        var info = CreateService().GetTextInfo("a\r\nb", TextEncoding.Utf8, LineEnding.CrLf);

        Assert.Equal(3, info.CharacterCount);
    }

    // ByteCount

    [Fact]
    public void GetTextInfo_Utf8のバイト数が計算される()
    {
        var service = CreateService();

        Assert.Equal(3, service.GetTextInfo("a\nb", TextEncoding.Utf8, LineEnding.Lf).ByteCount);
        Assert.Equal(3, service.GetTextInfo("あ", TextEncoding.Utf8, LineEnding.CrLf).ByteCount);
    }

    [Fact]
    public void GetTextInfo_改行コード変換がバイト数に反映される()
    {
        var service = CreateService();

        // "a\nb" はLfで3バイト、CrLf変換で "a\r\nb" となり4バイト
        Assert.Equal(3, service.GetTextInfo("a\nb", TextEncoding.Utf8, LineEnding.Lf).ByteCount);
        Assert.Equal(4, service.GetTextInfo("a\nb", TextEncoding.Utf8, LineEnding.CrLf).ByteCount);
        Assert.Equal(3, service.GetTextInfo("a\nb", TextEncoding.Utf8, LineEnding.Cr).ByteCount);
    }

    [Fact]
    public void GetTextInfo_ShiftJisのバイト数が計算される()
    {
        var service = CreateService();

        // 「あ」はShift-JISで2バイト、UTF-8で3バイト
        Assert.Equal(2, service.GetTextInfo("あ", TextEncoding.ShiftJis, LineEnding.CrLf).ByteCount);
        Assert.Equal(3, service.GetTextInfo("あ", TextEncoding.Utf8, LineEnding.CrLf).ByteCount);
    }

    [Fact]
    public void GetTextInfo_Utf8Bomはプレアンブル分のバイト数が加算される()
    {
        var service = CreateService();

        // 空文字でもBOM 3バイトが付く
        Assert.Equal(3, service.GetTextInfo(string.Empty, TextEncoding.Utf8Bom, LineEnding.CrLf).ByteCount);
        // 「あ」3バイト + BOM 3バイト = 6バイト
        Assert.Equal(6, service.GetTextInfo("あ", TextEncoding.Utf8Bom, LineEnding.CrLf).ByteCount);
    }

    [Fact]
    public void GetTextInfo_ShiftJisで変換不能な文字がある場合_EncodingOperationExceptionが発生する()
    {
        var service = CreateService();

        Assert.Throws<EncodingOperationException>(() => service.GetTextInfo(
            "😀", TextEncoding.ShiftJis, LineEnding.CrLf));
    }

    // 引き継ぎ・異常系

    [Fact]
    public void GetTextInfo_encodingとlineEndingがTextInfoに引き継がれる()
    {
        var info = CreateService().GetTextInfo("a", TextEncoding.ShiftJis, LineEnding.Lf);

        Assert.Equal(TextEncoding.ShiftJis, info.Encoding);
        Assert.Equal(LineEnding.Lf, info.LineEnding);
    }

    [Fact]
    public void GetTextInfo_未定義のTextEncodingの場合_EncodingOperationExceptionが発生する()
    {
        var service = CreateService();

        Assert.Throws<EncodingOperationException>(() => service.GetTextInfo(
            "a", (TextEncoding)999, LineEnding.CrLf));
    }

    [Fact]
    public void GetTextInfo_未定義のLineEndingの場合_ArgumentExceptionが発生する()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.GetTextInfo(
            "a", TextEncoding.Utf8, (LineEnding)999));
    }

    // テストデータ作成ヘルパー

    private static TextInfoService CreateService()
    {
        return new TextInfoService(new EncodingService(), new LineEndingService());
    }
}
