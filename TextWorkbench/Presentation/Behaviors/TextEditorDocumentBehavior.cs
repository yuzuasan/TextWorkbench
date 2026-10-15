using ICSharpCode.AvalonEdit;
using TextWorkbench.Application.Interfaces;
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
        var content = _textEditor.Text;
        // 改行コードをLFに正規化
        var normalizedContent = _lineEndingService.NormalizeToLf(content);
        _document.UpdateContent(normalizedContent);
        // ViewModelへ通知してIsModifiedの変更通知を発火（DocumentからEditorへ同期しない）
        _onDocumentContentChanged?.Invoke(false);
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
