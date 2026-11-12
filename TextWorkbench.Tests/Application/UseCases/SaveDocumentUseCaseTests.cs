using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Tests.Application.UseCases;

/// <summary>
/// <see cref="SaveDocumentUseCase"/> の単体テストです。
/// </summary>
public class SaveDocumentUseCaseTests
{
    // コンストラクタ

    [Fact]
    public void コンストラクタ_fileServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new SaveDocumentUseCase(
            null!, new FakeEncodingService(), new FakeLineEndingService()));
    }

    [Fact]
    public void コンストラクタ_encodingServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new SaveDocumentUseCase(
            new FakeFileService(), null!, new FakeLineEndingService()));
    }

    [Fact]
    public void コンストラクタ_lineEndingServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new SaveDocumentUseCase(
            new FakeFileService(), new FakeEncodingService(), null!));
    }

    // ガード

    [Fact]
    public void Execute_documentがnullの場合_ArgumentNullExceptionが発生する()
    {
        var useCase = CreateUseCase();

        Assert.Throws<ArgumentNullException>(() => useCase.Execute(null!));
    }

    [Fact]
    public void Execute_FilePathがnullのDocumentの場合_ValidationOperationExceptionが発生し書き込まれない()
    {
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(fileService: fileService);
        var document = new Document();

        var ex = Assert.Throws<ValidationOperationException>(() => useCase.Execute(document));

        Assert.Equal("FilePathがnullのDocumentは保存できません。Save Asを使用してください。", ex.Message);
        Assert.False(fileService.WriteAllBytesCalled);
    }

    // 正常系

    [Fact]
    public void Execute_正常系でサービス間の引数が連鎖し保存成功後にIsModifiedがfalseになる()
    {
        var lineEndingService = new FakeLineEndingService("converted content");
        var encodingService = new FakeEncodingService(new byte[] { 9, 8, 7 });
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(fileService, encodingService, lineEndingService);

        var document = CreateFileDocument(content: "original content");
        document.UpdateContent("edited content");
        Assert.True(document.IsModified);

        useCase.Execute(document);

        // ConvertFromLf: CurrentContent + Document.LineEnding
        Assert.Equal("edited content", lineEndingService.LastConvertText);
        Assert.Equal(LineEnding.CrLf, lineEndingService.LastConvertLineEnding);

        // Encode: 変換後コンテンツ + Document.Encoding
        Assert.Equal("converted content", encodingService.LastEncodeText);
        Assert.Equal(TextEncoding.Utf8, encodingService.LastEncodeEncoding);

        // WriteAllBytes: FilePath + エンコード済みbytes
        Assert.Equal(@"C:\work\sample.txt", fileService.LastWritePath);
        Assert.Equal(new byte[] { 9, 8, 7 }, fileService.LastWriteBytes);

        Assert.False(document.IsModified);
        Assert.False(document.IsSaving);
    }

    [Fact]
    public void Execute_保存処理中はIsSavingがtrueになる()
    {
        var document = CreateFileDocument();
        bool? isSavingDuringWrite = null;
        var fileService = new FakeFileService(onWrite: () => isSavingDuringWrite = document.IsSaving);
        var useCase = CreateUseCase(fileService: fileService);

        useCase.Execute(document);

        Assert.True(isSavingDuringWrite);
        Assert.False(document.IsSaving);
    }

    [Fact]
    public void Execute_IsModifiedがfalseのDocumentでも_保存処理が実行される()
    {
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(fileService: fileService);
        var document = CreateFileDocument();

        useCase.Execute(document);

        Assert.True(fileService.WriteAllBytesCalled);
        Assert.False(document.IsModified);
    }

    // 失敗系

    [Fact]
    public void Execute_WriteAllBytesで例外が発生した場合_例外が伝播しIsModifiedとIsSavingが維持される()
    {
        var fileService = new FakeFileService(exception: new FileOperationException("書き込み失敗"));
        var useCase = CreateUseCase(fileService: fileService);
        var document = CreateFileDocument();
        document.UpdateContent("edited");

        Assert.Throws<FileOperationException>(() => useCase.Execute(document));

        Assert.True(document.IsModified);
        Assert.False(document.IsSaving);
    }

    [Fact]
    public void Execute_Encodeで例外が発生した場合_例外が伝播し書き込みは実行されない()
    {
        var fileService = new FakeFileService();
        var encodingService = new FakeEncodingService(exception: new EncodingOperationException("エンコード失敗"));
        var useCase = CreateUseCase(fileService, encodingService);
        var document = CreateFileDocument();
        document.UpdateContent("edited");

        Assert.Throws<EncodingOperationException>(() => useCase.Execute(document));

        Assert.False(fileService.WriteAllBytesCalled);
        Assert.True(document.IsModified);
        Assert.False(document.IsSaving);
    }

    [Fact]
    public void Execute_ConvertFromLfで例外が発生した場合_例外が伝播しエンコードと書き込みは実行されない()
    {
        var fileService = new FakeFileService();
        var encodingService = new FakeEncodingService();
        var lineEndingService = new FakeLineEndingService(exception: new InvalidOperationException("変換失敗"));
        var useCase = CreateUseCase(fileService, encodingService, lineEndingService);
        var document = CreateFileDocument();
        document.UpdateContent("edited");

        Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Null(encodingService.LastEncodeText);
        Assert.False(fileService.WriteAllBytesCalled);
        Assert.True(document.IsModified);
        Assert.False(document.IsSaving);
    }

    // テストデータ作成ヘルパー

    private static SaveDocumentUseCase CreateUseCase(
        FakeFileService? fileService = null,
        FakeEncodingService? encodingService = null,
        FakeLineEndingService? lineEndingService = null)
    {
        return new SaveDocumentUseCase(
            fileService ?? new FakeFileService(),
            encodingService ?? new FakeEncodingService(),
            lineEndingService ?? new FakeLineEndingService());
    }

    private static Document CreateFileDocument(string content = "content")
    {
        return new Document(
            @"C:\work\sample.txt",
            "sample.txt",
            content,
            TextEncoding.Utf8,
            LineEnding.CrLf,
            isReadOnly: false);
    }

    private sealed class FakeFileService : IFileService
    {
        private readonly Exception? _exception;
        private readonly Action? _onWrite;

        public bool WriteAllBytesCalled { get; private set; }
        public string? LastWritePath { get; private set; }
        public byte[]? LastWriteBytes { get; private set; }

        public FakeFileService(Exception? exception = null, Action? onWrite = null)
        {
            _exception = exception;
            _onWrite = onWrite;
        }

        public FileReadResult ReadAllBytes(string filePath)
        {
            return new FileReadResult(Array.Empty<byte>(), false);
        }

        public void WriteAllBytes(string filePath, byte[] bytes)
        {
            WriteAllBytesCalled = true;
            LastWritePath = filePath;
            LastWriteBytes = bytes;
            _onWrite?.Invoke();
            if (_exception != null)
                throw _exception;
        }
    }

    private sealed class FakeEncodingService : IEncodingService
    {
        private readonly byte[] _encodeResult;
        private readonly Exception? _exception;

        public string? LastEncodeText { get; private set; }
        public TextEncoding LastEncodeEncoding { get; private set; }

        public FakeEncodingService(byte[]? encodeResult = null, Exception? exception = null)
        {
            _encodeResult = encodeResult ?? Array.Empty<byte>();
            _exception = exception;
        }

        public DecodedText Decode(byte[] bytes)
        {
            return new DecodedText(string.Empty, TextEncoding.Utf8);
        }

        public byte[] Encode(string text, TextEncoding encoding)
        {
            LastEncodeText = text;
            LastEncodeEncoding = encoding;
            if (_exception != null)
                throw _exception;
            return _encodeResult;
        }
    }

    private sealed class FakeLineEndingService : ILineEndingService
    {
        private readonly string _convertResult;
        private readonly Exception? _exception;

        public string? LastConvertText { get; private set; }
        public LineEnding LastConvertLineEnding { get; private set; }

        public FakeLineEndingService(string convertResult = "", Exception? exception = null)
        {
            _convertResult = convertResult;
            _exception = exception;
        }

        public LineEnding Detect(string text)
        {
            return LineEnding.CrLf;
        }

        public string NormalizeToLf(string text)
        {
            return text;
        }

        public string ConvertFromLf(string text, LineEnding lineEnding)
        {
            LastConvertText = text;
            LastConvertLineEnding = lineEnding;
            if (_exception != null)
                throw _exception;
            return _convertResult;
        }
    }
}
