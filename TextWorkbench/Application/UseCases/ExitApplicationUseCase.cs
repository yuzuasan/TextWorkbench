using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// アプリケーション終了前の未保存Document確認と保存を担当するUseCaseです。
/// 実際のWindow終了処理とViewModel通知はPresentation層が担当します。
/// </summary>
public sealed class ExitApplicationUseCase
{
    private readonly IOpenDocumentProvider _openDocumentProvider;
    private readonly IDocumentActivationService _documentActivationService;
    private readonly IExitConfirmationService _exitConfirmationService;
    private readonly IUserNotificationService _notificationService;
    private readonly SaveDocumentUseCase _saveDocumentUseCase;
    private readonly SaveDocumentAsUseCase _saveDocumentAsUseCase;

    /// <summary>
    /// <see cref="ExitApplicationUseCase"/> の新しいインスタンスを初期化します。
    /// </summary>
    public ExitApplicationUseCase(
        IOpenDocumentProvider openDocumentProvider,
        IDocumentActivationService documentActivationService,
        IExitConfirmationService exitConfirmationService,
        IUserNotificationService notificationService,
        SaveDocumentUseCase saveDocumentUseCase,
        SaveDocumentAsUseCase saveDocumentAsUseCase)
    {
        _openDocumentProvider = openDocumentProvider
            ?? throw new ArgumentNullException(nameof(openDocumentProvider));
        _documentActivationService = documentActivationService
            ?? throw new ArgumentNullException(nameof(documentActivationService));
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
    /// <returns>終了可否と保存済みDocumentを含む終了処理結果</returns>
    public ExitApplicationResult Execute()
    {
        // Providerが返すタブ順を維持し、ToArrayで対象を固定する。
        // DiffTabはProviderが返さないため、保存対象外となる。
        var modifiedDocuments = _openDocumentProvider
            .GetOpenDocuments()
            .Where(info => info.Document.IsModified)
            .ToArray();

        if (modifiedDocuments.Length == 0)
        {
            return new ExitApplicationResult(
                true,
                Array.Empty<Guid>(),
                Array.Empty<Guid>());
        }

        var documentNames = modifiedDocuments
            .Select(info => info.DisplayTitle)
            .ToArray();

        var decision = _exitConfirmationService.ConfirmExit(documentNames);

        switch (decision)
        {
            case ExitConfirmationResult.ExitWithoutSaving:
                // 保存処理を実行せず、終了を許可する。
                return new ExitApplicationResult(
                    true,
                    Array.Empty<Guid>(),
                    Array.Empty<Guid>());

            case ExitConfirmationResult.Cancel:
                // ユーザーキャンセルはエラー通知せず、終了のみ中止する。
                return new ExitApplicationResult(
                    false,
                    Array.Empty<Guid>(),
                    Array.Empty<Guid>());

            case ExitConfirmationResult.SaveAndExit:
                return SaveModifiedDocuments(modifiedDocuments);

            default:
                // 未知の値を終了許可として扱わない。
                return new ExitApplicationResult(
                    false,
                    Array.Empty<Guid>(),
                    Array.Empty<Guid>());
        }
    }

    private ExitApplicationResult SaveModifiedDocuments(
        IReadOnlyList<OpenDocumentInfo> modifiedDocuments)
    {
        var savedDocumentIds = new List<Guid>();
        var savedAsDocumentIds = new List<Guid>();

        foreach (var info in modifiedDocuments)
        {
            var document = info.Document;

            // 先行する保存処理等で変更が解消されていれば、再保存しない。
            if (!document.IsModified)
            {
                continue;
            }

            try
            {
                if (document.FilePath is null)
                {
                    // Save As前にタブを切り替えて、ユーザーがどのDocumentか認識できるようにする
                    _documentActivationService.ActivateDocument(document.Id);

                    // Save Asのキャンセルはfalseとして返り、通知せず終了を中止する。
                    // それ以前に保存成功したDocumentのIDは結果へ含める。
                    if (!_saveDocumentAsUseCase.Execute(document))
                    {
                        return new ExitApplicationResult(
                            false,
                            savedDocumentIds,
                            savedAsDocumentIds);
                    }

                    savedDocumentIds.Add(document.Id);
                    savedAsDocumentIds.Add(document.Id);
                }
                else
                {
                    _saveDocumentUseCase.Execute(document);
                    savedDocumentIds.Add(document.Id);
                }
            }
            catch (Exception ex)
            {
                // 失敗した時点で停止する。成功済みDocumentの保存状態は維持し、
                // 失敗したDocumentおよび後続Documentは変更しない。
                _notificationService.ShowError(
                    $"「{info.DisplayTitle}」の保存に失敗したため、アプリケーションを終了しません。{Environment.NewLine}{ex.Message}");
                return new ExitApplicationResult(
                    false,
                    savedDocumentIds,
                    savedAsDocumentIds);
            }
        }

        return new ExitApplicationResult(
            true,
            savedDocumentIds,
            savedAsDocumentIds);
    }
}
