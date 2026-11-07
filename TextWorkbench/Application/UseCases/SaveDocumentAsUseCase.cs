using System.IO;
using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// Documentを別名保存するUseCaseです。
/// </summary>
public class SaveDocumentAsUseCase
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IDocumentPathRegistry _documentPathRegistry;
    private readonly IFileService _fileService;
    private readonly IEncodingService _encodingService;
    private readonly ILineEndingService _lineEndingService;

    /// <summary>
    /// <see cref="SaveDocumentAsUseCase"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="fileDialogService">ファイルダイアログサービス</param>
    /// <param name="documentPathRegistry">Documentパスレジストリ</param>
    /// <param name="fileService">ファイルサービス</param>
    /// <param name="encodingService">エンコーディングサービス</param>
    /// <param name="lineEndingService">改行コードサービス</param>
    public SaveDocumentAsUseCase(
        IFileDialogService fileDialogService,
        IDocumentPathRegistry documentPathRegistry,
        IFileService fileService,
        IEncodingService encodingService,
        ILineEndingService lineEndingService)
    {
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        _documentPathRegistry = documentPathRegistry ?? throw new ArgumentNullException(nameof(documentPathRegistry));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _encodingService = encodingService ?? throw new ArgumentNullException(nameof(encodingService));
        _lineEndingService = lineEndingService ?? throw new ArgumentNullException(nameof(lineEndingService));
    }

    /// <summary>
    /// Documentを別名保存します。
    /// </summary>
    /// <param name="document">保存対象のDocument</param>
    /// <returns>保存成功時はtrue、キャンセル時はfalse</returns>
    public bool Execute(Document document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        // SaveFileDialogを表示
        var newFilePath = _fileDialogService.ShowSaveFileDialog(document.FilePath);
        if (newFilePath == null)
        {
            return false; // キャンセル
        }

        // パスを正規化
        newFilePath = Path.GetFullPath(newFilePath);

        // 別Documentとのパス重複確認
        if (_documentPathRegistry.IsPathInUse(newFilePath, document.Id))
        {
            throw new ValidationOperationException("指定されたファイルは別のDocumentとして既に開かれています。");
        }

        try
        {
            document.StartSaving();

            // LF → Document.LineEnding変換
            var contentForSave = _lineEndingService.ConvertFromLf(document.CurrentContent, document.LineEnding);

            // Encodingでバイト列に変換
            var bytes = _encodingService.Encode(contentForSave, document.Encoding);

            // 保存
            _fileService.WriteAllBytes(newFilePath, bytes);

            // 成功時：FilePath更新、IsReadOnly = false, IsModified = false
            var newFileName = Path.GetFileName(newFilePath);
            document.UpdateFilePath(newFilePath, newFileName, false);
            document.MarkAsSaved();
        }
        finally
        {
            document.EndSaving();
        }

        return true;
    }
}
