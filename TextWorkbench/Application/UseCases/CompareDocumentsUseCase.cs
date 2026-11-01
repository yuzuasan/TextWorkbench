using System.IO;
using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// 2つの保存済みファイルを比較し、DiffSessionを生成するUseCaseです。
/// </summary>
public sealed class CompareDocumentsUseCase
{
    private readonly IFileService _fileService;
    private readonly IEncodingService _encodingService;
    private readonly ILineEndingService _lineEndingService;
    private readonly IDiffService _diffService;

    /// <summary>
    /// <see cref="CompareDocumentsUseCase"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="fileService">ファイルサービス</param>
    /// <param name="encodingService">エンコーディングサービス</param>
    /// <param name="lineEndingService">改行コードサービス</param>
    /// <param name="diffService">Diffサービス</param>
    public CompareDocumentsUseCase(
        IFileService fileService,
        IEncodingService encodingService,
        ILineEndingService lineEndingService,
        IDiffService diffService)
    {
        _fileService = fileService
            ?? throw new ArgumentNullException(nameof(fileService));

        _encodingService = encodingService
            ?? throw new ArgumentNullException(nameof(encodingService));

        _lineEndingService = lineEndingService
            ?? throw new ArgumentNullException(nameof(lineEndingService));

        _diffService = diffService
            ?? throw new ArgumentNullException(nameof(diffService));
    }

    /// <summary>
    /// 2つのファイルを比較します。
    /// </summary>
    /// <param name="leftSource">左側の比較元</param>
    /// <param name="rightSource">右側の比較元</param>
    /// <returns>比較結果を保持したDiffSession</returns>
    public DiffSession Execute(
        ComparisonSource leftSource,
        ComparisonSource rightSource)
    {
        ArgumentNullException.ThrowIfNull(leftSource);
        ArgumentNullException.ThrowIfNull(rightSource);

        // 比較元のファイルパスを取得・正規化
        var leftFilePath = GetFilePath(leftSource);
        var rightFilePath = GetFilePath(rightSource);

        // ファイル読み込み
        var leftReadResult = _fileService.ReadAllBytes(leftFilePath);
        var rightReadResult = _fileService.ReadAllBytes(rightFilePath);

        // デコード
        var leftDecodedText = _encodingService.Decode(leftReadResult.Content);
        var rightDecodedText = _encodingService.Decode(rightReadResult.Content);

        // 改行コードをLFに正規化
        var leftContent = _lineEndingService.NormalizeToLf(leftDecodedText.Text);
        var rightContent = _lineEndingService.NormalizeToLf(rightDecodedText.Text);

        // 表示名を取得
        var leftDisplayName = Path.GetFileName(leftFilePath);
        var rightDisplayName = Path.GetFileName(rightFilePath);

        // Diff実行
        DiffResult diffResult;

        try
        {
            diffResult = _diffService.Compare(
                leftDisplayName,
                rightDisplayName,
                leftContent,
                rightContent);
        }
        catch (Exception exception)
        {
            throw new DiffOperationException(
                "ファイルの比較に失敗しました。",
                exception);
        }

        // 比較開始時点の内容をスナップショットとして保持
        var leftSnapshot = new DiffSession.DiffSource(
            leftFilePath,
            leftDisplayName,
            leftDecodedText.Encoding,
            leftContent);

        var rightSnapshot = new DiffSession.DiffSource(
            rightFilePath,
            rightDisplayName,
            rightDecodedText.Encoding,
            rightContent);

        return new DiffSession(
            leftSnapshot,
            rightSnapshot,
            diffResult);
    }

    /// <summary>
    /// 比較元からファイルパスを取得し、絶対パスへ正規化します。
    /// </summary>
    /// <param name="source">比較元</param>
    /// <returns>正規化されたファイルパス</returns>
    private static string GetFilePath(ComparisonSource source)
    {
        if (source.SourceType != SourceType.File)
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source.SourceType,
                "対応していない比較元の種類です。");
        }

        if (string.IsNullOrWhiteSpace(source.SourceIdentifier))
        {
            throw new ArgumentException(
                "比較元のファイルパスを指定してください。",
                nameof(source));
        }

        return Path.GetFullPath(source.SourceIdentifier);
    }
}
