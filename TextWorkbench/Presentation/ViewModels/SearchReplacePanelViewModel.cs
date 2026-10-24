using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using TextWorkbench.Application.Models;
using TextWorkbench.Application.UseCases;
using TextWorkbench.Domain.Models;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// SearchReplacePanelの表示状態と、アクティブなDocumentTabViewModelの検索状態を仲介します。
/// </summary>
public sealed class SearchReplacePanelViewModel : ObservableObject
{
    private readonly SearchTextUseCase _searchTextUseCase;
    private readonly ReplaceTextUseCase _replaceTextUseCase;
    private DocumentTabViewModel? _activeDocumentTab;
    private string _replaceText = string.Empty;
    private bool _isOpen;

    /// <summary>
    /// SearchReplacePanelViewModelの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="searchTextUseCase">検索UseCase</param>
    /// <param name="replaceTextUseCase">置換UseCase</param>
    public SearchReplacePanelViewModel(
        SearchTextUseCase searchTextUseCase,
        ReplaceTextUseCase replaceTextUseCase)
    {
        _searchTextUseCase = searchTextUseCase
            ?? throw new ArgumentNullException(nameof(searchTextUseCase));
        _replaceTextUseCase = replaceTextUseCase
            ?? throw new ArgumentNullException(nameof(replaceTextUseCase));

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
            var searchText = value ?? string.Empty;

            if (_activeDocumentTab == null
                || _activeDocumentTab.SearchText == searchText)
            {
                return;
            }

            _activeDocumentTab.SearchText = searchText;
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
    /// アクティブなDocumentの開始位置以降で、最初に一致した箇所を置換します。
    /// 文書への反映と置換後の状態更新は呼び出し元が行います。
    /// </summary>
    /// <returns>置換後のテキストと置換情報</returns>
    /// <exception cref="InvalidOperationException">
    /// アクティブなDocumentTabが存在しない場合
    /// </exception>
    public ReplaceResult ReplaceFirst()
    {
        if (_activeDocumentTab == null)
        {
            throw new InvalidOperationException(
                "置換対象のDocumentTabがありません。");
        }

        return _replaceTextUseCase.ExecuteFirst(
            _activeDocumentTab.Document.CurrentContent,
            SearchText,
            ReplaceText,
            IsRegex,
            _activeDocumentTab.ReplaceNextStartIndex);
    }

    /// <summary>
    /// アクティブなDocument全体を対象に、一致した箇所をすべて置換します。
    /// 文書への反映と置換後の状態更新は呼び出し元が行います。
    /// </summary>
    /// <returns>置換後のテキストと置換情報</returns>
    /// <exception cref="InvalidOperationException">
    /// アクティブなDocumentTabが存在しない場合
    /// </exception>
    public ReplaceResult ReplaceAll()
    {
        if (_activeDocumentTab == null)
        {
            throw new InvalidOperationException(
                "置換対象のDocumentTabがありません。");
        }

        return _replaceTextUseCase.ExecuteAll(
            _activeDocumentTab.Document.CurrentContent,
            SearchText,
            ReplaceText,
            IsRegex);
    }

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
            ExecuteSearch();
        }
        else if (e.PropertyName == nameof(DocumentTabViewModel.IsRegex))
        {
            OnPropertyChanged(nameof(IsRegex));
            ExecuteSearch();
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

    /// <summary>
    /// アクティブなDocumentTabの内容を検索します。
    /// </summary>
    private void ExecuteSearch()
    {
        if (_activeDocumentTab == null)
        {
            return;
        }

        var searchText = _activeDocumentTab.SearchText;

        // 空の検索文字列では検索結果をクリアします。
        if (string.IsNullOrEmpty(searchText))
        {
            _activeDocumentTab.ClearSearchResults();
            return;
        }

        try
        {
            var results = _searchTextUseCase.Execute(
                _activeDocumentTab.Document.CurrentContent,
                searchText,
                _activeDocumentTab.IsRegex);

            _activeDocumentTab.SetSearchResults(results);
        }
        catch (NotSupportedException)
        {
            // 未対応の検索方法では古い結果を残さないようにします。
            _activeDocumentTab.ClearSearchResults();
        }
    }
}
