using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Tests.Application.UseCases;

/// <summary>
/// <see cref="SaveDocumentAsUseCase"/> の単体テストです。
/// </summary>
public class SaveDocumentAsUseCaseTests
{
    // コンストラクタ

    [Fact]
    public void コンストラクタ_fileDialogServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new SaveDocumentAsUseCase(
            null!, new FakeDocumentPathRegistry(), new FakeFileService(), new FakeEncodingService(), new FakeLineEndingService()));
    }

    [Fact]
    public void コンストラクタ_documentPathRegistryがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new SaveDocumentAsUseCase(
            new FakeFileDialogService(null), null!, new FakeFileService(), new FakeEncodingService(), new FakeLineEndingService()));
    }

    [Fact]
    public void コンストラクタ_fileServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new SaveDocumentAsUseCase(
            new FakeFileDialogService(null), new FakeDocumentPathRegistry(), null!, new FakeEncodingService(), new FakeLineEndingService()));
    }

    [Fact]
    public void コンストラクタ_encodingServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new SaveDocumentAsUseCase(
            new FakeFileDialogService(null), new FakeDocumentPathRegistry(), new FakeFileService(), null!, new FakeLineEndingService()));
    }

    [Fact]
    public void コンストラクタ_lineEndingServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new SaveDocumentAsUseCase(
            new FakeFileDialogService(null), new FakeDocumentPathRegistry(), new FakeFileService(), new FakeEncodingService(), null!));
    }

    // ガード

    [Fact]
    public void Execute_documentがnullの場合_ArgumentNullExceptionが発生する()
    {
        var useCase = CreateUseCase();

        Assert.Throws<ArgumentNullException>(() => useCase.Execute(null!));
    }

    [Fact]
    public void Execute_ダイアログでキャンセルした場合_falseが返り書き込まれない()
    {
        var fileService = new FakeFileService();
        var pathRegistry = new FakeDocumentPathRegistry();
        var useCase = CreateUseCase(
            new FakeFileDialogService(null),
            pathRegistry,
            fileService);
        var document = CreateFileDocument();

        var result = useCase.Execute(document);

        Assert.False(result);
        Assert.False(pathRegistry.IsPathInUseCalled);
        Assert.False(fileService.WriteAllBytesCalled);
    }

    // 正常系

    [Fact]
    public void Execute_正常系でDocumentの状態が更新される()
    {
        var newPath = @"C:\work\new.txt";
        var dialogService = new FakeFileDialogService(newPath);
        var pathRegistry = new FakeDocumentPathRegistry();
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(dialogService, pathRegistry, fileService);
        var document = CreateFileDocument();
        document.UpdateContent("edited");
        Assert.True(document.IsModified);

        var result = useCase.Execute(document);

        Assert.True(result);

        // ShowSaveFileDialogに現在のFilePathが初期値として渡る
        Assert.Equal(@"C:\work\sample.txt", dialogService.LastSaveInitialPath);

        // 重複チェックに正規化パスと自Document.Idが渡る
        Assert.Equal(Path.GetFullPath(newPath), pathRegistry.LastCheckedPath);
        Assert.Equal(document.Id, pathRegistry.LastExcludedDocumentId);

        // WriteAllBytesに新パスとエンコード済みbytesが渡る
        Assert.Equal(Path.GetFullPath(newPath), fileService.LastWritePath);
        Assert.NotNull(fileService.LastWriteBytes);

        // Document状態更新
        Assert.Equal(Path.GetFullPath(newPath), document.FilePath);
        Assert.Equal("new.txt", document.FileName);
        Assert.False(document.IsReadOnly);
        Assert.False(document.IsModified);
        Assert.False(document.IsSaving);
    }

    [Fact]
    public void Execute_保存処理中はIsSavingがtrueになる()
    {
        var document = CreateFileDocument();
        bool? isSavingDuringWrite = null;
        var fileService = new FakeFileService(onWrite: () => isSavingDuringWrite = document.IsSaving);
        var useCase = CreateUseCase(
            new FakeFileDialogService(@"C:\work\new.txt"),
            new FakeDocumentPathRegistry(),
            fileService);

        useCase.Execute(document);

        Assert.True(isSavingDuringWrite);
        Assert.False(document.IsSaving);
    }

    [Fact]
    public void Execute_相対パスが正規化されて重複チェックと書き込みに使われる()
    {
        var pathRegistry = new FakeDocumentPathRegistry();
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(
            new FakeFileDialogService("new.txt"),
            pathRegistry,
            fileService);

        useCase.Execute(CreateFileDocument());

        var expected = Path.GetFullPath("new.txt");
        Assert.Equal(expected, pathRegistry.LastCheckedPath);
        Assert.Equal(expected, fileService.LastWritePath);
    }

    [Fact]
    public void Execute_無題DocumentでもSaveAsが成功しFilePathが設定される()
    {
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(
            new FakeFileDialogService(@"C:\work\new.txt"),
            new FakeDocumentPathRegistry(),
            fileService);
        var document = new Document();

        var result = useCase.Execute(document);

        Assert.True(result);
        Assert.Equal(Path.GetFullPath(@"C:\work\new.txt"), document.FilePath);
        Assert.Equal("new.txt", document.FileName);
        Assert.False(document.IsModified);
    }

    // 重複チェック

    [Fact]
    public void Execute_パスが重複している場合_ValidationOperationExceptionが発生し書き込まれない()
    {
        var fileService = new FakeFileService();
        var pathRegistry = new FakeDocumentPathRegistry(isPathInUse: true);
        var useCase = CreateUseCase(
            new FakeFileDialogService(@"C:\work\new.txt"),
            pathRegistry,
            fileService);
        var document = CreateFileDocument();
        document.UpdateContent("edited");

        var ex = Assert.Throws<ValidationOperationException>(() => useCase.Execute(document));

        Assert.Equal("指定されたファイルは別のDocumentとして既に開かれています。", ex.Message);
        Assert.False(fileService.WriteAllBytesCalled);
        Assert.True(document.IsModified);
        Assert.False(document.IsSaving);
    }

    // 失敗系

    [Fact]
    public void Execute_WriteAllBytesで例外が発生した場合_例外が伝播しFilePathとIsModifiedが維持される()
    {
        var fileService = new FakeFileService(exception: new FileOperationException("書き込み失敗"));
        var useCase = CreateUseCase(
            new FakeFileDialogService(@"C:\work\new.txt"),
            new FakeDocumentPathRegistry(),
            fileService);
        var document = CreateFileDocument();
        document.UpdateContent("edited");

        Assert.Throws<FileOperationException>(() => useCase.Execute(document));

        Assert.Equal(@"C:\work\sample.txt", document.FilePath);
        Assert.Equal("sample.txt", document.FileName);
        Assert.True(document.IsModified);
        Assert.False(document.IsSaving);
    }

    [Fact]
    public void Execute_Encodeで例外が発生した場合_例外が伝播し書き込みは実行されない()
    {
        var fileService = new FakeFileService();
        var encodingService = new FakeEncodingService(exception: new EncodingOperationException("エンコード失敗"));
        var useCase = CreateUseCase(
            new FakeFileDialogService(@"C:\work\new.txt"),
            new FakeDocumentPathRegistry(),
            fileService,
            encodingService);
        var document = CreateFileDocument();
        document.UpdateContent("edited");

        Assert.Throws<EncodingOperationException>(() => useCase.Execute(document));

        Assert.False(fileService.WriteAllBytesCalled);
        Assert.Equal(@"C:\work\sample.txt", document.FilePath);
        Assert.True(document.IsModified);
        Assert.False(document.IsSaving);
    }

    // テストデータ作成ヘルパー

    private static SaveDocumentAsUseCase CreateUseCase(
        IFileDialogService? fileDialogService = null,
        IDocumentPathRegistry? pathRegistry = null,
        FakeFileService? fileService = null,
        FakeEncodingService? encodingService = null,
        FakeLineEndingService? lineEndingService = null)
    {
        return new SaveDocumentAsUseCase(
            fileDialogService ?? new FakeFileDialogService(null),
            pathRegistry ?? new FakeDocumentPathRegistry(),
            fileService ?? new FakeFileService(),
            encodingService ?? new FakeEncodingService(),
            lineEndingService ?? new FakeLineEndingService());
    }

    private static Document CreateFileDocument()
    {
        return new Document(
            @"C:\work\sample.txt",
            "sample.txt",
            "content",
            TextEncoding.Utf8,
            LineEnding.CrLf,
            isReadOnly: false);
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        private readonly string? _saveResult;

        public string? LastSaveInitialPath { get; private set; }

        public FakeFileDialogService(string? saveResult)
        {
            _saveResult = saveResult;
        }

        public string? ShowOpenFileDialog() => null;

        public string? ShowSaveFileDialog(string? initialFilePath)
        {
            LastSaveInitialPath = initialFilePath;
            return _saveResult;
        }
    }

    private sealed class FakeDocumentPathRegistry : IDocumentPathRegistry
    {
        private readonly bool _isPathInUse;

        public bool IsPathInUseCalled { get; private set; }
        public string? LastCheckedPath { get; private set; }
        public Guid LastExcludedDocumentId { get; private set; }

        public FakeDocumentPathRegistry(bool isPathInUse = false)
        {
            _isPathInUse = isPathInUse;
        }

        public bool IsPathInUse(string filePath, Guid excludingDocumentId)
        {
            IsPathInUseCalled = true;
            LastCheckedPath = filePath;
            LastExcludedDocumentId = excludingDocumentId;
            return _isPathInUse;
        }
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
        private readonly Exception? _exception;

        public FakeEncodingService(Exception? exception = null)
        {
            _exception = exception;
        }

        public DecodedText Decode(byte[] bytes)
        {
            return new DecodedText(string.Empty, TextEncoding.Utf8);
        }

        public byte[] Encode(string text, TextEncoding encoding)
        {
            if (_exception != null)
                throw _exception;
            return new byte[] { 1, 2, 3 };
        }
    }

    private sealed class FakeLineEndingService : ILineEndingService
    {
        public LineEnding Detect(string text) => LineEnding.CrLf;

        public string NormalizeToLf(string text) => text;

        public string ConvertFromLf(string text, LineEnding lineEnding) => text;
    }
}
