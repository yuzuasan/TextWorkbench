using ICSharpCode.AvalonEdit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Infrastructure.LineEnding;
using TextWorkbench.Presentation.Behaviors;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Presentation.Views;

/// <summary>
/// DocumentView.xaml の相互作用ロジック
/// </summary>
public partial class DocumentView : UserControl
{
    private TextEditorDocumentBehavior? _documentBehavior;
    private EditorStateBehavior? _editorStateBehavior;
    private readonly ILineEndingService _lineEndingService;

    private TextEditor? _textEditor;
    private bool _isCaretPositionHandlerAttached;

    public DocumentView()
    {
        InitializeComponent();
        _lineEndingService = new LineEndingService();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is DocumentTabViewModel viewModel)
        {
            InitializeBehaviors(viewModel);
        }
        else
        {
            // DataContextがDocumentTabViewModelでなくなった場合、Behaviorをクリア
            if (_documentBehavior != null)
            {
                _documentBehavior.Document = null;
            }

            if (_editorStateBehavior != null)
            {
                _editorStateBehavior.Document = null;
            }
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DocumentTabViewModel viewModel)
        {
            InitializeBehaviors(viewModel);
        }
    }

    /// <summary>
    /// エディタにフォーカスを設定します。
    /// </summary>
    public void FocusEditor()
    {
        if (FindName("TextEditor") is not TextEditor textEditor)
        {
            return;
        }

        if (!IsLoaded || !IsVisible || !textEditor.IsVisible)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() =>
            {
                // Dispatcher実行時にも表示状態を確認します。
                if (!IsLoaded || !IsVisible || !textEditor.IsVisible)
                {
                    return;
                }

                textEditor.Focus();
                Keyboard.Focus(textEditor);
            }));
    }

    private void InitializeBehaviors(DocumentTabViewModel viewModel)
    {
        var textEditor = FindName("TextEditor") as TextEditor;
        if (textEditor == null)
        {
            return;
        }

        _textEditor = textEditor;

        // Caretイベントは同じTextEditorに対して一度だけ購読する。
        if (!_isCaretPositionHandlerAttached)
        {
            textEditor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
            _isCaretPositionHandlerAttached = true;
        }

        // Behaviorが未作成の場合は作成
        if (_documentBehavior == null)
        {
            _documentBehavior = new TextEditorDocumentBehavior(_lineEndingService);
            _documentBehavior.TextEditor = textEditor;
        }

        // DocumentViewが別のDocumentTabViewModelで再利用された場合も、
        // 現在のViewModelへ変更通知コールバックを再接続する。
        viewModel.SetDocumentBehavior(_documentBehavior);

        if (_editorStateBehavior == null)
        {
            _editorStateBehavior = new EditorStateBehavior();
            _editorStateBehavior.TextEditor = textEditor;
        }

        // 現在のViewModelとEditorStateBehaviorの関連付けも更新する。
        viewModel.SetEditorStateBehavior(_editorStateBehavior);

        // タブ切替時にも、現在のEditor位置を新しいViewModelへ渡す。
        UpdateCaretPosition(viewModel, textEditor);
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (_textEditor == null || DataContext is not DocumentTabViewModel viewModel)
        {
            return;
        }

        UpdateCaretPosition(viewModel, _textEditor);
    }

    private static void UpdateCaretPosition(
        DocumentTabViewModel viewModel,
        TextEditor textEditor)
    {
        var position = textEditor.TextArea.Caret.Position;
        viewModel.UpdateCaretPosition(position.Line, position.Column);
    }
}
