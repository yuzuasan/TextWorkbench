namespace TextWorkbench.Application.Interfaces;

/// <summary>
/// Documentに対応するタブをアクティブ化するためのインターフェースです。
/// </summary>
public interface IDocumentActivationService
{
    /// <summary>
    /// 指定したDocument IDに対応するタブをアクティブ化します。
    /// </summary>
    /// <param name="documentId">アクティブ化するDocumentのID</param>
    void ActivateDocument(Guid documentId);
}
