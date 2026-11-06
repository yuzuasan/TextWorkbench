using ICSharpCode.AvalonEdit.Document;

namespace TextWorkbench.Presentation.Models;

/// <summary>
/// 1つのDocumentタブに対応するAvalonEditの編集状態を保持します。
/// </summary>
public sealed class DocumentEditorState
{
    /// <summary>
    /// タブ専用の編集文書を取得します。
    /// </summary>
    public TextDocument TextDocument { get; }

    /// <summary>
    /// <see cref="DocumentEditorState"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="initialContent">初期テキスト内容</param>
    public DocumentEditorState(string initialContent)
    {
        TextDocument = new TextDocument(initialContent);
    }
}
