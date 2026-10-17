namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// アプリケーション終了時に、未保存Documentの処理方針を確認するサービスです。
/// </summary>
public interface IExitConfirmationService
{
    /// <summary>
    /// 未保存Documentの一覧を提示し、終了時の処理方針を確認します。
    /// </summary>
    /// <param name="documentNames">Workspaceのタブ順に並んだ未保存Document名</param>
    /// <returns>保存して終了、保存せず終了、キャンセルのいずれか</returns>
    ExitConfirmationResult ConfirmExit(IReadOnlyList<string> documentNames);
}

/// <summary>
/// アプリケーション終了時の確認結果を表します。
/// </summary>
public enum ExitConfirmationResult
{
    /// <summary>未保存Documentを保存して終了します。</summary>
    SaveAndExit,

    /// <summary>未保存Documentを保存せず終了します。</summary>
    ExitWithoutSaving,

    /// <summary>終了をキャンセルします。</summary>
    Cancel
}
