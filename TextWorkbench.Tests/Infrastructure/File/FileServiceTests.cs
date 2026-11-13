using TextWorkbench.Application.Exceptions;
using TextWorkbench.Infrastructure.File;

namespace TextWorkbench.Tests.Infrastructure.File;

/// <summary>
/// <see cref="FileService"/> の単体テストです。
/// </summary>
public class FileServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public FileServiceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "TextWorkbenchTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        // ReadOnly属性を解除してから削除
        foreach (var file in Directory.EnumerateFiles(_tempDirectory, "*", SearchOption.AllDirectories))
        {
            System.IO.File.SetAttributes(file, FileAttributes.Normal);
        }
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    // ReadAllBytes

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ReadAllBytes_パスがnullまたは空の場合_ArgumentExceptionが発生する(string? path)
    {
        var service = new FileService();

        Assert.Throws<ArgumentException>(() => service.ReadAllBytes(path!));
    }

    [Fact]
    public void ReadAllBytes_ファイル内容とReadOnly状態が取得できる()
    {
        var path = CreateFile("test.txt", new byte[] { 1, 2, 3 });
        var service = new FileService();

        var result = service.ReadAllBytes(path);

        Assert.Equal(new byte[] { 1, 2, 3 }, result.Content);
        Assert.False(result.IsReadOnly);
    }

    [Fact]
    public void ReadAllBytes_ReadOnly属性のファイルはIsReadOnlyがtrueになる()
    {
        var path = CreateFile("readonly.txt", new byte[] { 1 });
        System.IO.File.SetAttributes(path, FileAttributes.ReadOnly);
        var service = new FileService();

        var result = service.ReadAllBytes(path);

        Assert.True(result.IsReadOnly);
    }

    [Fact]
    public void ReadAllBytes_存在しないファイルの場合_FileOperationExceptionが発生する()
    {
        var service = new FileService();
        var missingPath = Path.Combine(_tempDirectory, "missing.txt");

        var ex = Assert.Throws<FileOperationException>(() => service.ReadAllBytes(missingPath));
        Assert.Equal("ファイルを開けませんでした。", ex.Message);
    }

    [Fact]
    public void ReadAllBytes_ディレクトリパスの場合_FileOperationExceptionが発生する()
    {
        var service = new FileService();

        Assert.Throws<FileOperationException>(() => service.ReadAllBytes(_tempDirectory));
    }

    // WriteAllBytes

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WriteAllBytes_パスがnullまたは空の場合_ArgumentExceptionが発生する(string? path)
    {
        var service = new FileService();

        Assert.Throws<ArgumentException>(() => service.WriteAllBytes(path!, new byte[] { 1 }));
    }

    [Fact]
    public void WriteAllBytes_bytesがnullの場合_ArgumentNullExceptionが発生する()
    {
        var service = new FileService();
        var path = Path.Combine(_tempDirectory, "test.txt");

        Assert.Throws<ArgumentNullException>(() => service.WriteAllBytes(path, null!));
    }

    [Fact]
    public void WriteAllBytes_新規ファイルに書き込める()
    {
        var path = Path.Combine(_tempDirectory, "new.txt");
        var bytes = new byte[] { 10, 20, 30, 40 };
        var service = new FileService();

        service.WriteAllBytes(path, bytes);

        Assert.Equal(bytes, System.IO.File.ReadAllBytes(path));
    }

    [Fact]
    public void WriteAllBytes_既存ファイルが上書きされる()
    {
        var path = CreateFile("existing.txt", new byte[] { 1, 2, 3 });
        var service = new FileService();

        service.WriteAllBytes(path, new byte[] { 9, 9 });

        Assert.Equal(new byte[] { 9, 9 }, System.IO.File.ReadAllBytes(path));
    }

    [Fact]
    public void WriteAllBytes_存在しないディレクトリ配下の場合_FileOperationExceptionが発生する()
    {
        var service = new FileService();
        var path = Path.Combine(_tempDirectory, "no-such-dir", "file.txt");

        var ex = Assert.Throws<FileOperationException>(() => service.WriteAllBytes(path, new byte[] { 1 }));
        Assert.Equal("ファイルを保存できませんでした。", ex.Message);
    }

    [Fact]
    public void WriteAllBytes_ReadOnlyファイルへの書き込みは失敗する()
    {
        var path = CreateFile("readonly-write.txt", new byte[] { 1 });
        System.IO.File.SetAttributes(path, FileAttributes.ReadOnly);
        var service = new FileService();

        var ex = Assert.Throws<FileOperationException>(() => service.WriteAllBytes(path, new byte[] { 2 }));
        Assert.Equal("ファイルへのアクセスが拒否されました。", ex.Message);
        // 内容が変更されていないこと
        Assert.Equal(new byte[] { 1 }, System.IO.File.ReadAllBytes(path));
    }

    // テストデータ作成ヘルパー

    private string CreateFile(string fileName, byte[] content)
    {
        var path = Path.Combine(_tempDirectory, fileName);
        System.IO.File.WriteAllBytes(path, content);
        return path;
    }
}
