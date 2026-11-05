using System.IO;
using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Application.UseCases;

/// <summary>
/// 2つの比較元を比較し、DiffSessionを生成するUseCaseです。
/// </summary>
public sealed class CompareDocumentsUseCase
{
    private readonly IFileService _fileService;
    private readonly IEncodingService _encodingService;
    private readonly ILineEndingService _lineEndingService;
    private readonly IDiffService _diffService;
    private readonly IDocumentContentProvider _documentContentProvider;

    /// <summary>
    /// <see cref="CompareDocumentsUseCase"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="fileService">ファイルサービス</param>
    /// <param name="encodingService">エンコーディングサービス</param>
    /// <param name="lineEndingService">改行コードサービス</param>
    /// <param name="diffService">Diffサービス</param>
    /// <param name="documentContentProvider">Document内容取得プロバイダー</param>
    public CompareDocumentsUseCase(
        IFileService fileService,
        IEncodingService encodingService,
        ILineEndingService lineEndingService,
        IDiffService diffService,
        IDocumentContentProvider documentContentProvider)
    {
        _fileService = fileService
            ?? throw new ArgumentNullException(nameof(fileService));

        _encodingService = encodingService
            ?? throw new ArgumentNullException(nameof(encodingService));

        _lineEndingService = lineEndingService
            ?? throw new ArgumentNullException(nameof(lineEndingService));

        _diffService = diffService
            ?? throw new ArgumentNullException(nameof(diffService));

        _documentContentProvider = documentContentProvider
            ?? throw new ArgumentNullException(nameof(documentContentProvider));
    }

    /// <summary>
    /// 2つの比較元を比較します。
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

        // 左側の比較元情報を取得
        var leftInfo = GetSourceInfo(leftSource);

        // 右側の比較元情報を取得
        var rightInfo = GetSourceInfo(rightSource);

        // Diff実行
        DiffResult diffResult;

        try
        {
            diffResult = _diffService.Compare(
                leftInfo.DisplayName,
                rightInfo.DisplayName,
                leftInfo.Content,
                rightInfo.Content);
        }
        catch (Exception exception)
        {
            throw new DiffOperationException(
                "比較に失敗しました。",
                exception);
        }

        // 比較開始時点の内容をスナップショットとして保持
        var leftSnapshot = new DiffSession.DiffSource(
            leftSource.SourceType,
            leftSource.SourceIdentifier,
            leftInfo.FilePath,
            leftInfo.DisplayName,
            leftInfo.Encoding,
            leftInfo.Content);

        var rightSnapshot = new DiffSession.DiffSource(
            rightSource.SourceType,
            rightSource.SourceIdentifier,
            rightInfo.FilePath,
            rightInfo.DisplayName,
            rightInfo.Encoding,
            rightInfo.Content);

        return new DiffSession(
            leftSnapshot,
            rightSnapshot,
            diffResult);
    }

    /// <summary>
    /// 比較元から比較情報を取得します。
    /// </summary>
    /// <param name="source">比較元</param>
    /// <returns>比較情報</returns>
    private SourceInfo GetSourceInfo(ComparisonSource source)
    {
        switch (source.SourceType)
        {
            case SourceType.File:
                return GetFileSourceInfo(source);

            case SourceType.OpenDocument:
                return GetDocumentSourceInfo(source);

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(source),
                    source.SourceType,
                    "対応していない比較元の種類です。");
        }
    }

    /// <summary>
    /// File比較元から比較情報を取得します。
    /// </summary>
    /// <param name="source">比較元</param>
    /// <returns>比較情報</returns>
    private SourceInfo GetFileSourceInfo(ComparisonSource source)
    {
        if (string.IsNullOrWhiteSpace(source.SourceIdentifier))
        {
            throw new ArgumentException(
                "比較元のファイルパスを指定してください。",
                nameof(source));
        }

        var filePath = Path.GetFullPath(source.SourceIdentifier);

        // ファイル読み込み
        var readResult = _fileService.ReadAllBytes(filePath);

        // デコード
        var decodedText = _encodingService.Decode(readResult.Content);

        // 改行コードをLFに正規化
        var content = _lineEndingService.NormalizeToLf(decodedText.Text);

        // 表示名を取得
        var displayName = Path.GetFileName(filePath);

        return new SourceInfo(
            filePath,
            displayName,
            decodedText.Encoding,
            content);
    }

    /// <summary>
    /// OpenDocument比較元から比較情報を取得します。
    /// </summary>
    /// <param name="source">比較元</param>
    /// <returns>比較情報</returns>
    private SourceInfo GetDocumentSourceInfo(ComparisonSource source)
    {
        if (string.IsNullOrWhiteSpace(source.SourceIdentifier))
        {
            throw new ArgumentException(
                "比較元のDocument.Idを指定してください。",
                nameof(source));
        }

        if (!Guid.TryParse(source.SourceIdentifier, out var documentId))
        {
            throw new ArgumentException(
                "比較元のDocument.Idが不正です。",
                nameof(source));
        }

        // IDocumentContentProviderからDocumentを取得
        var document = _documentContentProvider.GetDocument(documentId);

        // DisplayTitleを取得
        var displayTitle = _documentContentProvider.GetDisplayTitle(documentId);

        return new SourceInfo(
            document.FilePath,
            displayTitle,
            document.Encoding,
            document.CurrentContent);
    }

    /// <summary>
    /// 比較情報を表します。
    /// </summary>
    private sealed class SourceInfo
    {
        /// <summary>
        /// ファイルパスを取得します。
        /// </summary>
        public string? FilePath { get; }

        /// <summary>
        /// 表示名を取得します。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 文字コードを取得します。
        /// </summary>
        public TextEncoding Encoding { get; }

        /// <summary>
        /// 内容を取得します。
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// <see cref="SourceInfo"/> の新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <param name="displayName">表示名</param>
        /// <param name="encoding">文字コード</param>
        /// <param name="content">内容</param>
        public SourceInfo(string? filePath, string displayName, TextEncoding encoding, string content)
        {
            FilePath = filePath;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Encoding = encoding;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }
    }
}
