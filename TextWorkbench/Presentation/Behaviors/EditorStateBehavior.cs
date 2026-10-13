using ICSharpCode.AvalonEdit;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Presentation.Behaviors;

/// <summary>
/// Documentの編集状態をEditorへ反映するBehaviorです。
/// </summary>
public class EditorStateBehavior
{
    private TextEditor? _textEditor;
    private Document? _document;

    /// <summary>
    /// TextEditorを取得または設定します。
    /// </summary>
    public TextEditor? TextEditor
    {
        get => _textEditor;
        set
        {
            if (_textEditor == value)
            {
                return;
            }

            _textEditor = value;
            UpdateEditorState();
        }
    }

    /// <summary>
    /// Documentを取得または設定します。
    /// </summary>
    public Document? Document
    {
        get => _document;
        set
        {
            if (_document == value)
            {
                return;
            }

            _document = value;
            UpdateEditorState();
        }
    }

    private void UpdateEditorState()
    {
        if (_textEditor == null || _document == null)
        {
            return;
        }

        // Document.IsEditable = !IsReadOnly && !IsLoading && !IsSaving
        // Editorへは反対の状態として反映
        _textEditor.IsReadOnly = !_document.IsEditable;
    }

    /// <summary>
    /// Documentの状態変更時にEditorを更新します。
    /// </summary>
    public void UpdateEditorReadOnly()
    {
        UpdateEditorState();
    }
}
