using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace TextWorkbench.Presentation.ViewModels;

/// <summary>
/// アプリケーション全体の作業領域を表し、開いているTabと現在選択されているTabを管理します。
/// </summary>
public sealed partial class Workspace : ObservableObject
{
    /// <summary>
    /// Tab一覧を取得します。
    /// </summary>
    public ObservableCollection<TabViewModel> Tabs { get; }

    /// <summary>
    /// 現在アクティブなTabを取得または設定します。
    /// </summary>
    [ObservableProperty]
    private TabViewModel? _activeTab;

    /// <summary>
    /// <see cref="Workspace"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Workspace()
    {
        Tabs = new ObservableCollection<TabViewModel>();
    }

    /// <summary>
    /// Tabを追加し、ActiveTabに設定します。
    /// </summary>
    /// <param name="tab">追加するTab</param>
    public void AddTab(TabViewModel tab)
    {
        if (tab == null)
            throw new ArgumentNullException(nameof(tab));

        Tabs.Add(tab);
        ActiveTab = tab;
    }

    /// <summary>
    /// Tabを削除します。削除対象がActiveTabの場合、適切なTabをActiveTabに設定します。
    /// </summary>
    /// <param name="tab">削除するTab</param>
    public void RemoveTab(TabViewModel tab)
    {
        if (tab == null)
            throw new ArgumentNullException(nameof(tab));

        var index = Tabs.IndexOf(tab);
        if (index < 0)
            return;

        Tabs.Remove(tab);

        // 削除対象がActiveTabだった場合、ActiveTabを再設定
        if (ActiveTab == tab)
        {
            if (Tabs.Count > 0)
            {
                // 削除位置と同じインデックスがあればそこを、なければ最後をActiveにする
                var newIndex = Math.Min(index, Tabs.Count - 1);
                ActiveTab = Tabs[newIndex];
            }
            else
            {
                ActiveTab = null;
            }
        }
    }

    /// <summary>
    /// ActiveTabを設定します。
    /// </summary>
    /// <param name="tab">アクティブにするTab</param>
    public void SetActiveTab(TabViewModel? tab)
    {
        ActiveTab = tab;
    }

    /// <summary>
    /// 指定したファイルパスのDocumentを持つDocumentTabを検索します。
    /// </summary>
    /// <param name="filePath">検索するファイルパス（正規化済み絶対パス）</param>
    /// <returns>見つかったDocumentTab、見つからない場合は null</returns>
    public DocumentTabViewModel? FindDocumentByFilePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        foreach (var tab in Tabs)
        {
            if (tab is DocumentTabViewModel documentTab &&
                documentTab.Document.FilePath != null)
            {
                if (string.Equals(
                    documentTab.Document.FilePath,
                    filePath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return documentTab;
                }
            }
        }

        return null;
    }
}
