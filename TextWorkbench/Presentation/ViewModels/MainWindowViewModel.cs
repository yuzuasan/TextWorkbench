using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextWorkbench.Application.Interfaces;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// MainWindowのViewModelです。
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IUserNotificationService _notificationService;

    /// <summary>
    /// Workspaceを取得します。
    /// </summary>
    public Workspace Workspace { get; }

    /// <summary>
    /// 処理中かどうかを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private bool _isProcessing;

    /// <summary>
    /// 新規作成コマンドを取得します。
    /// </summary>
    public IRelayCommand NewCommand { get; }

    /// <summary>
    /// 開くコマンドを取得します。
    /// </summary>
    public IRelayCommand OpenCommand { get; }

    /// <summary>
    /// 保存コマンドを取得します。
    /// </summary>
    public IRelayCommand SaveCommand { get; }

    /// <summary>
    /// 名前を付けて保存コマンドを取得します。
    /// </summary>
    public IRelayCommand SaveAsCommand { get; }

    /// <summary>
    /// 閉じるコマンドを取得します。
    /// </summary>
    public IRelayCommand CloseCommand { get; }

    /// <summary>
    /// 終了コマンドを取得します。
    /// </summary>
    public IRelayCommand ExitCommand { get; }

    /// <summary>
    /// 検索コマンドを取得します。
    /// </summary>
    public IRelayCommand SearchCommand { get; }

    /// <summary>
    /// 置換コマンドを取得します。
    /// </summary>
    public IRelayCommand ReplaceCommand { get; }

    /// <summary>
    /// 比較コマンドを取得します。
    /// </summary>
    public IRelayCommand CompareCommand { get; }

    /// <summary>
    /// <see cref="MainWindowViewModel"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="notificationService">通知サービス</param>
    public MainWindowViewModel(IUserNotificationService notificationService)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        Workspace = new Workspace();

        NewCommand = new RelayCommand(ExecuteNew, CanExecuteNew);
        OpenCommand = new RelayCommand(ExecuteOpen, CanExecuteOpen);
        SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        SaveAsCommand = new RelayCommand(ExecuteSaveAs, CanExecuteSaveAs);
        CloseCommand = new RelayCommand(ExecuteClose, CanExecuteClose);
        ExitCommand = new RelayCommand(ExecuteExit, CanExecuteExit);
        SearchCommand = new RelayCommand(ExecuteSearch, CanExecuteSearch);
        ReplaceCommand = new RelayCommand(ExecuteReplace, CanExecuteReplace);
        CompareCommand = new RelayCommand(ExecuteCompare, CanExecuteCompare);
    }

    private bool CanExecuteNew() => !IsProcessing;
    private void ExecuteNew()
    {
        _notificationService.ShowInfo("新規作成（仮実装）");
    }

    private bool CanExecuteOpen() => !IsProcessing;
    private void ExecuteOpen()
    {
        _notificationService.ShowInfo("開く（仮実装）");
    }

    private bool CanExecuteSave() => !IsProcessing;
    private void ExecuteSave()
    {
        _notificationService.ShowInfo("保存（仮実装）");
    }

    private bool CanExecuteSaveAs() => !IsProcessing;
    private void ExecuteSaveAs()
    {
        _notificationService.ShowInfo("名前を付けて保存（仮実装）");
    }

    private bool CanExecuteClose() => !IsProcessing;
    private void ExecuteClose()
    {
        _notificationService.ShowInfo("閉じる（仮実装）");
    }

    private bool CanExecuteExit() => !IsProcessing;
    private void ExecuteExit()
    {
        _notificationService.ShowInfo("終了（仮実装）");
    }

    private bool CanExecuteSearch() => !IsProcessing;
    private void ExecuteSearch()
    {
        _notificationService.ShowInfo("検索（仮実装）");
    }

    private bool CanExecuteReplace() => !IsProcessing;
    private void ExecuteReplace()
    {
        _notificationService.ShowInfo("置換（仮実装）");
    }

    private bool CanExecuteCompare() => !IsProcessing;
    private void ExecuteCompare()
    {
        _notificationService.ShowInfo("比較（仮実装）");
    }

    /// <summary>
    /// 全てのコマンドのCanExecute状態を更新します。
    /// </summary>
    public void RefreshCommandStates()
    {
        NewCommand.NotifyCanExecuteChanged();
        OpenCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        SaveAsCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
        ExitCommand.NotifyCanExecuteChanged();
        SearchCommand.NotifyCanExecuteChanged();
        ReplaceCommand.NotifyCanExecuteChanged();
        CompareCommand.NotifyCanExecuteChanged();
    }
}

