using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// Documentを保存するUseCaseです。
/// </summary>
public class SaveDocumentUseCase
{
    private readonly IFileService _fileService;
    private readonly IEncodingService _encodingService;
    private readonly ILineEndingService _lineEndingService;

    /// <summary>
    /// <see cref="SaveDocumentUseCase"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="fileService">ファイルサービス</param>
    /// <param name="encodingService">エンコーディングサービス</param>
    /// <param name="lineEndingService">改行コードサービス</param>
    public SaveDocumentUseCase(
        IFileService fileService,
        IEncodingService encodingService,
        ILineEndingService lineEndingService)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _encodingService = encodingService ?? throw new ArgumentNullException(nameof(encodingService));
        _lineEndingService = lineEndingService ?? throw new ArgumentNullException(nameof(lineEndingService));
    }

    /// <summary>
    /// Documentを保存します。
    /// </summary>
    /// <param name="document">保存対象のDocument</param>
    public void Execute(Document document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.FilePath == null)
        {
            throw new InvalidOperationException("FilePathがnullのDocumentは保存できません。Save Asを使用してください。");
        }

        try
        {
            document.StartSaving();

            // LF → Document.LineEnding変換
            var contentForSave = _lineEndingService.ConvertFromLf(document.CurrentContent, document.LineEnding);

            // Encodingでバイト列に変換
            var bytes = _encodingService.Encode(contentForSave, document.Encoding);

            // 保存
            _fileService.WriteAllBytes(document.FilePath, bytes);

            // 成功時
            document.MarkAsSaved();
        }
        catch
        {
            // 失敗時：IsModifiedは維持
            throw;
        }
        finally
        {
            document.EndSaving();
        }
    }
}
