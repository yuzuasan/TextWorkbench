using System.IO;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// ファイルを開くUseCaseです。
/// </summary>
public sealed class OpenDocumentUseCase
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IFileService _fileService;
    private readonly IEncodingService _encodingService;
    private readonly ILineEndingService _lineEndingService;

    /// <summary>
    /// <see cref="OpenDocumentUseCase"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="fileDialogService">ファイルダイアログサービス</param>
    /// <param name="fileService">ファイルサービス</param>
    /// <param name="encodingService">エンコーディングサービス</param>
    /// <param name="lineEndingService">改行コードサービス</param>
    public OpenDocumentUseCase(
        IFileDialogService fileDialogService,
        IFileService fileService,
        IEncodingService encodingService,
        ILineEndingService lineEndingService)
    {
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _encodingService = encodingService ?? throw new ArgumentNullException(nameof(encodingService));
        _lineEndingService = lineEndingService ?? throw new ArgumentNullException(nameof(lineEndingService));
    }

    /// <summary>
    /// ファイルを開きます。
    /// </summary>
    /// <returns>開いたDocument、キャンセル時は null</returns>
    public Document? Execute()
    {
        // ファイル選択
        var filePath = _fileDialogService.ShowOpenFileDialog();
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        // パス正規化
        var normalizedPath = Path.GetFullPath(filePath);

        // ファイル読み込み
        var readResult = _fileService.ReadAllBytes(normalizedPath);

        // デコード
        var decodedText = _encodingService.Decode(readResult.Content);

        // 改行コード検出
        var lineEnding = _lineEndingService.Detect(decodedText.Text);

        // 改行コードをLFに正規化
        var normalizedContent = _lineEndingService.NormalizeToLf(decodedText.Text);

        // ファイル名取得
        var fileName = Path.GetFileName(normalizedPath);

        // Document生成
        var document = new Document(
            normalizedPath,
            fileName,
            normalizedContent,
            decodedText.Encoding,
            lineEnding,
            readResult.IsReadOnly);

        return document;
    }
}
