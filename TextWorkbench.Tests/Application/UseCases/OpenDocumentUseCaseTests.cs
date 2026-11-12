using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Tests.Application.UseCases;

/// <summary>
/// <see cref="OpenDocumentUseCase"/> の単体テストです。
/// </summary>
public class OpenDocumentUseCaseTests
{
    // コンストラクタ

    [Fact]
    public void コンストラクタ_fileDialogServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new OpenDocumentUseCase(
            null!, new FakeFileService(), new FakeEncodingService(), new FakeLineEndingService(LineEnding.CrLf, string.Empty)));
    }

    [Fact]
    public void コンストラクタ_fileServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new OpenDocumentUseCase(
            new FakeFileDialogService(null), null!, new FakeEncodingService(), new FakeLineEndingService(LineEnding.CrLf, string.Empty)));
    }

    [Fact]
    public void コンストラクタ_encodingServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new OpenDocumentUseCase(
            new FakeFileDialogService(null), new FakeFileService(), null!, new FakeLineEndingService(LineEnding.CrLf, string.Empty)));
    }

    [Fact]
    public void コンストラクタ_lineEndingServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new OpenDocumentUseCase(
            new FakeFileDialogService(null), new FakeFileService(), new FakeEncodingService(), null!));
    }

    // キャンセル

    [Fact]
    public void Execute_ダイアログでキャンセルした場合_nullが返る()
    {
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(
            new FakeFileDialogService(null),
            fileService,
            new FakeEncodingService(),
            new FakeLineEndingService(LineEnding.CrLf, string.Empty));

        var result = useCase.Execute();

        Assert.Null(result);
        Assert.Null(fileService.LastReadPath);
    }

    [Fact]
    public void Execute_ダイアログが空文字を返した場合_nullが返る()
    {
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(
            new FakeFileDialogService(string.Empty),
            fileService,
            new FakeEncodingService(),
            new FakeLineEndingService(LineEnding.CrLf, string.Empty));

        var result = useCase.Execute();

        Assert.Null(result);
        Assert.Null(fileService.LastReadPath);
    }

    // 正常系

    [Fact]
    public void Execute_正常系でDocumentが生成される()
    {
        var filePath = @"C:\work\sample.txt";
        var useCase = CreateUseCase(
            new FakeFileDialogService(filePath),
            new FakeFileService(new FileReadResult(new byte[] { 1, 2, 3 }, isReadOnly: true)),
            new FakeEncodingService(new DecodedText("content", TextEncoding.ShiftJis)),
            new FakeLineEndingService(LineEnding.Lf, "normalized"));

        var document = useCase.Execute();

        Assert.NotNull(document);
        Assert.Equal(Path.GetFullPath(filePath), document!.FilePath);
        Assert.Equal("sample.txt", document.FileName);
        Assert.Equal("normalized", document.CurrentContent);
        Assert.Equal(TextEncoding.ShiftJis, document.Encoding);
        Assert.Equal(LineEnding.Lf, document.LineEnding);
        Assert.True(document.IsReadOnly);
        Assert.False(document.IsModified);
    }

    [Fact]
    public void Execute_相対パスが正規化されてファイルサービスに渡される()
    {
        var fileService = new FakeFileService(new FileReadResult(Array.Empty<byte>(), false));
        var useCase = CreateUseCase(
            new FakeFileDialogService("sample.txt"),
            fileService,
            new FakeEncodingService(new DecodedText(string.Empty, TextEncoding.Utf8)),
            new FakeLineEndingService(LineEnding.CrLf, string.Empty));

        useCase.Execute();

        Assert.Equal(Path.GetFullPath("sample.txt"), fileService.LastReadPath);
    }

    [Fact]
    public void Execute_デコード結果がDetectとNormalizeToLfに渡される()
    {
        var lineEndingService = new FakeLineEndingService(LineEnding.Cr, "normalized");
        var useCase = CreateUseCase(
            new FakeFileDialogService(@"C:\work\sample.txt"),
            new FakeFileService(new FileReadResult(new byte[] { 1 }, false)),
            new FakeEncodingService(new DecodedText("decoded text", TextEncoding.Utf8)),
            lineEndingService);

        useCase.Execute();

        Assert.Equal("decoded text", lineEndingService.LastDetectedText);
        Assert.Equal("decoded text", lineEndingService.LastNormalizedText);
    }

    // 失敗系

    [Fact]
    public void Execute_ファイル読み込みで例外が発生した場合_例外が伝播する()
    {
        var fileService = new FakeFileService(exception: new FileOperationException("読み込み失敗"));
        var useCase = CreateUseCase(
            new FakeFileDialogService(@"C:\work\sample.txt"),
            fileService,
            new FakeEncodingService(new DecodedText(string.Empty, TextEncoding.Utf8)),
            new FakeLineEndingService(LineEnding.CrLf, string.Empty));

        Assert.Throws<FileOperationException>(() => useCase.Execute());
    }

    [Fact]
    public void Execute_デコードで例外が発生した場合_例外が伝播する()
    {
        var encodingService = new FakeEncodingService(exception: new EncodingOperationException("デコード失敗"));
        var useCase = CreateUseCase(
            new FakeFileDialogService(@"C:\work\sample.txt"),
            new FakeFileService(new FileReadResult(new byte[] { 1 }, false)),
            encodingService,
            new FakeLineEndingService(LineEnding.CrLf, string.Empty));

        Assert.Throws<EncodingOperationException>(() => useCase.Execute());
    }

    // テストデータ作成ヘルパー

    private static OpenDocumentUseCase CreateUseCase(
        IFileDialogService fileDialogService,
        IFileService fileService,
        IEncodingService encodingService,
        ILineEndingService lineEndingService)
    {
        return new OpenDocumentUseCase(fileDialogService, fileService, encodingService, lineEndingService);
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        private readonly string? _openResult;

        public FakeFileDialogService(string? openResult)
        {
            _openResult = openResult;
        }

        public string? ShowOpenFileDialog() => _openResult;

        public string? ShowSaveFileDialog(string? initialFilePath) => null;
    }

    private sealed class FakeFileService : IFileService
    {
        private readonly FileReadResult? _readResult;
        private readonly Exception? _exception;

        public string? LastReadPath { get; private set; }

        public FakeFileService(FileReadResult? readResult = null, Exception? exception = null)
        {
            _readResult = readResult;
            _exception = exception;
        }

        public FileReadResult ReadAllBytes(string filePath)
        {
            LastReadPath = filePath;
            if (_exception != null)
                throw _exception;
            return _readResult!;
        }

        public void WriteAllBytes(string filePath, byte[] bytes)
        {
        }
    }

    private sealed class FakeEncodingService : IEncodingService
    {
        private readonly DecodedText? _decodedText;
        private readonly Exception? _exception;

        public FakeEncodingService(DecodedText? decodedText = null, Exception? exception = null)
        {
            _decodedText = decodedText;
            _exception = exception;
        }

        public DecodedText Decode(byte[] bytes)
        {
            if (_exception != null)
                throw _exception;
            return _decodedText!;
        }

        public byte[] Encode(string text, TextEncoding encoding)
        {
            return Array.Empty<byte>();
        }
    }

    private sealed class FakeLineEndingService : ILineEndingService
    {
        private readonly LineEnding _detected;
        private readonly string _normalized;

        public string? LastDetectedText { get; private set; }
        public string? LastNormalizedText { get; private set; }

        public FakeLineEndingService(LineEnding detected, string normalized)
        {
            _detected = detected;
            _normalized = normalized;
        }

        public LineEnding Detect(string text)
        {
            LastDetectedText = text;
            return _detected;
        }

        public string NormalizeToLf(string text)
        {
            LastNormalizedText = text;
            return _normalized;
        }

        public string ConvertFromLf(string text, LineEnding lineEnding)
        {
            return text;
        }
    }
}
