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
        _backgroundRenderer.SetResults(Array.Empty<HighlightRange>());
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

    /// <summary>
    /// 検索結果をエディタのオフセット体系へ変換し、
    /// ハイライトと現在の検索結果の選択で共有します。
    /// </summary>
    private void UpdateHighlights()
    {
        if (_textEditor == null)
        {
            return;
        }

        var results = _viewModel?.SearchResults ?? Array.Empty<SearchResult>();

        // 検索結果を一度だけ変換し、全件ハイライトと選択で共有します。
        var ranges = new HighlightRange?[results.Count];

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];

            if (!TryConvertRange(
                    result.StartIndex,
                    result.Length,
                    out var startOffset,
                    out var length)
                || !IsValidRange(startOffset, length))
            {
                ranges[i] = null;
                continue;
            }

            ranges[i] = new HighlightRange(startOffset, length);
        }

        var validRanges = ranges
            .OfType<HighlightRange>()
            .ToArray();

        _backgroundRenderer.SetResults(validRanges);
        _textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);

        var currentIndex = _viewModel?.CurrentSearchIndex ?? -1;

        if (currentIndex < 0 || currentIndex >= ranges.Length)
        {
            return;
        }

        var currentRange = ranges[currentIndex];

        if (currentRange == null)
        {
            return;
        }

        // 全件ハイライトと同じ変換済み範囲を選択に使用します。
        _textEditor.Select(
            currentRange.StartOffset,
            currentRange.Length);

        // ScrollToは行・列を受け取るため、変換後の位置を使用します。
        var location = _textEditor.Document.GetLocation(
            currentRange.StartOffset);

        _textEditor.ScrollTo(location.Line, location.Column);
    }

    /// <summary>
    /// LF基準のオフセットをエディタのテキスト位置へ変換します。
    /// CRLFはLF基準では1文字、CRLF基準では2文字として扱います。
    /// </summary>
    private int ConvertLfOffsetToEditorOffset(int lfOffset)
    {
        if (_textEditor == null || lfOffset < 0)
        {
            return -1;
        }

        if (lfOffset == 0)
        {
            return 0;
        }

        var editorText = _textEditor.Text;
        var logicalOffset = 0;
        var editorOffset = 0;

        while (editorOffset < editorText.Length
            && logicalOffset < lfOffset)
        {
            if (editorText[editorOffset] == '\r'
                && editorOffset + 1 < editorText.Length
                && editorText[editorOffset + 1] == '\n')
            {
                // CRLFは2文字消費し、論理上の位置は1つ進めます。
                editorOffset += 2;
            }
            else
            {
                editorOffset++;
            }

            logicalOffset++;
        }

        // 指定された位置まで到達できなければ無効な位置です。
        return logicalOffset == lfOffset
            ? editorOffset
            : -1;
    }

    /// <summary>
    /// 検索結果の開始位置と終了位置を変換し、
    /// 変換後の範囲を取得します。
    /// </summary>
    private bool TryConvertRange(
        int startIndex,
        int length,
        out int convertedStart,
        out int convertedLength)
    {
        convertedStart = 0;
        convertedLength = 0;

        if (startIndex < 0 || length < 0)
        {
            return false;
        }

        var convertedEnd = ConvertLfOffsetToEditorOffset(
            startIndex + length);

        convertedStart = ConvertLfOffsetToEditorOffset(startIndex);

        if (convertedStart < 0
            || convertedEnd < convertedStart)
        {
            return false;
        }

        convertedLength = convertedEnd - convertedStart;
        return true;
    }

    /// <summary>
    /// 変換後の範囲がAvalonEditのDocument内に収まるか確認します。
    /// </summary>
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

    /// <summary>
    /// エディタ上で表示する変換済みの検索範囲です。
    /// </summary>
    private sealed record HighlightRange(
        int StartOffset,
        int Length);

    /// <summary>
    /// 検索結果の背景ハイライトを描画します。
    /// このクラスには変換済みの範囲を渡します。
    /// </summary>
    private sealed class SearchResultBackgroundRenderer : IBackgroundRenderer
    {
        private IReadOnlyList<HighlightRange> _ranges =
            Array.Empty<HighlightRange>();

        private static readonly Brush SearchBrush =
            CreateFrozenBrush(Color.FromArgb(100, 255, 220, 80));

        public KnownLayer Layer => KnownLayer.Background;

        public void SetResults(IReadOnlyList<HighlightRange> ranges)
        {
            _ranges = ranges;
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_ranges.Count == 0 || !textView.VisualLinesValid)
            {
                return;
            }

            var document = textView.Document;
            if (document == null)
            {
                return;
            }

            foreach (var range in _ranges)
            {
                if (range.StartOffset < 0
                    || range.Length <= 0
                    || range.StartOffset >= document.TextLength
                    || range.Length > document.TextLength - range.StartOffset)
                {
                    continue;
                }

                var segment = new TextSegment
                {
                    StartOffset = range.StartOffset,
                    Length = range.Length
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
