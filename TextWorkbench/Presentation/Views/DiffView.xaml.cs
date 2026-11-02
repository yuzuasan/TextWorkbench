using System.Windows;
using System.Windows.Controls;
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
        if (e.NewValue is DiffTabViewModel diffTabViewModel)
        {
            SideBySideViewer.DiffModel = CreateDiffModel(diffTabViewModel.DiffSession.Result);
            return;
        }

        SideBySideViewer.DiffModel = null;
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
}
