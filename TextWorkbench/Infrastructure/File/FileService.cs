using System.IO;
using TextWorkbench.Application.Exceptions;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Application.Models;

namespace TextWorkbench.Infrastructure.File;

/// <summary>
/// System.IOを使用したファイルサービスの実装です。
/// </summary>
public sealed class FileService : IFileService
{
    /// <summary>
    /// ファイルの全バイトを読み込みます。
    /// </summary>
    /// <param name="filePath">ファイルパス</param>
    /// <returns>読み込み結果</returns>
    public FileReadResult ReadAllBytes(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("ファイルパスを指定してください。", nameof(filePath));

        try
        {
            var content = System.IO.File.ReadAllBytes(filePath);
            var isReadOnly = System.IO.File.GetAttributes(filePath).HasFlag(FileAttributes.ReadOnly);

            return new FileReadResult(content, isReadOnly);
        }
        catch (FileNotFoundException)
        {
            throw new FileOperationException("ファイルを開けませんでした。");
        }
        catch (UnauthorizedAccessException)
        {
            throw new FileOperationException("ファイルへのアクセスが拒否されました。");
        }
        catch (IOException)
        {
            throw new FileOperationException("ファイル操作に失敗しました。");
        }
    }

    /// <summary>
    /// 全バイトをファイルに書き込みます。
    /// </summary>
    /// <param name="filePath">ファイルパス</param>
    /// <param name="bytes">書き込むバイト列</param>
    public void WriteAllBytes(string filePath, byte[] bytes)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("ファイルパスを指定してください。", nameof(filePath));

        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));

        try
        {
            System.IO.File.WriteAllBytes(filePath, bytes);
        }
        catch (UnauthorizedAccessException)
        {
            throw new FileOperationException("ファイルへのアクセスが拒否されました。");
        }
        catch (IOException)
        {
            throw new FileOperationException("ファイルを保存できませんでした。");
        }
    }
}
