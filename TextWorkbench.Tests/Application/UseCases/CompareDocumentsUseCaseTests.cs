using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Tests.Application.UseCases;

/// <summary>
/// <see cref="CompareDocumentsUseCase"/> の単体テストです。
/// </summary>
public class CompareDocumentsUseCaseTests
{
    // コンストラクタ

    [Fact]
    public void コンストラクタ_fileServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new CompareDocumentsUseCase(
            null!, new FakeEncodingService(), new FakeLineEndingService(), new FakeDiffService(), new FakeDocumentContentProvider()));
    }

    [Fact]
    public void コンストラクタ_encodingServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new CompareDocumentsUseCase(
            new FakeFileService(), null!, new FakeLineEndingService(), new FakeDiffService(), new FakeDocumentContentProvider()));
    }

    [Fact]
    public void コンストラクタ_lineEndingServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new CompareDocumentsUseCase(
            new FakeFileService(), new FakeEncodingService(), null!, new FakeDiffService(), new FakeDocumentContentProvider()));
    }

    [Fact]
    public void コンストラクタ_diffServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new CompareDocumentsUseCase(
            new FakeFileService(), new FakeEncodingService(), new FakeLineEndingService(), null!, new FakeDocumentContentProvider()));
    }

    [Fact]
    public void コンストラクタ_documentContentProviderがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new CompareDocumentsUseCase(
            new FakeFileService(), new FakeEncodingService(), new FakeLineEndingService(), new FakeDiffService(), null!));
    }

    // ガード

    [Fact]
    public void Execute_leftSourceがnullの場合_ArgumentNullExceptionが発生する()
    {
        var useCase = CreateUseCase();
        var source = new ComparisonSource(SourceType.File, @"C:\work\a.txt");

        Assert.Throws<ArgumentNullException>(() => useCase.Execute(null!, source));
    }

    [Fact]
    public void Execute_rightSourceがnullの場合_ArgumentNullExceptionが発生する()
    {
        var useCase = CreateUseCase();
        var source = new ComparisonSource(SourceType.File, @"C:\work\a.txt");

        Assert.Throws<ArgumentNullException>(() => useCase.Execute(source, null!));
    }

    // File×File 正常系

    [Fact]
    public void Execute_File同士の比較でDiffSessionが生成される()
    {
        var leftPath = @"C:\work\left.txt";
        var rightPath = @"C:\work\right.txt";
        var fileService = new FakeFileService()
            .AddFile(leftPath, new byte[] { 1 })
            .AddFile(rightPath, new byte[] { 2 });
        var encodingService = new FakeEncodingService()
            .AddDecodeResult(new DecodedText("left raw", TextEncoding.Utf8))
            .AddDecodeResult(new DecodedText("right raw", TextEncoding.ShiftJis));
        var lineEndingService = new FakeLineEndingService()
            .AddNormalizeResult("left normalized")
            .AddNormalizeResult("right normalized");
        var diffResult = new DiffResult("left.txt", "right.txt", Array.Empty<DiffLine>());
        var diffService = new FakeDiffService(diffResult);
        var useCase = CreateUseCase(fileService, encodingService, lineEndingService, diffService);

        var session = useCase.Execute(
            new ComparisonSource(SourceType.File, leftPath),
            new ComparisonSource(SourceType.File, rightPath));

        // DiffServiceへの受け渡し
        Assert.Equal("left.txt", diffService.LastLeftFileName);
        Assert.Equal("right.txt", diffService.LastRightFileName);
        Assert.Equal("left normalized", diffService.LastLeftText);
        Assert.Equal("right normalized", diffService.LastRightText);

        // スナップショット
        Assert.Equal(SourceType.File, session.LeftSource.SourceType);
        Assert.Equal(leftPath, session.LeftSource.SourceIdentifier);
        Assert.Equal(Path.GetFullPath(leftPath), session.LeftSource.FilePath);
        Assert.Equal("left.txt", session.LeftSource.DisplayName);
        Assert.Equal(TextEncoding.Utf8, session.LeftSource.Encoding);
        Assert.Equal("left normalized", session.LeftSource.Content);
        Assert.Equal("right.txt", session.RightSource.DisplayName);
        Assert.Equal(TextEncoding.ShiftJis, session.RightSource.Encoding);
        Assert.Same(diffResult, session.Result);
    }

    [Fact]
    public void Execute_File比較元のパスが正規化されて読み込まれる()
    {
        var fileService = new FakeFileService()
            .AddFile("relative.txt", new byte[] { 1 });
        var useCase = CreateUseCase(fileService: fileService);

        useCase.Execute(
            new ComparisonSource(SourceType.File, "relative.txt"),
            new ComparisonSource(SourceType.File, "relative.txt"));

        Assert.Equal(Path.GetFullPath("relative.txt"), fileService.LastReadPath);
    }

    // OpenDocument×OpenDocument 正常系

    [Fact]
    public void Execute_OpenDocument同士の比較でDocument内容が使われる()
    {
        var doc1 = CreateDocument(@"C:\work\a.txt", "a.txt", "doc1 content", TextEncoding.ShiftJis);
        var doc2 = CreateDocument(null, "無題1", "doc2 content", TextEncoding.Utf8);
        var provider = new FakeDocumentContentProvider()
            .AddDocument(doc1, "a.txt")
            .AddDocument(doc2, "無題1");
        var diffResult = new DiffResult("a.txt", "無題1", Array.Empty<DiffLine>());
        var diffService = new FakeDiffService(diffResult);
        var useCase = CreateUseCase(
            documentContentProvider: provider,
            diffService: diffService);

        var session = useCase.Execute(
            new ComparisonSource(SourceType.OpenDocument, doc1.Id.ToString()),
            new ComparisonSource(SourceType.OpenDocument, doc2.Id.ToString()));

        Assert.Equal("a.txt", diffService.LastLeftFileName);
        Assert.Equal("無題1", diffService.LastRightFileName);
        Assert.Equal("doc1 content", diffService.LastLeftText);
        Assert.Equal("doc2 content", diffService.LastRightText);

        Assert.Equal(doc1.Id.ToString(), session.LeftSource.SourceIdentifier);
        Assert.Equal(@"C:\work\a.txt", session.LeftSource.FilePath);
        Assert.Equal("a.txt", session.LeftSource.DisplayName);
        Assert.Equal(TextEncoding.ShiftJis, session.LeftSource.Encoding);
        Assert.Equal("doc1 content", session.LeftSource.Content);

        // 無題DocumentはFilePathがnull、EncodingはUtf8固定
        Assert.Null(session.RightSource.FilePath);
        Assert.Equal("無題1", session.RightSource.DisplayName);
        Assert.Equal(TextEncoding.Utf8, session.RightSource.Encoding);
    }

    // 異常系：比較元の検証

    [Fact]
    public void Execute_File比較元のSourceIdentifierが空の場合_ArgumentExceptionが発生する()
    {
        var useCase = CreateUseCase();
        var source = new ComparisonSource(SourceType.File, "  ");

        Assert.Throws<ArgumentException>(() => useCase.Execute(source, source));
    }

    [Fact]
    public void Execute_OpenDocument比較元のSourceIdentifierが空の場合_ArgumentExceptionが発生する()
    {
        var useCase = CreateUseCase();
        var source = new ComparisonSource(SourceType.OpenDocument, " ");

        Assert.Throws<ArgumentException>(() => useCase.Execute(source, source));
    }

    [Fact]
    public void Execute_OpenDocument比較元のSourceIdentifierがGuidでない場合_ArgumentExceptionが発生する()
    {
        var useCase = CreateUseCase();
        var source = new ComparisonSource(SourceType.OpenDocument, "not-a-guid");

        Assert.Throws<ArgumentException>(() => useCase.Execute(source, source));
    }

    [Fact]
    public void Execute_未知のSourceTypeの場合_ArgumentOutOfRangeExceptionが発生する()
    {
        var useCase = CreateUseCase();
        var source = new ComparisonSource((SourceType)999, "id");

        Assert.Throws<ArgumentOutOfRangeException>(() => useCase.Execute(source, source));
    }

    [Fact]
    public void Execute_Documentが見つからない場合_DocumentNotFoundExceptionが伝播する()
    {
        var provider = new FakeDocumentContentProvider();
        var useCase = CreateUseCase(documentContentProvider: provider);
        var source = new ComparisonSource(SourceType.OpenDocument, Guid.NewGuid().ToString());

        Assert.Throws<DocumentNotFoundException>(() => useCase.Execute(source, source));
    }

    // 異常系：Diff・I/O失敗

    [Fact]
    public void Execute_DiffServiceが例外をスローした場合_DiffOperationExceptionにラップされる()
    {
        var diffService = new FakeDiffService(exception: new InvalidOperationException("diff error"));
        var useCase = CreateUseCase(diffService: diffService);
        var source = new ComparisonSource(SourceType.File, "a.txt");

        var ex = Assert.Throws<DiffOperationException>(() => useCase.Execute(source, source));
        Assert.Equal("比較に失敗しました。", ex.Message);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void Execute_ファイル読み込みで例外が発生した場合_DiffOperationExceptionにラップされず伝播する()
    {
        var fileService = new FakeFileService()
            .AddFile("a.txt", exception: new FileOperationException("読み込み失敗"));
        var useCase = CreateUseCase(fileService: fileService);
        var source = new ComparisonSource(SourceType.File, "a.txt");

        Assert.Throws<FileOperationException>(() => useCase.Execute(source, source));
    }

    // 混合比較：File×OpenDocument

    [Fact]
    public void Execute_FileとOpenDocumentの混合比較で正しい情報が使われる()
    {
        var filePath = @"C:\work\left.txt";
        var fileService = new FakeFileService().AddFile(filePath, new byte[] { 1 });
        var encodingService = new FakeEncodingService()
            .AddDecodeResult(new DecodedText("file raw", TextEncoding.ShiftJis));
        var lineEndingService = new FakeLineEndingService()
            .AddNormalizeResult("file normalized");
        var doc = CreateDocument(@"C:\work\doc.txt", "doc.txt", "doc content", TextEncoding.Utf8);
        var provider = new FakeDocumentContentProvider().AddDocument(doc, "doc.txt");
        var diffResult = new DiffResult("left.txt", "doc.txt", Array.Empty<DiffLine>());
        var diffService = new FakeDiffService(diffResult);
        var useCase = CreateUseCase(
            fileService, encodingService, lineEndingService, diffService, provider);

        var session = useCase.Execute(
            new ComparisonSource(SourceType.File, filePath),
            new ComparisonSource(SourceType.OpenDocument, doc.Id.ToString()));

        // DiffServiceへの引数：左=File側（正規化済み）、右=Document側（CurrentContentそのまま）
        Assert.Equal("left.txt", diffService.LastLeftFileName);
        Assert.Equal("doc.txt", diffService.LastRightFileName);
        Assert.Equal("file normalized", diffService.LastLeftText);
        Assert.Equal("doc content", diffService.LastRightText);

        // File側のみNormalizeToLfが呼ばれる（OpenDocument側は呼ばれない非対称仕様）
        Assert.Single(lineEndingService.NormalizedInputs);
        Assert.Equal("file raw", lineEndingService.NormalizedInputs[0]);

        // File側スナップショットの全プロパティ
        Assert.Equal(SourceType.File, session.LeftSource.SourceType);
        Assert.Equal(filePath, session.LeftSource.SourceIdentifier);
        Assert.Equal(Path.GetFullPath(filePath), session.LeftSource.FilePath);
        Assert.Equal("left.txt", session.LeftSource.DisplayName);
        Assert.Equal(TextEncoding.ShiftJis, session.LeftSource.Encoding);
        Assert.Equal("file normalized", session.LeftSource.Content);

        // OpenDocument側スナップショットの全プロパティ
        Assert.Equal(SourceType.OpenDocument, session.RightSource.SourceType);
        Assert.Equal(doc.Id.ToString(), session.RightSource.SourceIdentifier);
        Assert.Equal(@"C:\work\doc.txt", session.RightSource.FilePath);
        Assert.Equal("doc.txt", session.RightSource.DisplayName);
        Assert.Equal(TextEncoding.Utf8, session.RightSource.Encoding);
        Assert.Equal("doc content", session.RightSource.Content);
    }

    [Fact]
    public void Execute_OpenDocumentとFileの逆順混合比較で正しい情報が使われる()
    {
        var filePath = @"C:\work\right.txt";
        var fileService = new FakeFileService().AddFile(filePath, new byte[] { 1 });
        var encodingService = new FakeEncodingService()
            .AddDecodeResult(new DecodedText("file raw", TextEncoding.Utf8));
        var lineEndingService = new FakeLineEndingService()
            .AddNormalizeResult("file normalized");
        var doc = CreateDocument(null, "無題1", "doc content", TextEncoding.Utf8);
        var provider = new FakeDocumentContentProvider().AddDocument(doc, "無題1");
        var diffResult = new DiffResult("無題1", "right.txt", Array.Empty<DiffLine>());
        var diffService = new FakeDiffService(diffResult);
        var useCase = CreateUseCase(
            fileService, encodingService, lineEndingService, diffService, provider);

        var session = useCase.Execute(
            new ComparisonSource(SourceType.OpenDocument, doc.Id.ToString()),
            new ComparisonSource(SourceType.File, filePath));

        // DiffServiceへの引数：左=Document側、右=File側
        Assert.Equal("無題1", diffService.LastLeftFileName);
        Assert.Equal("right.txt", diffService.LastRightFileName);
        Assert.Equal("doc content", diffService.LastLeftText);
        Assert.Equal("file normalized", diffService.LastRightText);

        // スナップショットの左右対応
        Assert.Equal(SourceType.OpenDocument, session.LeftSource.SourceType);
        Assert.Equal("doc content", session.LeftSource.Content);
        Assert.Equal(SourceType.File, session.RightSource.SourceType);
        Assert.Equal("file normalized", session.RightSource.Content);
    }

    // スナップショット不変性

    [Fact]
    public void Execute_比較後に元Documentを編集してもDiffSessionの内容は変わらない()
    {
        var doc = CreateDocument(@"C:\work\a.txt", "a.txt", "before", TextEncoding.Utf8);
        var provider = new FakeDocumentContentProvider().AddDocument(doc, "a.txt");
        var diffService = new FakeDiffService(CreateEmptyDiffResult());
        var useCase = CreateUseCase(
            documentContentProvider: provider,
            diffService: diffService);
        var fileSource = new ComparisonSource(SourceType.File, "a.txt");
        var docSource = new ComparisonSource(SourceType.OpenDocument, doc.Id.ToString());

        var session = useCase.Execute(docSource, fileSource);

        // 比較後にDocumentを編集
        doc.UpdateContent("after");

        // DiffSessionのスナップショットは比較時点の内容を保持
        Assert.Equal("before", session.LeftSource.Content);
        Assert.Equal("before", diffService.LastLeftText);
    }

    // テストデータ作成ヘルパー

    private static CompareDocumentsUseCase CreateUseCase(
        FakeFileService? fileService = null,
        FakeEncodingService? encodingService = null,
        FakeLineEndingService? lineEndingService = null,
        FakeDiffService? diffService = null,
        FakeDocumentContentProvider? documentContentProvider = null)
    {
        return new CompareDocumentsUseCase(
            fileService ?? new FakeFileService(),
            encodingService ?? new FakeEncodingService(),
            lineEndingService ?? new FakeLineEndingService(),
            diffService ?? new FakeDiffService(CreateEmptyDiffResult()),
            documentContentProvider ?? new FakeDocumentContentProvider());
    }

    private static DiffResult CreateEmptyDiffResult()
    {
        return new DiffResult("left", "right", Array.Empty<DiffLine>());
    }

    private static Document CreateDocument(string? filePath, string? fileName, string content, TextEncoding encoding)
    {
        if (filePath == null)
        {
            var untitled = new Document();
            untitled.UpdateContent(content);
            return untitled;
        }

        return new Document(filePath, fileName!, content, encoding, LineEnding.CrLf, false);
    }

    private sealed class FakeFileService : IFileService
    {
        private readonly Dictionary<string, FileReadResult> _files = new();
        private readonly Dictionary<string, Exception> _exceptions = new();

        public string? LastReadPath { get; private set; }

        public FakeFileService AddFile(string path, byte[] content)
        {
            _files[Path.GetFullPath(path)] = new FileReadResult(content, false);
            return this;
        }

        public FakeFileService AddFile(string path, Exception exception)
        {
            _exceptions[Path.GetFullPath(path)] = exception;
            return this;
        }

        public FileReadResult ReadAllBytes(string filePath)
        {
            LastReadPath = filePath;
            var fullPath = Path.GetFullPath(filePath);
            if (_exceptions.TryGetValue(fullPath, out var exception))
                throw exception;
            if (_files.TryGetValue(fullPath, out var result))
                return result;
            return new FileReadResult(Array.Empty<byte>(), false);
        }

        public void WriteAllBytes(string filePath, byte[] bytes)
        {
        }
    }

    private sealed class FakeEncodingService : IEncodingService
    {
        private readonly Queue<DecodedText> _results = new();
        private readonly DecodedText _defaultResult = new(string.Empty, TextEncoding.Utf8);

        public FakeEncodingService AddDecodeResult(DecodedText result)
        {
            _results.Enqueue(result);
            return this;
        }

        public DecodedText Decode(byte[] bytes)
        {
            return _results.Count > 0 ? _results.Dequeue() : _defaultResult;
        }

        public byte[] Encode(string text, TextEncoding encoding)
        {
            return Array.Empty<byte>();
        }
    }

    private sealed class FakeLineEndingService : ILineEndingService
    {
        private readonly Queue<string> _results = new();

        public List<string> NormalizedInputs { get; } = new();

        public FakeLineEndingService AddNormalizeResult(string result)
        {
            _results.Enqueue(result);
            return this;
        }

        public LineEnding Detect(string text) => LineEnding.CrLf;

        public string NormalizeToLf(string text)
        {
            NormalizedInputs.Add(text);
            return _results.Count > 0 ? _results.Dequeue() : text;
        }

        public string ConvertFromLf(string text, LineEnding lineEnding) => text;
    }

    private sealed class FakeDiffService : IDiffService
    {
        private readonly DiffResult? _result;
        private readonly Exception? _exception;

        public string? LastLeftFileName { get; private set; }
        public string? LastRightFileName { get; private set; }
        public string? LastLeftText { get; private set; }
        public string? LastRightText { get; private set; }

        public FakeDiffService(DiffResult? result = null, Exception? exception = null)
        {
            _result = result;
            _exception = exception;
        }

        public DiffResult Compare(string leftFileName, string rightFileName, string leftText, string rightText)
        {
            LastLeftFileName = leftFileName;
            LastRightFileName = rightFileName;
            LastLeftText = leftText;
            LastRightText = rightText;
            if (_exception != null)
                throw _exception;
            return _result!;
        }
    }

    private sealed class FakeDocumentContentProvider : IDocumentContentProvider
    {
        private readonly Dictionary<Guid, (Document Document, string DisplayTitle)> _documents = new();

        public FakeDocumentContentProvider AddDocument(Document document, string displayTitle)
        {
            _documents[document.Id] = (document, displayTitle);
            return this;
        }

        public Document GetDocument(Guid documentId)
        {
            if (_documents.TryGetValue(documentId, out var info))
                return info.Document;
            throw new DocumentNotFoundException($"ID {documentId} のドキュメントが見つかりませんでした。");
        }

        public string GetDisplayTitle(Guid documentId)
        {
            if (_documents.TryGetValue(documentId, out var info))
                return info.DisplayTitle;
            throw new DocumentNotFoundException($"ID {documentId} のドキュメントが見つかりませんでした。");
        }
    }
}
