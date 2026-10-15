using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
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

    private void InitializeBehaviors(DocumentTabViewModel viewModel)
    {
        var textEditor = FindName("TextEditor") as TextEditor;
        if (textEditor == null)
        {
            return;
        }

        // Behaviorが未作成の場合は作成
        if (_documentBehavior == null)
        {
            _documentBehavior = new TextEditorDocumentBehavior(_lineEndingService);
            _documentBehavior.TextEditor = textEditor;
            viewModel.SetDocumentBehavior(_documentBehavior);
        }
        else
        {
            // 既存の場合はDocumentのみ更新
            _documentBehavior.Document = viewModel.Document;
        }

        if (_editorStateBehavior == null)
        {
            _editorStateBehavior = new EditorStateBehavior();
            _editorStateBehavior.TextEditor = textEditor;
            viewModel.SetEditorStateBehavior(_editorStateBehavior);
        }
        else
        {
            // 既存の場合はDocumentのみ更新
            _editorStateBehavior.Document = viewModel.Document;
        }
    }
}
