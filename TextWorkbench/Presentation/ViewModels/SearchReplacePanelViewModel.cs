using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// SearchReplacePanelの表示状態と、アクティブなDocumentTabViewModelの検索状態を仲介します。
/// </summary>
public sealed class SearchReplacePanelViewModel : ObservableObject
{
    private DocumentTabViewModel? _activeDocumentTab;
    private string _replaceText = string.Empty;
    private bool _isOpen;

    public SearchReplacePanelViewModel()
    {
        CloseCommand = new RelayCommand(() => IsOpen = false);
    }

    /// <summary>
    /// 検索・置換の対象となるアクティブなDocumentTabを取得または設定します。
    /// </summary>
    public DocumentTabViewModel? ActiveDocumentTab
    {
        get => _activeDocumentTab;
        set
        {
            if (ReferenceEquals(_activeDocumentTab, value))
            {
                return;
            }

            // 以前のDocumentTabの変更通知を解除します。
            if (_activeDocumentTab != null)
            {
                _activeDocumentTab.PropertyChanged -= OnActiveDocumentTabPropertyChanged;
            }

            _activeDocumentTab = value;

            // 新しいDocumentTabの変更通知を購読します。
            if (_activeDocumentTab != null)
            {
                _activeDocumentTab.PropertyChanged += OnActiveDocumentTabPropertyChanged;
            }

            OnPropertyChanged(nameof(ActiveDocumentTab));
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(IsRegex));
            OnPropertyChanged(nameof(SearchResults));
            OnPropertyChanged(nameof(CurrentSearchIndex));
            OnPropertyChanged(nameof(IsVisible));
        }
    }

    /// <summary>
    /// パネルを開いているかどうかを取得または設定します。
    /// </summary>
    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (SetProperty(ref _isOpen, value))
            {
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    /// <summary>
    /// Documentタブがアクティブで、かつパネルが開いている場合に表示します。
    /// </summary>
    public bool IsVisible => IsOpen && ActiveDocumentTab != null;

    /// <summary>
    /// パネルを閉じるコマンドを取得します。
    /// </summary>
    public IRelayCommand CloseCommand { get; }

    /// <summary>
    /// アクティブなDocumentTabの検索文字列を取得または設定します。
    /// DocumentTabがない場合は空文字列を返します。
    /// </summary>
    public string SearchText
    {
        get => _activeDocumentTab?.SearchText ?? string.Empty;
        set
        {
            if (_activeDocumentTab == null
                || _activeDocumentTab.SearchText == value)
            {
                return;
            }

            _activeDocumentTab.SearchText = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// アクティブなDocumentTabで正規表現を使用するかどうかを取得または設定します。
    /// DocumentTabがない場合はfalseを返します。
    /// </summary>
    public bool IsRegex
    {
        get => _activeDocumentTab?.IsRegex ?? false;
        set
        {
            if (_activeDocumentTab == null
                || _activeDocumentTab.IsRegex == value)
            {
                return;
            }

            _activeDocumentTab.IsRegex = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 置換後の文字列を取得または設定します。
    /// </summary>
    public string ReplaceText
    {
        get => _replaceText;
        set => SetProperty(ref _replaceText, value ?? string.Empty);
    }

    /// <summary>
    /// アクティブなDocumentTabの検索結果を取得します。
    /// DocumentTabがない場合は空の一覧を返します。
    /// </summary>
    public IReadOnlyList<SearchResult> SearchResults =>
        _activeDocumentTab?.SearchResults ?? Array.Empty<SearchResult>();

    /// <summary>
    /// アクティブなDocumentTabの現在の検索結果インデックスを取得します。
    /// DocumentTabがない場合は-1を返します。
    /// </summary>
    public int CurrentSearchIndex =>
        _activeDocumentTab?.CurrentSearchIndex ?? -1;

    /// <summary>
    /// アクティブなDocumentTabの検索状態変更をパネルへ通知します。
    /// </summary>
    private void OnActiveDocumentTabPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentTabViewModel.SearchText))
        {
            OnPropertyChanged(nameof(SearchText));
        }
        else if (e.PropertyName == nameof(DocumentTabViewModel.IsRegex))
        {
            OnPropertyChanged(nameof(IsRegex));
        }
        else if (e.PropertyName == nameof(DocumentTabViewModel.SearchResults))
        {
            OnPropertyChanged(nameof(SearchResults));
        }
        else if (e.PropertyName == nameof(DocumentTabViewModel.CurrentSearchIndex))
        {
            OnPropertyChanged(nameof(CurrentSearchIndex));
        }
    }
}
