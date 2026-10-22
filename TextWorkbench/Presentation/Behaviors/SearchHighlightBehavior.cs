using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows.Media;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.ViewModels;

namespace TextWorkbench.Presentation.Behaviors;

/// <summary>
/// AvalonEdit上の検索結果ハイライトと現在の検索結果の選択表示を担当します。
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SearchHighlightBehavior
{
    private TextEditor? _textEditor;
    private DocumentTabViewModel? _viewModel;
    private readonly SearchResultBackgroundRenderer _backgroundRenderer = new();

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

            DetachTextEditor();
            _textEditor = value;
            AttachTextEditor();
            UpdateHighlights();
        }
    }

    /// <summary>
    /// 検索状態を保持するDocumentTabViewModelを取得または設定します。
    /// </summary>
    public DocumentTabViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel == value)
            {
                return;
            }

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = value;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }

            UpdateHighlights();
        }
    }

    private void AttachTextEditor()
    {
        if (_textEditor == null)
        {
            return;
        }

        _textEditor.TextChanged += OnTextChanged;
        _textEditor.TextArea.TextView.BackgroundRenderers.Add(_backgroundRenderer);
    }

    private void DetachTextEditor()
    {
        if (_textEditor == null)
        {
            return;
        }

        _textEditor.TextChanged -= OnTextChanged;
        _textEditor.TextArea.TextView.BackgroundRenderers.Remove(_backgroundRenderer);
        _backgroundRenderer.SetResults(Array.Empty<SearchResult>());
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DocumentTabViewModel.SearchResults)
            or nameof(DocumentTabViewModel.CurrentSearchIndex))
        {
            UpdateHighlights();
        }
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        // Document編集時の検索結果無効化はDocumentTabViewModelが担当します。
        // ここでは表示を更新し、古い位置情報を描画しないようにします。
        UpdateHighlights();
    }

    private void UpdateHighlights()
    {
        if (_textEditor == null)
        {
            return;
        }

        var results = _viewModel?.SearchResults ?? Array.Empty<SearchResult>();
        _backgroundRenderer.SetResults(results);
        _textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);

        var currentIndex = _viewModel?.CurrentSearchIndex ?? -1;
        if (currentIndex < 0 || currentIndex >= results.Count)
        {
            return;
        }

        var currentResult = results[currentIndex];
        if (!IsValidRange(currentResult.StartIndex, currentResult.Length))
        {
            return;
        }

        // Current resultは全件ハイライトとは別に、AvalonEditのSelectionで強調します。
        _textEditor.Select(currentResult.StartIndex, currentResult.Length);

        // ScrollToは行・列を受け取るため、文字オフセットをDocumentLocationへ変換します。
        var location = _textEditor.Document.GetLocation(currentResult.StartIndex);
        _textEditor.ScrollTo(location.Line, location.Column);
    }

    private bool IsValidRange(int startIndex, int length)
    {
        if (_textEditor?.Document == null)
        {
            return false;
        }

        var documentLength = _textEditor.Document.TextLength;
        return startIndex >= 0
            && length >= 0
            && startIndex <= documentLength
            && length <= documentLength - startIndex;
    }

    private sealed class SearchResultBackgroundRenderer : IBackgroundRenderer
    {
        private IReadOnlyList<SearchResult> _results = Array.Empty<SearchResult>();

        private static readonly Brush SearchBrush =
            CreateFrozenBrush(Color.FromArgb(100, 255, 220, 80));

        public KnownLayer Layer => KnownLayer.Background;

        public void SetResults(IReadOnlyList<SearchResult> results)
        {
            _results = results;
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_results.Count == 0 || !textView.VisualLinesValid)
            {
                return;
            }

            var document = textView.Document;
            if (document == null)
            {
                return;
            }

            foreach (var result in _results)
            {
                if (result.StartIndex < 0
                    || result.Length <= 0
                    || result.StartIndex >= document.TextLength
                    || result.Length > document.TextLength - result.StartIndex)
                {
                    continue;
                }

                var segment = new TextSegment
                {
                    StartOffset = result.StartIndex,
                    Length = result.Length
                };

                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
                {
                    drawingContext.DrawRectangle(SearchBrush, null, rect);
                }
            }
        }

        private static Brush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
