using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
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

    public DocumentView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is DocumentTabViewModel viewModel)
        {
            InitializeBehaviors(viewModel);
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

        _documentBehavior = new TextEditorDocumentBehavior();
        _documentBehavior.TextEditor = textEditor;
        viewModel.SetDocumentBehavior(_documentBehavior);

        _editorStateBehavior = new EditorStateBehavior();
        _editorStateBehavior.TextEditor = textEditor;
        viewModel.SetEditorStateBehavior(_editorStateBehavior);
    }
}
