using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DiffPlex.DiffBuilder.Model;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;
using TextWorkbench.Presentation.ViewModels;
using DiffPlexChangeType = DiffPlex.DiffBuilder.Model.ChangeType;

namespace TextWorkbench.Presentation.Views;

/// <summary>
/// DiffTabViewModelが保持する比較結果を左右ペインで表示します。
/// </summary>
public partial class DiffView : UserControl
{
    /// <summary>
    /// 現在差分の強調表示に使用しているAdornerを保持します。
    /// </summary>
    private readonly List<(AdornerLayer Layer, Adorner Adorner)> _currentDiffAdorners = [];

    public DiffView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// 表示対象のDiffTabが切り替わった場合に表示モデルを更新します。
    /// </summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        RemoveCurrentDiffHighlight();

        if (e.OldValue is DiffTabViewModel oldDiffTab)
        {
            oldDiffTab.PropertyChanged -= OnDiffTabPropertyChanged;
        }

        if (e.NewValue is DiffTabViewModel diffTabViewModel)
        {
            diffTabViewModel.PropertyChanged += OnDiffTabPropertyChanged;
            SideBySideViewer.DiffModel = CreateDiffModel(diffTabViewModel.DiffSession.Result);
            UpdateCurrentDiff(diffTabViewModel);
            return;
        }

        SideBySideViewer.DiffModel = null;
    }

    /// <summary>
    /// DiffTabViewModelのプロパティ変更を監視します。
    /// </summary>
    private void OnDiffTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is DiffTabViewModel diffTabViewModel)
        {
            if (e.PropertyName == nameof(DiffTabViewModel.CurrentDiffIndex))
            {
                UpdateCurrentDiff(diffTabViewModel);
            }
        }
    }

    /// <summary>
    /// 現在の差分へのスクロールと強調表示を更新します。
    /// </summary>
    private void UpdateCurrentDiff(DiffTabViewModel diffTabViewModel)
    {
        // 前回の強調表示を解除します。
        RemoveCurrentDiffHighlight();

        var currentDiffLine = diffTabViewModel.GetCurrentDiffLine();
        if (currentDiffLine == null)
        {
            return;
        }

        // DiffResult内で現在差分に対応する行インデックスを取得します。
        var diffLineIndex = FindDiffLineIndex(
            diffTabViewModel.DiffSession.Result.Lines,
            currentDiffLine);

        if (diffLineIndex < 0)
        {
            return;
        }

        // DiffPlexの表示モデルはDiffResult.Linesと同じ順序で生成しています。
        var leftDiffPiece = SideBySideViewer.OldText?.Lines
            .ElementAtOrDefault(diffLineIndex);

        var rightDiffPiece = SideBySideViewer.NewText?.Lines
            .ElementAtOrDefault(diffLineIndex);

        if (leftDiffPiece == null && rightDiffPiece == null)
        {
            return;
        }

        // DiffPlex.WpfのGoToメソッドを使用して対象行へ移動
        // 左側の行番号を使用（nullの場合は右側を使用）
        var lineNumber = currentDiffLine.LeftLineNumber ?? currentDiffLine.RightLineNumber ?? 0;
        var isLeftLine = currentDiffLine.LeftLineNumber.HasValue;

        SideBySideViewer.GoTo(lineNumber, isLeftLine);

        // 左右両方の対応行を強調表示します。
        // Added / Deletedの場合、片側はImaginary行になりますが、
        // その行も含めて強調します。
        AddCurrentDiffHighlight(leftDiffPiece);
        AddCurrentDiffHighlight(rightDiffPiece);
    }

    /// <summary>
    /// DiffResult内で現在差分に対応する行インデックスを取得します。
    /// </summary>
    private static int FindDiffLineIndex(
        IReadOnlyList<DiffLine> lines,
        DiffLine target)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (ReferenceEquals(lines[i], target))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// DiffPieceに対応する表示要素へCurrent Diff用Adornerを追加します。
    /// </summary>
    private void AddCurrentDiffHighlight(DiffPiece? diffPiece)
    {
        if (diffPiece == null)
        {
            return;
        }

        // DiffPlex.Wpfは表示行のTagにDiffPieceを設定しています。
        var lineElement = FindDiffPieceElement(
            SideBySideViewer,
            diffPiece);

        if (lineElement == null)
        {
            return;
        }

        var adornerLayer = AdornerLayer.GetAdornerLayer(lineElement);

        if (adornerLayer == null)
        {
            return;
        }

        var adorner = new CurrentDiffLineAdorner(lineElement);

        adornerLayer.Add(adorner);

        _currentDiffAdorners.Add((adornerLayer, adorner));
    }

    /// <summary>
    /// Tagに指定したDiffPieceを保持する表示要素をVisual Treeから検索します。
    /// </summary>
    private static FrameworkElement? FindDiffPieceElement(
        DependencyObject parent,
        DiffPiece target)
    {
        if (parent is FrameworkElement element
            && ReferenceEquals(element.Tag, target))
        {
            return element;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(parent);

        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            var result = FindDiffPieceElement(child, target);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// 前回のCurrent Diff強調表示をすべて解除します。
    /// </summary>
    private void RemoveCurrentDiffHighlight()
    {
        foreach (var (layer, adorner) in _currentDiffAdorners)
        {
            layer.Remove(adorner);
        }

        _currentDiffAdorners.Clear();
    }

    /// <summary>
    /// TextWorkbenchのDiffResultをDiffPlex.Wpfの表示用モデルへ変換します。
    /// ここでは差分計算を行わず、既存のDiffSegmentを描画用の型へ写像します。
    /// </summary>
    private static SideBySideDiffModel CreateDiffModel(DiffResult result)
    {
        var model = new SideBySideDiffModel();

        foreach (var line in result.Lines)
        {
            model.OldText.Lines.Add(CreateDiffPiece(line, isLeft: true));
            model.NewText.Lines.Add(CreateDiffPiece(line, isLeft: false));
        }

        return model;
    }

    /// <summary>
    /// 1つのDiffLineを左右いずれかのDiffPlex行モデルへ変換します。
    /// </summary>
    private static DiffPiece CreateDiffPiece(DiffLine line, bool isLeft)
    {
        var lineNumber = isLeft ? line.LeftLineNumber : line.RightLineNumber;
        var segments = isLeft ? line.LeftSegments : line.RightSegments;
        var text = string.Concat(segments.Select(segment => segment.Text));

        var changeType = line.Type switch
        {
            DiffLineType.Unchanged => DiffPlexChangeType.Unchanged,
            DiffLineType.Added when isLeft => DiffPlexChangeType.Imaginary,
            DiffLineType.Added => DiffPlexChangeType.Inserted,
            DiffLineType.Deleted when isLeft => DiffPlexChangeType.Deleted,
            DiffLineType.Deleted => DiffPlexChangeType.Imaginary,
            DiffLineType.Changed => DiffPlexChangeType.Modified,
            _ => throw new ArgumentOutOfRangeException(nameof(line.Type), line.Type, null)
        };

        var piece = new DiffPiece(text, changeType, lineNumber);

        // Changed行は行全体をModifiedとし、文字単位の差分をSubPiecesへ反映します。
        if (line.Type == DiffLineType.Changed)
        {
            foreach (var segment in segments)
            {
                piece.SubPieces.Add(new DiffPiece(
                    segment.Text,
                    ToDiffPlexChangeType(segment.Type)));
            }
        }

        return piece;
    }

    /// <summary>
    /// TextWorkbenchの文字セグメント種別をDiffPlexの文字変更種別へ変換します。
    /// </summary>
    private static DiffPlexChangeType ToDiffPlexChangeType(DiffSegmentType segmentType)
    {
        return segmentType switch
        {
            DiffSegmentType.Unchanged => DiffPlexChangeType.Unchanged,
            DiffSegmentType.Added => DiffPlexChangeType.Inserted,
            DiffSegmentType.Deleted => DiffPlexChangeType.Deleted,
            _ => throw new ArgumentOutOfRangeException(nameof(segmentType), segmentType, null)
        };
    }

    /// <summary>
    /// 現在選択されている差分行を通常の差分色とは別に強調するAdornerです。
    /// </summary>
    private sealed class CurrentDiffLineAdorner : Adorner
    {
        private readonly Brush _backgroundBrush;
        private readonly Pen _borderPen;

        public CurrentDiffLineAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            // 半透明の黄色背景
            _backgroundBrush = new SolidColorBrush(
                Color.FromArgb(64, 255, 193, 7));

            // オレンジ色の枠線
            var borderBrush = new SolidColorBrush(
                Color.FromRgb(255, 160, 0));

            _backgroundBrush.Freeze();
            borderBrush.Freeze();

            _borderPen = new Pen(borderBrush, 1.5);
            _borderPen.Freeze();

            // Adornerがマウス操作を妨げないようにします。
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var bounds = new Rect(RenderSize)
            {
                Width = Math.Max(0, RenderSize.Width - 1),
                Height = Math.Max(0, RenderSize.Height - 1)
            };

            drawingContext.DrawRectangle(
                _backgroundBrush,
                _borderPen,
                bounds);
        }
    }
}
