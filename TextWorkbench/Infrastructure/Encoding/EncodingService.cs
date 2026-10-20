using System.Text;
using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Infrastructure.Encoding;

/// <summary>
/// エンコーディング判定・変換サービスの実装です。
/// </summary>
public sealed class EncodingService : IEncodingService
{
    private const byte Utf8BomByte1 = 0xEF;
    private const byte Utf8BomByte2 = 0xBB;
    private const byte Utf8BomByte3 = 0xBF;

    /// <summary>
    /// 入力byte列を判定・デコードし、テキストと判定したEncodingを返します。
    /// </summary>
    /// <param name="bytes">入力byte列</param>
    /// <returns>デコード結果</returns>
    public DecodedText Decode(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));

        // 空ファイルはUTF-8の空文書として扱う
        if (bytes.Length == 0)
        {
            return new DecodedText(string.Empty, TextEncoding.Utf8);
        }

        // BOM確認
        if (TryDetectBom(bytes, out var bomEncoding))
        {
            if (bomEncoding == TextEncoding.Utf8Bom)
            {
                var encoding = new UTF8Encoding(true, true);
                var text = encoding.GetString(bytes, 3, bytes.Length - 3);
                return new DecodedText(text, TextEncoding.Utf8Bom);
            }
            else
            {
                // UTF-16/UTF-32等は非対応
                throw new EncodingOperationException("対応していない文字コードのため、ファイルを開けません。");
            }
        }

        // BOMなしの場合、strict UTF-8を試行
        try
        {
            var utf8Encoding = new UTF8Encoding(false, true);
            var text = utf8Encoding.GetString(bytes);
            return new DecodedText(text, TextEncoding.Utf8);
        }
        catch
        {
            // UTF-8失敗時はShift-JISを試行
        }

        // strict Shift-JISを試行
        try
        {
            var shiftJisEncoding = System.Text.Encoding.GetEncoding(
                "Shift-JIS",
                EncoderFallback.ExceptionFallback,
                DecoderFallback.ExceptionFallback);
            var text = shiftJisEncoding.GetString(bytes);
            return new DecodedText(text, TextEncoding.ShiftJis);
        }
        catch
        {
            throw new EncodingOperationException("対応していない文字コードまたは形式です。");
        }
    }

    /// <summary>
    /// 指定Encodingへ変換します。
    /// </summary>
    /// <param name="text">テキスト</param>
    /// <param name="encoding">文字コード</param>
    /// <returns>変換後のbyte列</returns>
    public byte[] Encode(string text, TextEncoding encoding)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        try
        {
            var systemEncoding = GetSystemEncoding(encoding);
            var bytes = systemEncoding.GetBytes(text);

            if (encoding == TextEncoding.Utf8Bom)
            {
                var preamble = systemEncoding.GetPreamble();
                return preamble.Concat(bytes).ToArray();
            }

            return bytes;
        }
        catch (EncoderFallbackException)
        {
            throw new EncodingOperationException("現在の文字コードでは保存できない文字が含まれています。");
        }
    }

    /// <summary>
    /// BOMを検出します。
    /// </summary>
    private static bool TryDetectBom(byte[] bytes, out TextEncoding encoding)
    {
        if (bytes.Length >= 3 &&
            bytes[0] == Utf8BomByte1 &&
            bytes[1] == Utf8BomByte2 &&
            bytes[2] == Utf8BomByte3)
        {
            encoding = TextEncoding.Utf8Bom;
            return true;
        }

        // UTF-16/UTF-32等のBOM検出はMVPでは実装しない
        encoding = TextEncoding.Utf8;
        return false;
    }

    /// <summary>
    /// TextEncodingからSystem.Encodingを取得します。
    /// </summary>
    private static System.Text.Encoding GetSystemEncoding(TextEncoding encoding)
    {
        return encoding switch
        {
            TextEncoding.Utf8 => new UTF8Encoding(false, true),
            TextEncoding.Utf8Bom => new UTF8Encoding(true, true),
            TextEncoding.ShiftJis => System.Text.Encoding.GetEncoding(
                "Shift-JIS",
                EncoderFallback.ExceptionFallback,
                DecoderFallback.ExceptionFallback),
            _ => throw new EncodingOperationException("対応していない文字コードです。")
        };
    }
}
