using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using TextWorkbench.Application.Interfaces;
using TextWorkbench.Domain.Enums;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// 比較元選択ダイアログのViewModelです。
/// </summary>
public sealed partial class ComparisonSourceSelectionDialogViewModel : ObservableObject
{
    private readonly IFileDialogService _fileDialogService;

    /// <summary>
    /// 左側の選択状態を取得します。
    /// </summary>
    public SelectionState LeftSelection { get; }

    /// <summary>
    /// 右側の選択状態を取得します。
    /// </summary>
    public SelectionState RightSelection { get; }

    /// <summary>
    /// Document候補リストを取得します。
    /// </summary>
    public IReadOnlyList<DocumentTabViewModel> DocumentCandidates { get; }

    /// <summary>
    /// 比較実行可能かどうかを取得します。
    /// </summary>
    public bool CanExecuteCompare =>
        LeftSelection.HasSelection && RightSelection.HasSelection;

    /// <summary>
    /// 左側の比較元を保持します。
    /// </summary>
    public ComparisonSource? LeftComparisonSource { get; private set; }

    /// <summary>
    /// 右側の比較元を保持します。
    /// </summary>
    public ComparisonSource? RightComparisonSource { get; private set; }

    /// <summary>
    /// ダイアログを閉じるイベントです。
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>
    /// <see cref="ComparisonSourceSelectionDialogViewModel"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="workspace">Workspace</param>
    /// <param name="fileDialogService">ファイルダイアログサービス</param>
    public ComparisonSourceSelectionDialogViewModel(
        Workspace workspace,
        IFileDialogService fileDialogService)
    {
        _fileDialogService = fileDialogService
            ?? throw new ArgumentNullException(nameof(fileDialogService));

        if (workspace == null)
            throw new ArgumentNullException(nameof(workspace));

        // 左右の選択状態を初期化
        LeftSelection = new SelectionState();
        RightSelection = new SelectionState();

        // 選択状態変更時にCommandの実行可能状態を更新
        LeftSelection.PropertyChanged += OnSelectionPropertyChanged;
        RightSelection.PropertyChanged += OnSelectionPropertyChanged;

        // Document候補リストを作成（IsLoading/IsSavingのDocumentを除外）
        DocumentCandidates = workspace.Tabs
            .OfType<DocumentTabViewModel>()
            .Where(tab => !tab.IsLoading && !tab.IsSaving)
            .ToList();
    }

    /// <summary>
    /// 左側の保存済みファイル選択を実行します。
    /// </summary>
    [RelayCommand]
    private void SelectLeftFile()
    {
        SelectFile(LeftSelection);
    }

    /// <summary>
    /// 右側の保存済みファイル選択を実行します。
    /// </summary>
    [RelayCommand]
    private void SelectRightFile()
    {
        SelectFile(RightSelection);
    }

    /// <summary>
    /// 保存済みファイル選択を実行します。
    /// </summary>
    /// <param name="selection">選択状態</param>
    private void SelectFile(SelectionState selection)
    {
        var filePath = _fileDialogService.ShowOpenFileDialog();
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            selection.SetFilePath(filePath);
        }
    }

    /// <summary>
    /// 比較を実行します。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteCompare))]
    private void ExecuteCompare()
    {
        // 左側のComparisonSourceを生成
        LeftComparisonSource = CreateComparisonSource(LeftSelection);

        // 右側のComparisonSourceを生成
        RightComparisonSource = CreateComparisonSource(RightSelection);

        // ダイアログを閉じるイベントを発行
        CloseRequested?.Invoke(this, true);
    }

    /// <summary>
    /// キャンセルを実行します。
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        // ダイアログを閉じるイベントを発行
        CloseRequested?.Invoke(this, false);
    }

    /// <summary>
    /// 選択状態からComparisonSourceを生成します。
    /// </summary>
    /// <param name="selection">選択状態</param>
    /// <returns>ComparisonSource</returns>
    private static ComparisonSource CreateComparisonSource(SelectionState selection)
    {
        if (selection.SelectedDocumentTab != null)
        {
            return new ComparisonSource(
                SourceType.OpenDocument,
                selection.SelectedDocumentTab.Document.Id.ToString());
        }
        else if (selection.SelectedFilePath != null)
        {
            return new ComparisonSource(
                SourceType.File,
                selection.SelectedFilePath);
        }

        throw new InvalidOperationException("選択が行われていません。");
    }

    /// <summary>
    /// 選択状態のプロパティ変更時の処理です。
    /// </summary>
    private void OnSelectionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CanExecuteCompare));
        ExecuteCompareCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 比較元選択状態を表します。
    /// </summary>
    public sealed partial class SelectionState : ObservableObject
    {
        /// <summary>
        /// 選択されたDocumentTabを取得または設定します。
        /// </summary>
        [ObservableProperty]
        private DocumentTabViewModel? _selectedDocumentTab;

        /// <summary>
        /// 選択されたファイルパスを取得または設定します。
        /// </summary>
        [ObservableProperty]
        private string? _selectedFilePath;

        /// <summary>
        /// 選択が行われているかどうかを取得します。
        /// </summary>
        public bool HasSelection => SelectedDocumentTab != null || SelectedFilePath != null;

        /// <summary>
        /// 比較元表示名を取得します。
        /// </summary>
        public string SourceDisplayName =>
            SelectedDocumentTab != null
                ? $"開いているドキュメント：{SelectedDocumentTab.DisplayTitle}"
                : SelectedFilePath != null
                    ? $"保存済みファイル：{Path.GetFileName(SelectedFilePath)}"
                    : string.Empty;

        /// <summary>
        /// ファイルパスを設定し、DocumentTabをクリアします。
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        public void SetFilePath(string filePath)
        {
            SelectedFilePath = filePath;
            SelectedDocumentTab = null;
        }

        /// <summary>
        /// DocumentTabを設定し、ファイルパスをクリアします。
        /// </summary>
        /// <param name="documentTab">DocumentTab</param>
        public void SetDocumentTab(DocumentTabViewModel documentTab)
        {
            SelectedDocumentTab = documentTab;
            SelectedFilePath = null;
        }

        /// <summary>
        /// SelectedDocumentTabが変更されたときにプロパティ変更を通知します。
        /// </summary>
        partial void OnSelectedDocumentTabChanged(DocumentTabViewModel? value)
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SourceDisplayName));
        }

        /// <summary>
        /// SelectedFilePathが変更されたときにプロパティ変更を通知します。
        /// </summary>
        partial void OnSelectedFilePathChanged(string? value)
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SourceDisplayName));
        }
    }
}
