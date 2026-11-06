using ICSharpCode.AvalonEdit;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.Models;

namespace TextWorkbench.Presentation.Behaviors;

/// <summary>
/// TextEditorとDocumentのテキスト同期を担当するBehaviorです。
/// </summary>
public class TextEditorDocumentBehavior
{
    private TextEditor? _textEditor;
    private Document? _document;
    private DocumentEditorState? _editorState;
    private bool _isSynchronizing;
    private Action<bool>? _onDocumentContentChanged;
    private readonly ILineEndingService _lineEndingService;

    /// <summary>
    /// <see cref="TextEditorDocumentBehavior"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="lineEndingService">改行コードサービス</param>
    public TextEditorDocumentBehavior(ILineEndingService lineEndingService)
    {
        _lineEndingService = lineEndingService ?? throw new ArgumentNullException(nameof(lineEndingService));
    }

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

    /// <summary>
    /// DocumentとEditorStateを一体として設定します。
    /// </summary>
    /// <param name="document">DomainのDocument</param>
    /// <param name="editorState">タブ専用の編集状態</param>
    public void SetDocumentAndEditorState(Document? document, DocumentEditorState? editorState)
    {
        if (_document == document && _editorState == editorState)
        {
            return;
        }

        _document = document;
        _editorState = editorState;

        // 同じTextDocumentが既に接続されている場合は、不要な再設定を避ける
        if (_textEditor != null && _editorState != null)
        {
            if (_textEditor.Document != _editorState.TextDocument)
            {
                try
                {
                    _isSynchronizing = true;
                    _textEditor.Document = _editorState.TextDocument;
                }
                finally
                {
                    _isSynchronizing = false;
                }
            }
        }
    }

    /// <summary>
    /// Documentの内容変更時に呼び出されるコールバックを設定します。
    /// </summary>
    /// <param name="callback">コールバック（syncFromDocument: DocumentからEditorへ同期するかどうか）</param>
    public void SetOnDocumentContentChanged(Action<bool> callback)
    {
        _onDocumentContentChanged = callback;
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
        var content = _textEditor.Document?.Text ?? string.Empty;
        // 改行コードをLFに正規化
        var normalizedContent = _lineEndingService.NormalizeToLf(content);
        _document.UpdateContent(normalizedContent);
        // ViewModelへ通知してIsModifiedの変更通知を発火（DocumentからEditorへ同期しない）
        _onDocumentContentChanged?.Invoke(false);
    }

    private void SyncDocumentToEditor()
    {
        if (_textEditor == null || _document == null || _editorState == null)
        {
            return;
        }

        try
        {
            _isSynchronizing = true;
            // TextDocument.Textを使用してUndo履歴を保持する
            _editorState.TextDocument.Text = _document.CurrentContent;
        }
        finally
        {
            _isSynchronizing = false;
        }
    }

    /// <summary>
    /// プログラムによる変更時にEditorを同期します。
    /// </summary>
    /// <param name="document">同期元のDocument</param>
    public void SyncFromDocument(Document document)
    {
        // 呼び出し元DocumentがBehaviorのアクティブDocumentと一致するか確認する
        if (_document != document)
        {
            return;
        }

        SyncDocumentToEditor();
    }
}
