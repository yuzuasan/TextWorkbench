using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// Documentを閉じるUseCaseです。
/// </summary>
public sealed class CloseDocumentUseCase
{
    private readonly IUserConfirmationService _confirmationService;
    private readonly SaveDocumentUseCase _saveDocumentUseCase;
    private readonly SaveDocumentAsUseCase _saveDocumentAsUseCase;

    /// <summary>
    /// <see cref="CloseDocumentUseCase"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="confirmationService">ユーザー確認サービス</param>
    /// <param name="saveDocumentUseCase">Document保存UseCase</param>
    /// <param name="saveDocumentAsUseCase">名前を付けて保存UseCase</param>
    public CloseDocumentUseCase(
        IUserConfirmationService confirmationService,
        SaveDocumentUseCase saveDocumentUseCase,
        SaveDocumentAsUseCase saveDocumentAsUseCase)
    {
        _confirmationService = confirmationService
            ?? throw new ArgumentNullException(nameof(confirmationService));
        _saveDocumentUseCase = saveDocumentUseCase
            ?? throw new ArgumentNullException(nameof(saveDocumentUseCase));
        _saveDocumentAsUseCase = saveDocumentAsUseCase
            ?? throw new ArgumentNullException(nameof(saveDocumentAsUseCase));
    }

    /// <summary>
    /// Documentを閉じてよいか確認し、必要に応じて保存します。
    /// </summary>
    /// <param name="documentTab">閉じる対象のDocumentTab</param>
    /// <param name="workspace">Workspace（Save As時の重複確認用）</param>
    /// <returns>閉じてよい場合はtrue、閉じない場合はfalse</returns>
    public bool Execute(DocumentTabViewModel documentTab, Workspace workspace)
    {
        if (documentTab == null)
        {
            throw new ArgumentNullException(nameof(documentTab));
        }

        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        Document document = documentTab.Document;

        // 変更されていないDocumentは、そのまま閉じてよい。
        if (!document.IsModified)
        {
            return true;
        }

        // 変更内容を保存するか、保存せずに閉じるか、操作をキャンセルするか確認する。
        var result = _confirmationService.ConfirmSaveChanges(
            documentTab.DisplayTitle);

        switch (result)
        {
            case SaveConfirmationResult.Save:
                // 保存先が未設定の場合はSave Asを実行する。
                if (document.FilePath == null)
                {
                    // Save Asのキャンセル時は閉じない。
                    // 保存に失敗した場合は例外を呼び出し元へ伝播させる。
                    return _saveDocumentAsUseCase.Execute(document, workspace);
                }

                // 通常保存に失敗した場合は例外を呼び出し元へ伝播させる。
                _saveDocumentUseCase.Execute(document);
                return true;

            case SaveConfirmationResult.DontSave:
                // 変更内容を破棄して閉じることを許可する。
                return true;

            case SaveConfirmationResult.Cancel:
            default:
                // キャンセル時はDocumentを閉じない。
                return false;
        }
    }
}
