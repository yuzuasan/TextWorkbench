using System.IO;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// Documentを別名保存するUseCaseです。
/// </summary>
public class SaveDocumentAsUseCase
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IFileService _fileService;
    private readonly IEncodingService _encodingService;
    private readonly ILineEndingService _lineEndingService;

    /// <summary>
    /// <see cref="SaveDocumentAsUseCase"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="fileDialogService">ファイルダイアログサービス</param>
    /// <param name="fileService">ファイルサービス</param>
    /// <param name="encodingService">エンコーディングサービス</param>
    /// <param name="lineEndingService">改行コードサービス</param>
    public SaveDocumentAsUseCase(
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
    /// Documentを別名保存します。
    /// </summary>
    /// <param name="document">保存対象のDocument</param>
    /// <param name="workspace">Workspace（重複確認用）</param>
    /// <returns>保存成功時はtrue、キャンセル時はfalse</returns>
    public bool Execute(Document document, Workspace workspace)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        // SaveFileDialogを表示
        var newFilePath = _fileDialogService.ShowSaveFileDialog(document.FilePath);
        if (newFilePath == null)
        {
            return false; // キャンセル
        }

        // パスを正規化
        newFilePath = Path.GetFullPath(newFilePath);

        // Workspace内の別Documentと重複確認
        var existingTab = workspace.FindDocumentByFilePath(newFilePath);
        if (existingTab != null && existingTab.Document != document)
        {
            throw new InvalidOperationException("指定されたファイルは別のDocumentとして既に開かれています。");
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
