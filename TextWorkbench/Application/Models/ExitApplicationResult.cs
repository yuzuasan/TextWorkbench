namespace TextWorkbench.Application.Models;

/// <summary>
/// アプリケーション終了処理の結果を表します。
/// </summary>
public sealed class ExitApplicationResult
{
    /// <summary>
    /// アプリケーション終了を許可するかどうかを取得します。
    /// </summary>
    public bool CanExit { get; }

    /// <summary>
    /// 今回のExit処理中に保存に成功したDocument ID一覧を取得します。
    /// </summary>
    public IReadOnlyList<Guid> SavedDocumentIds { get; }

    /// <summary>
    /// 今回のExit処理中にSave Asに成功したDocument ID一覧を取得します。
    /// SavedDocumentIdsの部分集合です。
    /// </summary>
    public IReadOnlyList<Guid> SavedAsDocumentIds { get; }

    /// <summary>
    /// <see cref="ExitApplicationResult"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="canExit">アプリケーション終了を許可するかどうか</param>
    /// <param name="savedDocumentIds">保存に成功したDocument ID一覧</param>
    /// <param name="savedAsDocumentIds">Save Asに成功したDocument ID一覧</param>
    public ExitApplicationResult(
        bool canExit,
        IReadOnlyList<Guid> savedDocumentIds,
        IReadOnlyList<Guid> savedAsDocumentIds)
    {
        CanExit = canExit;
        SavedDocumentIds = savedDocumentIds ?? throw new ArgumentNullException(nameof(savedDocumentIds));
        SavedAsDocumentIds = savedAsDocumentIds ?? throw new ArgumentNullException(nameof(savedAsDocumentIds));
    }
}
