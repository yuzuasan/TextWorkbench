using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Tests.Domain.Models;

/// <summary>
/// <see cref="Document"/> の単体テストです。
/// </summary>
public class DocumentTests
{
    // コンストラクタ（無題Document）

    [Fact]
    public void パラメータなしコンストラクタ_無題Documentの初期値が設定される()
    {
        var document = new Document();

        Assert.NotEqual(Guid.Empty, document.Id);
        Assert.Null(document.FilePath);
        Assert.Null(document.FileName);
        Assert.Equal(string.Empty, document.CurrentContent);
        Assert.Equal(TextEncoding.Utf8, document.Encoding);
        Assert.Equal(LineEnding.CrLf, document.LineEnding);
    }

    [Fact]
    public void パラメータなしコンストラクタ_状態フラグがすべてfalseで初期化される()
    {
        var document = new Document();

        Assert.False(document.IsModified);
        Assert.False(document.IsReadOnly);
        Assert.False(document.IsLoading);
        Assert.False(document.IsSaving);
        Assert.True(document.IsEditable);
    }

    [Fact]
    public void パラメータなしコンストラクタ_Idが一意に生成される()
    {
        var document1 = new Document();
        var document2 = new Document();

        Assert.NotEqual(document1.Id, document2.Id);
    }

    // コンストラクタ（既存ファイルDocument）

    [Fact]
    public void コンストラクタ_引数の値がプロパティに設定される()
    {
        var document = new Document(
            @"C:\work\sample.txt",
            "sample.txt",
            "hello",
            TextEncoding.ShiftJis,
            LineEnding.Lf,
            isReadOnly: true);

        Assert.NotEqual(Guid.Empty, document.Id);
        Assert.Equal(@"C:\work\sample.txt", document.FilePath);
        Assert.Equal("sample.txt", document.FileName);
        Assert.Equal("hello", document.CurrentContent);
        Assert.Equal(TextEncoding.ShiftJis, document.Encoding);
        Assert.Equal(LineEnding.Lf, document.LineEnding);
        Assert.True(document.IsReadOnly);
        Assert.False(document.IsModified);
        Assert.False(document.IsEditable);
    }

    [Fact]
    public void コンストラクタ_filePathがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Document(null!, "sample.txt", "content", TextEncoding.Utf8, LineEnding.CrLf, false));
    }

    [Fact]
    public void コンストラクタ_fileNameがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Document(@"C:\work\sample.txt", null!, "content", TextEncoding.Utf8, LineEnding.CrLf, false));
    }

    [Fact]
    public void コンストラクタ_contentがnullの場合_ArgumentNullExceptionが発生する()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Document(@"C:\work\sample.txt", "sample.txt", null!, TextEncoding.Utf8, LineEnding.CrLf, false));
    }

    // UpdateContent

    [Fact]
    public void UpdateContent_nullの場合_ArgumentNullExceptionが発生する()
    {
        var document = new Document();

        Assert.Throws<ArgumentNullException>(() => document.UpdateContent(null!));
    }

    [Fact]
    public void UpdateContent_内容が異なる場合_内容が更新されIsModifiedがtrueになる()
    {
        var document = new Document();

        document.UpdateContent("new content");

        Assert.Equal("new content", document.CurrentContent);
        Assert.True(document.IsModified);
    }

    [Fact]
    public void UpdateContent_内容が同一の場合_IsModifiedが変わらない()
    {
        var document = new Document();

        document.UpdateContent(string.Empty);

        Assert.Equal(string.Empty, document.CurrentContent);
        Assert.False(document.IsModified);
    }

    [Fact]
    public void UpdateContent_変更済みで内容が同一の場合_IsModifiedがtrueのまま維持される()
    {
        var document = new Document();
        document.UpdateContent("changed");
        Assert.True(document.IsModified);

        document.UpdateContent("changed");

        Assert.True(document.IsModified);
    }

    // MarkAsSaved

    [Fact]
    public void MarkAsSaved_変更後に呼び出すと_IsModifiedがfalseになる()
    {
        var document = new Document();
        document.UpdateContent("changed");
        Assert.True(document.IsModified);

        document.MarkAsSaved();

        Assert.False(document.IsModified);
    }

    // IsLoading / IsSaving

    [Fact]
    public void 読み込み状態_StartLoadingとEndLoadingでフラグが遷移しIsEditableが連動する()
    {
        var document = new Document();

        document.StartLoading();
        Assert.True(document.IsLoading);
        Assert.False(document.IsEditable);

        document.EndLoading();
        Assert.False(document.IsLoading);
        Assert.True(document.IsEditable);
    }

    [Fact]
    public void 保存状態_StartSavingとEndSavingでフラグが遷移しIsEditableが連動する()
    {
        var document = new Document();

        document.StartSaving();
        Assert.True(document.IsSaving);
        Assert.False(document.IsEditable);

        document.EndSaving();
        Assert.False(document.IsSaving);
        Assert.True(document.IsEditable);
    }

    // IsReadOnly / SetReadOnly

    [Fact]
    public void SetReadOnly_trueを設定すると_IsReadOnlyがtrueになりIsEditableがfalseになる()
    {
        var document = new Document();

        document.SetReadOnly(true);

        Assert.True(document.IsReadOnly);
        Assert.False(document.IsEditable);
    }

    [Fact]
    public void SetReadOnly_falseを設定すると_IsEditableが復帰する()
    {
        var document = new Document();
        document.SetReadOnly(true);

        document.SetReadOnly(false);

        Assert.False(document.IsReadOnly);
        Assert.True(document.IsEditable);
    }

    // IsEditable の組み合わせ

    [Fact]
    public void IsEditable_読み取り専用かつ読み込み中_EndLoading後もfalseのまま()
    {
        var document = new Document();
        document.SetReadOnly(true);
        document.StartLoading();

        Assert.False(document.IsEditable);

        document.EndLoading();
        Assert.False(document.IsEditable);
    }

    [Fact]
    public void IsEditable_読み取り専用かつ保存中_EndSaving後もfalseのまま()
    {
        var document = new Document();
        document.SetReadOnly(true);
        document.StartSaving();

        Assert.False(document.IsEditable);

        document.EndSaving();
        Assert.False(document.IsEditable);
    }

    // UpdateFilePath

    [Fact]
    public void UpdateFilePath_newFilePathがnullの場合_ArgumentNullExceptionが発生する()
    {
        var document = new Document();

        Assert.Throws<ArgumentNullException>(() =>
            document.UpdateFilePath(null!, "new.txt", false));
    }

    [Fact]
    public void UpdateFilePath_newFileNameがnullの場合_ArgumentNullExceptionが発生する()
    {
        var document = new Document();

        Assert.Throws<ArgumentNullException>(() =>
            document.UpdateFilePath(@"C:\work\new.txt", null!, false));
    }

    [Fact]
    public void UpdateFilePath_正常な値で_FilePathとFileNameとIsReadOnlyが更新される()
    {
        var document = new Document();

        document.UpdateFilePath(@"C:\work\new.txt", "new.txt", newReadOnly: true);

        Assert.Equal(@"C:\work\new.txt", document.FilePath);
        Assert.Equal("new.txt", document.FileName);
        Assert.True(document.IsReadOnly);
        Assert.False(document.IsEditable);
    }

    [Fact]
    public void UpdateFilePath_呼び出しても_IsModifiedは変化しない()
    {
        var document = new Document();
        document.UpdateContent("changed");
        Assert.True(document.IsModified);

        document.UpdateFilePath(@"C:\work\new.txt", "new.txt", false);

        Assert.True(document.IsModified);
    }
}
