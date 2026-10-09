using TextWorkbench.Domain.Enums;

namespace TextWorkbench.Domain.Models;

/// <summary>
/// 1つのテキストファイル、または未保存のテキスト文書を表します。
/// </summary>
public sealed class Document
{
    /// <summary>
    /// Documentを一意に識別するIDを取得します。
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// 正規化済み絶対パスを取得します。新規未保存Documentでは null。
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// ファイル名を取得します。
    /// </summary>
    public string? FileName { get; }

    /// <summary>
    /// 現在編集中のテキストを取得します。内部では改行コードをLFに統一します。
    /// </summary>
    public string CurrentContent { get; private set; }

    /// <summary>
    /// 文字コードを取得します。
    /// </summary>
    public TextEncoding Encoding { get; }

    /// <summary>
    /// 保存時に使用する改行形式を取得します。
    /// </summary>
    public LineEnding LineEnding { get; }

    /// <summary>
    /// 保存後から編集が行われたかどうかを取得または設定します。
    /// </summary>
    public bool IsModified { get; private set; }

    /// <summary>
    /// ファイルが読み取り専用として扱われるかどうかを取得または設定します。
    /// </summary>
    public bool IsReadOnly { get; private set; }

    /// <summary>
    /// Documentの読み込み処理中かどうかを取得または設定します。
    /// </summary>
    public bool IsLoading { get; private set; }

    /// <summary>
    /// Documentの保存処理中かどうかを取得または設定します。
    /// </summary>
    public bool IsSaving { get; private set; }

    /// <summary>
    /// Documentが編集可能かどうかを取得します。
    /// </summary>
    public bool IsEditable => !IsReadOnly && !IsLoading && !IsSaving;

    /// <summary>
    /// 新規未保存Document用のインスタンスを初期化します。
    /// </summary>
    public Document()
    {
        Id = Guid.NewGuid();
        FilePath = null;
        FileName = null;
        CurrentContent = string.Empty;
        Encoding = TextEncoding.Utf8;
        LineEnding = LineEnding.CrLf;
        IsModified = false;
        IsReadOnly = false;
        IsLoading = false;
        IsSaving = false;
    }

    /// <summary>
    /// 既存ファイルからDocumentを作成するためのインスタンスを初期化します。
    /// </summary>
    /// <param name="filePath">正規化済み絶対パス</param>
    /// <param name="fileName">ファイル名</param>
    /// <param name="content">テキスト内容（LF正規化済み）</param>
    /// <param name="encoding">文字コード</param>
    /// <param name="lineEnding">改行コード</param>
    /// <param name="isReadOnly">読み取り専用かどうか</param>
    public Document(
        string filePath,
        string fileName,
        string content,
        TextEncoding encoding,
        LineEnding lineEnding,
        bool isReadOnly)
    {
        Id = Guid.NewGuid();
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        CurrentContent = content ?? throw new ArgumentNullException(nameof(content));
        Encoding = encoding;
        LineEnding = lineEnding;
        IsModified = false;
        IsReadOnly = isReadOnly;
        IsLoading = false;
        IsSaving = false;
    }

    /// <summary>
    /// テキスト内容を更新します。
    /// </summary>
    /// <param name="newContent">新しいテキスト内容（LF正規化済み）</param>
    public void UpdateContent(string newContent)
    {
        CurrentContent = newContent ?? throw new ArgumentNullException(nameof(newContent));
        IsModified = true;
    }

    /// <summary>
    /// 保存完了状態を設定します。
    /// </summary>
    public void MarkAsSaved()
    {
        IsModified = false;
    }

    /// <summary>
    /// 読み込み処理の開始を設定します。
    /// </summary>
    public void StartLoading()
    {
        IsLoading = true;
    }

    /// <summary>
    /// 読み込み処理の完了を設定します。
    /// </summary>
    public void EndLoading()
    {
        IsLoading = false;
    }

    /// <summary>
    /// 保存処理の開始を設定します。
    /// </summary>
    public void StartSaving()
    {
        IsSaving = true;
    }

    /// <summary>
    /// 保存処理の完了を設定します。
    /// </summary>
    public void EndSaving()
    {
        IsSaving = false;
    }

    /// <summary>
    /// 読み取り専用状態を設定します。
    /// </summary>
    /// <param name="isReadOnly">読み取り専用かどうか</param>
    public void SetReadOnly(bool isReadOnly)
    {
        IsReadOnly = isReadOnly;
    }
}
