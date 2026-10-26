using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// Documentのテキスト情報を取得するUseCaseです。
/// </summary>
public sealed class GetTextInfoUseCase
{
    private readonly ITextInfoService _textInfoService;

    /// <summary>
    /// <see cref="GetTextInfoUseCase"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="textInfoService">テキスト情報サービス</param>
    public GetTextInfoUseCase(ITextInfoService textInfoService)
    {
        _textInfoService = textInfoService ?? throw new ArgumentNullException(nameof(textInfoService));
    }

    /// <summary>
    /// Documentのテキスト情報を取得します。
    /// </summary>
    /// <param name="content">テキスト内容</param>
    /// <param name="encoding">文字コード</param>
    /// <param name="lineEnding">改行コード</param>
    /// <returns>テキスト情報</returns>
    public TextInfo Execute(string content, TextEncoding encoding, LineEnding lineEnding)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        return _textInfoService.GetTextInfo(content, encoding, lineEnding);
    }
}
