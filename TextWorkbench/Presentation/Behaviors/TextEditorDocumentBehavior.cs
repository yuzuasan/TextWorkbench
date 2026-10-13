using ICSharpCode.AvalonEdit;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Presentation.Behaviors;

/// <summary>
/// TextEditorとDocumentのテキスト同期を担当するBehaviorです。
/// </summary>
public class TextEditorDocumentBehavior
{
    private TextEditor? _textEditor;
    private Document? _document;
    private bool _isSynchronizing;

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

            Detach();
            _textEditor = value;
            Attach();
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
            SyncDocumentToEditor();
        }
    }

    private void Attach()
    {
        if (_textEditor == null)
        {
            return;
        }

        _textEditor.TextChanged += OnTextChanged;
    }

    private void Detach()
    {
        if (_textEditor == null)
        {
            return;
        }

        _textEditor.TextChanged -= OnTextChanged;
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (_isSynchronizing || _textEditor == null || _document == null)
        {
            return;
        }

        // ユーザー編集：Editor → Document
        var content = _textEditor.Text;
        _document.UpdateContent(content);
    }

    private void SyncDocumentToEditor()
    {
        if (_textEditor == null || _document == null)
        {
            return;
        }

        try
        {
            _isSynchronizing = true;
            _textEditor.Text = _document.CurrentContent;
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    /// <summary>
    /// プログラムによる変更時にEditorを同期します。
    /// </summary>
    public void SyncFromDocument()
    {
        SyncDocumentToEditor();
    }
}
