namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// ユーザーに確認を求めるサービスのインターフェースです。
/// </summary>
public interface IUserConfirmationService
{
    /// <summary>
    /// Documentの変更内容を保存するかどうかを確認します。
    /// </summary>
    /// <param name="documentTitle">確認対象Documentの表示名</param>
    /// <returns>保存、保存せず続行、キャンセルのいずれか</returns>
    SaveConfirmationResult ConfirmSaveChanges(string documentTitle);
}

/// <summary>
/// 変更内容の保存確認結果を表します。
/// </summary>
public enum SaveConfirmationResult
{
    /// <summary>変更内容を保存します。</summary>
    Save,

    /// <summary>変更内容を保存せずに続行します。</summary>
    DontSave,

    /// <summary>操作をキャンセルします。</summary>
    Cancel
}
