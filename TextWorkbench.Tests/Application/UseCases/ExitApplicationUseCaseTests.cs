using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Tests.Application.UseCases;

/// <summary>
/// <see cref="ExitApplicationUseCase"/> の単体テストです。
/// </summary>
public class ExitApplicationUseCaseTests
{
    // コンストラクタ

    [Fact]
    public void コンストラクタ_openDocumentProviderがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new ExitApplicationUseCase(
            null!, new FakeDocumentActivationService(), new FakeExitConfirmationService(ExitConfirmationResult.SaveAndExit),
            new FakeUserNotificationService(), CreateSaveUseCase(), CreateSaveAsUseCase()));
    }

    [Fact]
    public void コンストラクタ_documentActivationServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new ExitApplicationUseCase(
            new FakeOpenDocumentProvider(Array.Empty<OpenDocumentInfo>()), null!, new FakeExitConfirmationService(ExitConfirmationResult.SaveAndExit),
            new FakeUserNotificationService(), CreateSaveUseCase(), CreateSaveAsUseCase()));
    }

    [Fact]
    public void コンストラクタ_exitConfirmationServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new ExitApplicationUseCase(
            new FakeOpenDocumentProvider(Array.Empty<OpenDocumentInfo>()), new FakeDocumentActivationService(), null!,
            new FakeUserNotificationService(), CreateSaveUseCase(), CreateSaveAsUseCase()));
    }

    [Fact]
    public void コンストラクタ_notificationServiceがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new ExitApplicationUseCase(
            new FakeOpenDocumentProvider(Array.Empty<OpenDocumentInfo>()), new FakeDocumentActivationService(), new FakeExitConfirmationService(ExitConfirmationResult.SaveAndExit),
            null!, CreateSaveUseCase(), CreateSaveAsUseCase()));
    }

    [Fact]
    public void コンストラクタ_saveDocumentUseCaseがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new ExitApplicationUseCase(
            new FakeOpenDocumentProvider(Array.Empty<OpenDocumentInfo>()), new FakeDocumentActivationService(), new FakeExitConfirmationService(ExitConfirmationResult.SaveAndExit),
            new FakeUserNotificationService(), null!, CreateSaveAsUseCase()));
    }

    [Fact]
    public void コンストラクタ_saveDocumentAsUseCaseがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() => new ExitApplicationUseCase(
            new FakeOpenDocumentProvider(Array.Empty<OpenDocumentInfo>()), new FakeDocumentActivationService(), new FakeExitConfirmationService(ExitConfirmationResult.SaveAndExit),
            new FakeUserNotificationService(), CreateSaveUseCase(), null!));
    }

    // 未保存Documentなし

    [Fact]
    public void Execute_未保存Documentがない場合_確認せずCanExitがtrueになる()
    {
        var provider = new FakeOpenDocumentProvider(new[]
        {
            new OpenDocumentInfo(CreateUnmodifiedDocument(), "sample.txt"),
        });
        var confirmation = new FakeExitConfirmationService(ExitConfirmationResult.SaveAndExit);
        var useCase = CreateUseCase(provider, confirmation);

        var result = useCase.Execute();

        Assert.True(result.CanExit);
        Assert.Empty(result.SavedDocumentIds);
        Assert.Empty(result.SavedAsDocumentIds);
        Assert.False(confirmation.Called);
    }

    // 未保存Documentのフィルタ

    [Fact]
    public void Execute_未保存と保存済みが混在する場合_未保存のみが確認対象になる()
    {
        var modified = CreateModifiedDocument(@"C:\work\a.txt", "a.txt");
        var unmodified = CreateUnmodifiedDocument(@"C:\work\b.txt", "b.txt");
        var provider = new FakeOpenDocumentProvider(new[]
        {
            new OpenDocumentInfo(modified, "a.txt"),
            new OpenDocumentInfo(unmodified, "b.txt"),
        });
        var confirmation = new FakeExitConfirmationService(ExitConfirmationResult.ExitWithoutSaving);
        var useCase = CreateUseCase(provider, confirmation);

        useCase.Execute();

        Assert.Equal(new[] { "a.txt" }, confirmation.LastDocumentNames);
    }

    // 終了方針

    [Fact]
    public void Execute_ExitWithoutSaving選択の場合_保存せずCanExitがtrueになる()
    {
        var document = CreateModifiedDocument(@"C:\work\a.txt", "a.txt");
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(
            new FakeOpenDocumentProvider(new[] { new OpenDocumentInfo(document, "a.txt") }),
            new FakeExitConfirmationService(ExitConfirmationResult.ExitWithoutSaving),
            fileService: fileService);

        var result = useCase.Execute();

        Assert.True(result.CanExit);
        Assert.Empty(result.SavedDocumentIds);
        Assert.Equal(0, fileService.WriteCount);
        Assert.True(document.IsModified);
    }

    [Fact]
    public void Execute_Cancel選択の場合_CanExitがfalseになり状態が維持される()
    {
        var document = CreateModifiedDocument(@"C:\work\a.txt", "a.txt");
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(
            new FakeOpenDocumentProvider(new[] { new OpenDocumentInfo(document, "a.txt") }),
            new FakeExitConfirmationService(ExitConfirmationResult.Cancel),
            fileService: fileService);

        var result = useCase.Execute();

        Assert.False(result.CanExit);
        Assert.Empty(result.SavedDocumentIds);
        Assert.Equal(0, fileService.WriteCount);
        Assert.True(document.IsModified);
    }

    [Fact]
    public void Execute_未知の確認結果の場合_CanExitがfalseになる()
    {
        var document = CreateModifiedDocument(@"C:\work\a.txt", "a.txt");
        var useCase = CreateUseCase(
            new FakeOpenDocumentProvider(new[] { new OpenDocumentInfo(document, "a.txt") }),
            new FakeExitConfirmationService((ExitConfirmationResult)999));

        var result = useCase.Execute();

        Assert.False(result.CanExit);
    }

    // SaveAndExit：FilePathあり

    [Fact]
    public void Execute_SaveAndExitでFilePathありDocumentが全件保存される()
    {
        var doc1 = CreateModifiedDocument(@"C:\work\a.txt", "a.txt");
        var doc2 = CreateModifiedDocument(@"C:\work\b.txt", "b.txt");
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(
            new FakeOpenDocumentProvider(new[]
            {
                new OpenDocumentInfo(doc1, "a.txt"),
                new OpenDocumentInfo(doc2, "b.txt"),
            }),
            new FakeExitConfirmationService(ExitConfirmationResult.SaveAndExit),
            fileService: fileService);

        var result = useCase.Execute();

        Assert.True(result.CanExit);
        Assert.Equal(new[] { doc1.Id, doc2.Id }, result.SavedDocumentIds);
        Assert.Empty(result.SavedAsDocumentIds);
        Assert.Equal(2, fileService.WriteCount);
        Assert.False(doc1.IsModified);
        Assert.False(doc2.IsModified);
    }

    // SaveAndExit：無題Document

    [Fact]
    public void Execute_SaveAndExitで無題DocumentはActivate後にSaveAsされる()
    {
        var untitled = CreateModifiedUntitledDocument();
        var activation = new FakeDocumentActivationService();
        var dialogService = new FakeFileDialogService(@"C:\work\new.txt");
        var fileService = new FakeFileService();
        var useCase = CreateUseCase(
            new FakeOpenDocumentProvider(new[] { new OpenDocumentInfo(untitled, "無題1") }),
            new FakeExitConfirmationService(ExitConfirmationResult.SaveAndExit),
            activation: activation,
            fileService: fileService,
            fileDialogService: dialogService);

        var result = useCase.Execute();

        Assert.True(result.CanExit);
        Assert.Equal(new[] { untitled.Id }, activation.ActivatedIds);
        Assert.Equal(new[] { untitled.Id }, result.SavedDocumentIds);
        Assert.Equal(new[] { untitled.Id }, result.SavedAsDocumentIds);
        Assert.False(untitled.IsModified);
    }

    // 部分失敗：SaveAsキャンセル

    [Fact]
    public void Execute_SaveAsキャンセルの場合_CanExitがfalseで先行保存済みIDが結果に含まれる()
    {
        var doc1 = CreateModifiedDocument(@"C:\work\a.txt", "a.txt");
        var untitled = CreateModifiedUntitledDocument();
        var useCase = CreateUseCase(
            new FakeOpenDocumentProvider(new[]
            {
                new OpenDocumentInfo(doc1, "a.txt"),
                new OpenDocumentInfo(untitled, "無題1"),
            }),
            new FakeExitConfirmationService(ExitConfirmationResult.SaveAndExit),
            fileService: new FakeFileService(),
            fileDialogService: new FakeFileDialogService(null));

        var result = useCase.Execute();

        Assert.False(result.CanExit);
        Assert.Equal(new[] { doc1.Id }, result.SavedDocumentIds);
        Assert.Empty(result.SavedAsDocumentIds);
        Assert.False(doc1.IsModified);
        Assert.True(untitled.IsModified);
    }

    // 部分失敗：保存エラー

    [Fact]
    public void Execute_保存失敗の場合_ShowErrorが呼ばれ後続Documentは処理されない()
    {
        var doc1 = CreateModifiedDocument(@"C:\work\a.txt", "a.txt");
        var doc2 = CreateModifiedDocument(@"C:\work\b.txt", "b.txt");
        var doc3 = CreateModifiedDocument(@"C:\work\c.txt", "c.txt");
        var fileService = new FakeFileService(failOnCallNumber: 2);
        var notification = new FakeUserNotificationService();
        var useCase = CreateUseCase(
            new FakeOpenDocumentProvider(new[]
            {
                new OpenDocumentInfo(doc1, "a.txt"),
                new OpenDocumentInfo(doc2, "b.txt"),
                new OpenDocumentInfo(doc3, "c.txt"),
            }),
            new FakeExitConfirmationService(ExitConfirmationResult.SaveAndExit),
            notification: notification,
            fileService: fileService);

        var result = useCase.Execute();

        Assert.False(result.CanExit);
        Assert.Equal(new[] { doc1.Id }, result.SavedDocumentIds);
        Assert.Single(notification.Errors);
        Assert.Contains("b.txt", notification.Errors[0]);
        Assert.False(doc1.IsModified);
        Assert.True(doc2.IsModified);
        Assert.True(doc3.IsModified);
        Assert.Equal(2, fileService.WriteCount);
    }

    // テストデータ作成ヘルパー

    private static ExitApplicationUseCase CreateUseCase(
        IOpenDocumentProvider provider,
        IExitConfirmationService confirmation,
        FakeDocumentActivationService? activation = null,
        FakeUserNotificationService? notification = null,
        FakeFileService? fileService = null,
        FakeFileDialogService? fileDialogService = null)
    {
        fileService ??= new FakeFileService();
        var encodingService = new FakeEncodingService();
        var lineEndingService = new FakeLineEndingService();

        var saveUseCase = new SaveDocumentUseCase(fileService, encodingService, lineEndingService);
        var saveAsUseCase = new SaveDocumentAsUseCase(
            fileDialogService ?? new FakeFileDialogService(@"C:\work\saved.txt"),
            new FakeDocumentPathRegistry(),
            fileService,
            encodingService,
            lineEndingService);

        return new ExitApplicationUseCase(
            provider,
            activation ?? new FakeDocumentActivationService(),
            confirmation,
            notification ?? new FakeUserNotificationService(),
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

    private static Document CreateUnmodifiedDocument(string? filePath = null, string? fileName = null)
    {
        if (filePath == null)
            return new Document();

        return new Document(filePath, fileName!, "content", TextEncoding.Utf8, LineEnding.CrLf, false);
    }

    private static Document CreateModifiedDocument(string filePath, string fileName)
    {
        var document = new Document(filePath, fileName, "content", TextEncoding.Utf8, LineEnding.CrLf, false);
        document.UpdateContent("edited");
        return document;
    }

    private static Document CreateModifiedUntitledDocument()
    {
        var document = new Document();
        document.UpdateContent("edited");
        return document;
    }

    private sealed class FakeOpenDocumentProvider : IOpenDocumentProvider
    {
        private readonly IReadOnlyList<OpenDocumentInfo> _documents;

        public FakeOpenDocumentProvider(IReadOnlyList<OpenDocumentInfo> documents)
        {
            _documents = documents;
        }

        public IReadOnlyList<OpenDocumentInfo> GetOpenDocuments() => _documents;
    }

    private sealed class FakeDocumentActivationService : IDocumentActivationService
    {
        public List<Guid> ActivatedIds { get; } = new();

        public void ActivateDocument(Guid documentId)
        {
            ActivatedIds.Add(documentId);
        }
    }

    private sealed class FakeExitConfirmationService : IExitConfirmationService
    {
        private readonly ExitConfirmationResult _result;

        public bool Called { get; private set; }
        public IReadOnlyList<string>? LastDocumentNames { get; private set; }

        public FakeExitConfirmationService(ExitConfirmationResult result)
        {
            _result = result;
        }

        public ExitConfirmationResult ConfirmExit(IReadOnlyList<string> documentNames)
        {
            Called = true;
            LastDocumentNames = documentNames;
            return _result;
        }
    }

    private sealed class FakeUserNotificationService : IUserNotificationService
    {
        public List<string> Errors { get; } = new();

        public void ShowInfo(string message)
        {
        }

        public void ShowError(string message)
        {
            Errors.Add(message);
        }

        public void ShowWarning(string message)
        {
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
        private readonly int? _failOnCallNumber;
        private int _callCount;

        public int WriteCount { get; private set; }

        public FakeFileService(int? failOnCallNumber = null)
        {
            _failOnCallNumber = failOnCallNumber;
        }

        public FileReadResult ReadAllBytes(string filePath)
        {
            return new FileReadResult(Array.Empty<byte>(), false);
        }

        public void WriteAllBytes(string filePath, byte[] bytes)
        {
            WriteCount++;
            _callCount++;
            if (_failOnCallNumber == _callCount)
                throw new FileOperationException("書き込み失敗");
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
