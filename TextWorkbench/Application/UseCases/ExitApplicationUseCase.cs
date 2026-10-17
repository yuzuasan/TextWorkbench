using TextWorkbench.Application.Interfaces;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// アプリケーション終了前の未保存Document確認と保存を担当するUseCaseです。
/// 実際のWindow終了処理はPresentation層が担当します。
/// </summary>
public sealed class ExitApplicationUseCase
{
    private readonly IExitConfirmationService _exitConfirmationService;
    private readonly IUserNotificationService _notificationService;
    private readonly SaveDocumentUseCase _saveDocumentUseCase;
    private readonly SaveDocumentAsUseCase _saveDocumentAsUseCase;

    /// <summary>
    /// <see cref="ExitApplicationUseCase"/> の新しいインスタンスを初期化します。
    /// </summary>
    public ExitApplicationUseCase(
        IExitConfirmationService exitConfirmationService,
        IUserNotificationService notificationService,
        SaveDocumentUseCase saveDocumentUseCase,
        SaveDocumentAsUseCase saveDocumentAsUseCase)
    {
        _exitConfirmationService = exitConfirmationService
            ?? throw new ArgumentNullException(nameof(exitConfirmationService));
        _notificationService = notificationService
            ?? throw new ArgumentNullException(nameof(notificationService));
        _saveDocumentUseCase = saveDocumentUseCase
            ?? throw new ArgumentNullException(nameof(saveDocumentUseCase));
        _saveDocumentAsUseCase = saveDocumentAsUseCase
            ?? throw new ArgumentNullException(nameof(saveDocumentAsUseCase));
    }

    /// <summary>
    /// 終了前に未保存Documentを確認し、必要に応じてタブ順に保存します。
    /// </summary>
    /// <param name="workspace">終了対象のWorkspace</param>
    /// <returns>終了を許可する場合はtrue、終了を中止する場合はfalse</returns>
    public bool Execute(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        // DocumentTabのみを対象とするため、DiffTabは自動的に保存対象外となる。
        // ToArrayで順序と対象を固定し、保存中の状態変化に影響されないようにする。
        var modifiedTabs = workspace.Tabs
            .OfType<DocumentTabViewModel>()
            .Where(tab => tab.Document.IsModified)
            .ToArray();

        if (modifiedTabs.Length == 0)
        {
            return true;
        }

        var documentNames = modifiedTabs
            .Select(tab => tab.DisplayTitle)
            .ToArray();

        var decision = _exitConfirmationService.ConfirmExit(documentNames);

        switch (decision)
        {
            case ExitConfirmationResult.ExitWithoutSaving:
                // 保存処理を実行せず、終了を許可する。
                return true;

            case ExitConfirmationResult.Cancel:
                // ユーザーキャンセルはエラー通知せず、終了のみ中止する。
                return false;

            case ExitConfirmationResult.SaveAndExit:
                return SaveModifiedDocuments(modifiedTabs, workspace);

            default:
                // 未知の値を終了許可として扱わない。
                return false;
        }
    }

    private bool SaveModifiedDocuments(
        IReadOnlyList<DocumentTabViewModel> modifiedTabs,
        Workspace workspace)
    {
        foreach (var tab in modifiedTabs)
        {
            var document = tab.Document;

            // 先行する保存処理等で変更が解消されていれば、再保存しない。
            if (!document.IsModified)
            {
                continue;
            }

            try
            {
                if (document.FilePath is null)
                {
                    // Save Asのキャンセルはfalseとして返り、通知せず終了を中止する。
                    if (!_saveDocumentAsUseCase.Execute(document, workspace))
                    {
                        return false;
                    }

                    tab.OnDocumentSavedAs();
                }
                else
                {
                    _saveDocumentUseCase.Execute(document);
                    tab.OnDocumentSaved();
                }
            }
            catch (Exception ex)
            {
                // 失敗した時点で停止する。成功済みDocumentの保存状態は維持し、
                // 失敗したDocumentおよび後続Documentは変更しない。
                _notificationService.ShowError(
                    $"「{tab.DisplayTitle}」の保存に失敗したため、アプリケーションを終了しません。{Environment.NewLine}{ex.Message}");
                return false;
            }
        }

        return true;
    }
}
