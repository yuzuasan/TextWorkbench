using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// 新規Documentを作成するUseCaseです。
/// </summary>
public sealed class NewDocumentUseCase
{
    /// <summary>
    /// 新規Documentを作成します。
    /// </summary>
    /// <returns>新規Document</returns>
    public Document Execute()
    {
        return new Document();
    }
}
