namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// 保存先パスを使用中の別Documentが存在するかを確認するためのインターフェースです。
/// </summary>
public interface IDocumentPathRegistry
{
    /// <summary>
    /// 指定したファイルパスを使用している別Documentが存在するかを確認します。
    /// </summary>
    /// <param name="filePath">正規化済みの絶対ファイルパス</param>
    /// <param name="excludingDocumentId">重複確認から除外するDocumentのID</param>
    /// <returns>指定パスを使用している別Documentが存在する場合はtrue</returns>
    bool IsPathInUse(string filePath, Guid excludingDocumentId);
}
