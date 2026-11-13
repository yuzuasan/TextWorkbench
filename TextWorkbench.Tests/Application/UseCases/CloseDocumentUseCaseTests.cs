using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Tests.Application.UseCases;

/// <summary>
/// <see cref="CloseDocumentUseCase"/> の単体テストです。
/// </summary>
public class CloseDocumentUseCaseTests
{
    // コンストラクタ

    [Fact]
    public void コンストラクタ_confirmationServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new CloseDocumentUseCase(
            null!, CreateSaveUseCase(), CreateSaveAsUseCase()));
    }

    [Fact]
    public void コンストラクタ_saveDocumentUseCaseがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new CloseDocumentUseCase(
            new FakeUserConfirmationService(SaveConfirmationResult.Save), null!, CreateSaveAsUseCase()));
    }

    [Fact]
    public void コンストラクタ_saveDocumentAsUseCaseがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new CloseDocumentUseCase(
            new FakeUserConfirmationService(SaveConfirmationResult.Save), CreateSaveUseCase(), null!));
    }

    // ガード

    [Fact]
    public void Execute_documentがnullの場合_ArgumentNullExceptionが発生する()
    {
        var useCase = CreateUseCase();

        Assert.Throws<ArgumentNullException>(() => useCase.Execute(null!, "title"));
    }

    [Fact]
    public void Execute_displayTitleがnullの場合_ArgumentNullExceptionが発生する()
    {
        var useCase = CreateUseCase();

        Assert.Throws<ArgumentNullException>(() => useCase.Execute(CreateDocument(), null!));
    }

    // 未変更Document

    [Fact]
    public void Execute_変更されていないDocumentの場合_確認せずにtrueが返る()
    {
        var confirmation = new FakeUserConfirmationService(SaveConfirmationResult.Cancel);
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(confirmation, fileService: fileService);

        var result = useCase.Execute(CreateDocument(), "sample.txt");

        Assert.True(result);
        Assert.False(confirmation.Called);
        Assert.False(fileService.WriteAllBytesCalled);
    }

    // Save分岐

    [Fact]
    public void Execute_変更ありでSave選択かつFilePathありの場合_保存されてtrueが返る()
    {
        var confirmation = new FakeUserConfirmationService(SaveConfirmationResult.Save);
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(confirmation, fileService: fileService);
        var document = CreateModifiedDocument();

        var result = useCase.Execute(document, "sample.txt");

        Assert.True(result);
        Assert.Equal("sample.txt", confirmation.LastTitle);
        Assert.True(fileService.WriteAllBytesCalled);
        Assert.False(document.IsModified);
    }

    [Fact]
    public void Execute_変更ありでSave選択かつFilePathなしの場合_SaveAsが実行される()
    {
        var confirmation = new FakeUserConfirmationService(SaveConfirmationResult.Save);
        var dialogService = new FakeFileDialogService(@"C:\work\new.txt");
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(
            confirmation,
            fileService: fileService,
            fileDialogService: dialogService);
        var document = CreateModifiedUntitledDocument();

        var result = useCase.Execute(document, "無題1");

        Assert.True(result);
        Assert.True(fileService.WriteAllBytesCalled);
        Assert.Equal(Path.GetFullPath(@"C:\work\new.txt"), document.FilePath);
        Assert.False(document.IsModified);
    }

    [Fact]
    public void Execute_変更ありでSave選択かつSaveAsキャンセルの場合_falseが返り状態が維持される()
    {
        var confirmation = new FakeUserConfirmationService(SaveConfirmationResult.Save);
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(
            confirmation,
            fileService: fileService,
            fileDialogService: new FakeFileDialogService(null));
        var document = CreateModifiedUntitledDocument();

        var result = useCase.Execute(document, "無題1");

        Assert.False(result);
        Assert.False(fileService.WriteAllBytesCalled);
        Assert.True(document.IsModified);
    }

    // DontSave / Cancel分岐

    [Fact]
    public void Execute_DontSave選択の場合_書き込まずtrueが返る()
    {
        var confirmation = new FakeUserConfirmationService(SaveConfirmationResult.DontSave);
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(confirmation, fileService: fileService);
        var document = CreateModifiedDocument();

        var result = useCase.Execute(document, "sample.txt");

        Assert.True(result);
        Assert.False(fileService.WriteAllBytesCalled);
        Assert.True(document.IsModified);
    }

    [Fact]
    public void Execute_Cancel選択の場合_書き込まずfalseが返る()
    {
        var confirmation = new FakeUserConfirmationService(SaveConfirmationResult.Cancel);
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(confirmation, fileService: fileService);
        var document = CreateModifiedDocument();

        var result = useCase.Execute(document, "sample.txt");

        Assert.False(result);
        Assert.False(fileService.WriteAllBytesCalled);
        Assert.True(document.IsModified);
    }

    // 失敗系

    [Fact]
    public void Execute_保存に失敗した場合_例外が伝播しIsModifiedが維持される()
    {
        var confirmation = new FakeUserConfirmationService(SaveConfirmationResult.Save);
        var fileService = new FakeFileService(exception: new FileOperationException("書き込み失敗"));
        var useCase = CreateUseCase(confirmation, fileService: fileService);
        var document = CreateModifiedDocument();

        Assert.Throws<FileOperationException>(() => useCase.Execute(document, "sample.txt"));

        Assert.True(document.IsModified);
    }

    // テストデータ作成ヘルパー

    private static CloseDocumentUseCase CreateUseCase(
        FakeUserConfirmationService? confirmation = null,
        FakeFileService? fileService = null,
        FakeFileDialogService? fileDialogService = null)
    {
        fileService ??= new FakeFileService();
        var encodingService = new FakeEncodingService();
        var lineEndingService = new FakeLineEndingService();

        var saveUseCase = new SaveDocumentUseCase(fileService, encodingService, lineEndingService);
        var saveAsUseCase = new SaveDocumentAsUseCase(
            fileDialogService ?? new FakeFileDialogService(null),
            new FakeDocumentPathRegistry(),
            fileService,
            encodingService,
            lineEndingService);

        return new CloseDocumentUseCase(
            confirmation ?? new FakeUserConfirmationService(SaveConfirmationResult.Save),
            saveUseCase,
            saveAsUseCase);
    }

    private static SaveDocumentUseCase CreateSaveUseCase()
    {
        return new SaveDocumentUseCase(
            new FakeFileService(),
            new FakeEncodingService(),
            new FakeLineEndingService());
    }

    private static SaveDocumentAsUseCase CreateSaveAsUseCase()
    {
        return new SaveDocumentAsUseCase(
            new FakeFileDialogService(null),
            new FakeDocumentPathRegistry(),
            new FakeFileService(),
            new FakeEncodingService(),
            new FakeLineEndingService());
    }

    private static Document CreateDocument()
    {
        return new Document(
            @"C:\work\sample.txt",
            "sample.txt",
            "content",
            TextEncoding.Utf8,
            LineEnding.CrLf,
            isReadOnly: false);
    }

    private static Document CreateModifiedDocument()
    {
        var document = CreateDocument();
        document.UpdateContent("edited");
        return document;
    }

    private static Document CreateModifiedUntitledDocument()
    {
        var document = new Document();
        document.UpdateContent("edited");
        return document;
    }

    private sealed class FakeUserConfirmationService : IUserConfirmationService
    {
        private readonly SaveConfirmationResult _result;

        public bool Called { get; private set; }
        public string? LastTitle { get; private set; }

        public FakeUserConfirmationService(SaveConfirmationResult result)
        {
            _result = result;
        }

        public SaveConfirmationResult ConfirmSaveChanges(string documentTitle)
        {
            Called = true;
            LastTitle = documentTitle;
            return _result;
        }
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        private readonly string? _saveResult;

        public FakeFileDialogService(string? saveResult)
        {
            _saveResult = saveResult;
        }

        public string? ShowOpenFileDialog() => null;

        public string? ShowSaveFileDialog(string? initialFilePath) => _saveResult;
    }

    private sealed class FakeDocumentPathRegistry : IDocumentPathRegistry
    {
        public bool IsPathInUse(string filePath, Guid excludingDocumentId) => false;
    }

    private sealed class FakeFileService : IFileService
    {
        private readonly Exception? _exception;

        public bool WriteAllBytesCalled { get; private set; }

        public FakeFileService(Exception? exception = null)
        {
            _exception = exception;
        }

        public FileReadResult ReadAllBytes(string filePath)
        {
            return new FileReadResult(Array.Empty<byte>(), false);
        }

        public void WriteAllBytes(string filePath, byte[] bytes)
        {
            WriteAllBytesCalled = true;
            if (_exception != null)
                throw _exception;
        }
    }

    private sealed class FakeEncodingService : IEncodingService
    {
        public DecodedText Decode(byte[] bytes)
        {
            return new DecodedText(string.Empty, TextEncoding.Utf8);
        }

        public byte[] Encode(string text, TextEncoding encoding)
        {
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
