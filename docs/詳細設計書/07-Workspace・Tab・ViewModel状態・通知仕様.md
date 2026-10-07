# TextWorkbench 詳細設計書 第7部：Workspace・Tab・ViewModel状態・通知仕様

## 1. 本書の対象

本書では、Workspace、TabViewModel、DocumentTabViewModel、DiffTabViewModel、MainWindowViewModelについて、実装時に必要となるプロパティ、状態通知、Tabの追加・削除・切替、および状態変更の伝播方法を定義する。

本書はWPF/MVVM実装時のViewModel契約を明確にすることを目的とする。

---

## 2. ViewModelの基本方針

ViewModelはCommunityToolkit.Mvvmの`ObservableObject`を基本クラスとして使用し、プロパティ変更通知には`INotifyPropertyChanged`を利用する。

ViewModelからViewへの通知は、WPF Bindingが利用できる通常の`PropertyChanged`通知で行う。

Collectionについては`ObservableCollection<T>`を使用する。

ViewModelは以下を担当する。

* UI表示用状態の保持
* Commandの公開
* Application層のUseCase呼び出し
* Document状態のUIへの反映
* Tab選択状態の管理
* Search/Diff状態のUIへの反映

ViewModelは以下を直接実行しない。

* ファイルI/O
* Encoding処理
* DiffPlex処理
* WPF MessageBox呼び出し
* AvalonEditへの検索処理

---

## 3. Workspace

Workspaceは開いているTabの集合と現在アクティブなTabを管理する。

### 3.1 プロパティ

```text
ObservableCollection<TabViewModel> Tabs
TabViewModel? ActiveTab
```

`Tabs`は常に同一のCollectionインスタンスを保持し、Tabの追加・削除はCollection操作によって通知する。

`ActiveTab`変更時には`PropertyChanged`を発生させる。

### 3.2 Tab追加

新しいTabを作成した場合、Workspaceへの追加後にそのTabをActiveTabへ設定する。

```text
Tabs.Add(tab)
ActiveTab = tab
```

Open成功時も同様に、作成したDocumentTabを追加してアクティブにする。

### 3.3 Tab削除

Tabを閉じる場合は、UseCase側で保存確認・保存処理などを完了した後にWorkspaceから削除する。

削除対象がActiveTabの場合、削除後のActiveTabをWorkspaceが決定する。

MVPでは、削除後に残っているTabの中からWPF TabControlの標準的な選択動作に依存せず、ViewModel側で明示的にActiveTabを設定する。

推奨する選択順は以下とする。

1. 閉じたTabの直前に存在したTab
2. それが存在しなければ閉じたTabの直後に存在したTab
3. それも存在しなければ`null`

ただし、実装時にTabControlの選択順との不整合が発生する場合は、Workspace側で「削除前のインデックス」を利用して決定する。

### 3.4 ActiveTab

MainWindowのTabControlとWorkspace.ActiveTabをTwoWay Bindingする。

```text
TabControl.SelectedItem <-> Workspace.ActiveTab
```

これにより、ユーザーによるTab切替とCommandによるTab切替の双方を同一状態として扱う。

---

## 4. TabViewModel

TabViewModelはTab共通のViewModel基底クラスとする。

共通して保持する状態は最小限とし、Document固有状態やDiff固有状態を基底クラスへ持ち込まない。

概念上は以下の階層とする。

```text
TabViewModel
 ├─ DocumentTabViewModel
 └─ DiffTabViewModel
```

`TabViewModel`はTabとして表示するための共通状態を提供する。

### 4.1 共通プロパティ

```text
string DisplayTitle
```

`DisplayTitle`はTabヘッダー表示用の文字列である。

DocumentTabでは以下の規則を使用する。

* FilePathあり：FileName
* FilePathなし：無題N

`IsModified`をDisplayTitleへ埋め込まない。

DiffTabでは比較対象のファイル名など、DiffTab固有の表示名を使用する。

---

## 5. DocumentTabViewModel

DocumentTabViewModelはDocumentをUIへ公開するViewModelである。

DocumentそのものをViewへ直接Bindingさせず、UIに必要な状態をViewModelで公開する。

### 5.1 主な状態

DocumentTabViewModelは少なくとも以下の状態を公開する。

```text
Document Document

string DisplayTitle

bool IsModified
bool IsReadOnly
bool IsLoading
bool IsSaving
bool IsEditable

string SearchText
bool IsRegex
IReadOnlyList<SearchResult> SearchResults
int CurrentSearchIndex
int ReplaceNextStartIndex
```

`IsEditable`はDocumentの状態から算出する。

```text
IsEditable =
    !Document.IsReadOnly &&
    !Document.IsLoading &&
    !Document.IsSaving
```

Documentの状態変更時には、これらの派生プロパティについても必要な通知を発生させる。

### 5.2 Document状態の公開

以下のDocument状態をDocumentTabViewModelへ反映する。

| Document   | ViewModel       |
| ---------- | --------------- |
| FilePath   | DisplayTitleの算出 |
| FileName   | DisplayTitleの算出 |
| IsModified | IsModified      |
| IsReadOnly | IsReadOnly      |
| IsLoading  | IsLoading       |
| IsSaving   | IsSaving        |
| IsEditable | IsEditable      |

Documentの内部モデルを変更した際、ViewModelがUI更新を認識できる仕組みを用意する。

---

## 6. Document状態変更の通知

Document自体はWPF/AvalonEditから独立したDomainモデルとして扱う。

そのため、Documentの状態変更をViewModelへ伝える方法は、DocumentへWPF依存の通知機構を持ち込まない方式を基本とする。

実装上は、UseCaseまたはViewModelがDocument更新後に必要なViewModel通知を行う。

特に以下の操作では通知が必要である。

* Open完了
* Save開始
* Save完了
* Save As完了
* 編集によるContent変更
* ReadOnly状態変更
* Loading状態変更
* Saving状態変更

### 6.1 Save成功時

Save成功後は以下を更新する。

```text
IsModified = false
IsSaving = false
```

Save As成功時にはさらに、

```text
FilePath
FileName
DisplayTitle
IsReadOnly = false
```

を更新する。

### 6.2 Save失敗時

Save失敗時はDocumentの変更状態を維持する。

```text
IsModified = true
```

また、保存処理中状態を解除する。

```text
IsSaving = false
```

---

## 7. Search状態通知

Search状態はDocumentTabViewModelごとに保持する。

以下の変更ではPropertyChangedを発生させる。

```text
SearchText
IsRegex
SearchResults
CurrentSearchIndex
ReplaceNextStartIndex
```

Search条件変更時はSearchServiceによって結果を再生成し、

```text
SearchResults
CurrentSearchIndex
```

を更新する。

検索結果が変更された場合、SearchHighlightBehaviorが再描画対象として認識できるよう通知する。

---

## 8. 編集によるSearch状態無効化

Documentの編集が発生した場合、既存の検索結果は無効になる。

編集後は以下とする。

```text
SearchResults = empty
CurrentSearchIndex = -1
```

AvalonEdit上の検索ハイライトも解除する。

検索条件そのものは保持する。

つまり、編集後に

```text
SearchText
IsRegex
```

を自動消去しない。

再検索はユーザーがFind Next等の操作を行った時点で実行する。

---

## 9. DiffTabViewModel

DiffTabViewModelはDiffSessionを保持し、Diff結果を表示・操作するためのViewModelである。

### 9.1 プロパティ

```text
DiffSession DiffSession
int CurrentDiffIndex
int DiffCount
```

以下のCommandを公開する。

```text
NextDiffCommand
PreviousDiffCommand
```

DiffTabViewModelはDiffPlexの具体型を保持しない。

### 9.2 DiffCount

`DiffCount`はDiffSession内の差分行グループ数である。

文字単位の差分数ではない。

```text
DiffCount = DiffLine.Type != Unchanged の件数
```

### 9.3 CurrentDiffIndex

`CurrentDiffIndex`は0-basedとする。

```text
-1      現在の差分未選択
0       1件目
1       2件目
2       3件目
...
N - 1   N件目
```

UI上で表示する番号は、

```text
CurrentDiffIndex + 1
```

とする。

例えば、

```text
DiffCount = 5
CurrentDiffIndex = 2
```

の場合、UI表示は、

```text
差分 3 / 5
```

となる。

### 9.4 Diff移動

NextDiffは次の差分へ移動する。

PreviousDiffは前の差分へ移動する。

循環しない。

```text
先頭でPrevious → 何もしない
末尾でNext     → 何もしない
```

境界位置でもCommand自体は無効化せず、実行時にNo-opとする。

---

## 10. MainWindowViewModel

MainWindowViewModelはアプリケーション全体のCommandとWorkspaceを管理する。

### 10.1 主な状態

MainWindowViewModelは、Workspaceをプロパティとして保持する。

```text
Workspace Workspace
```

また、アプリケーション全体の処理中状態を保持する。

```text
bool IsProcessing
```

`IsProcessing`は、OpenやDiffなどの処理中に不要な操作を制御するために使用する。

`IsProcessing`の具体的な算出方法は、既存設計で定義した各処理状態を基に実装する。

### 10.2 ActiveTab

MainWindowViewModelから、

```text
Workspace.ActiveTab
```

をUIへ公開する。

ActiveTabが変更された場合、以下の状態を更新する。

* CommandのCanExecute
* Searchパネル表示状態
* StatusBar表示
* Document/Diff固有操作
* AvalonEdit側の表示対象

---

## 11. ActiveTab変更時の処理

ActiveTab変更時は、旧Tabに対する表示状態を解除し、新Tabの状態をUIへ反映する。

DocumentTabの場合：

* Document Editorを表示
* Search状態をそのDocumentTabから取得
* SearchHighlightを再構築
* Document編集可否を反映
* StatusBarをDocument用に更新

DiffTabの場合：

* DiffViewerを表示
* Searchパネルを非表示
* Diff navigation状態を反映
* StatusBarをDiff用に更新

Tab切替によってDocumentやDiffSession自体を再生成しない。

---

## 12. Tabライフサイクルとイベント購読

DocumentTabViewModelやDiffTabViewModelが外部イベントを購読する場合、Tab削除時に必ず購読解除する。

特に以下のような状態を残さない。

```text
閉じたTab
   ↓
イベント購読が残る
   ↓
Workspaceから削除しても参照され続ける
```

これを避けるため、Tab生成時に購読を設定し、Tab破棄時に解除する。

Presentation Behaviorについても、対象Editorの破棄時にイベント購読を解除する。

MVPでは、イベント購読を必要最小限とし、可能な限りViewModelから明示的な状態更新を行う。

---

## 13. ViewとのBinding契約

MainWindowはMainWindowViewModelをDataContextとする。

TabControlはWorkspace.TabsをItemsSourceとし、SelectedItemをActiveTabへTwoWay Bindingする。

概念的には以下とする。

```text
ItemsSource  = Workspace.Tabs
SelectedItem = Workspace.ActiveTab
Mode         = TwoWay
```

Tabの具体的な表示はDataTemplate/DataTemplateSelectorによってDocumentTabとDiffTabを切り替える。

View側でDocumentTabViewModelとDiffTabViewModelを型判定して処理ロジックを大量に持たせない。

表示切替に必要な処理はPresentation層に限定する。

---

## 14. Command状態更新

Tab追加・削除・切替、Document状態変更、Search結果変更、Diff状態変更などによりCommandのCanExecuteが変化する。

状態変更後は対象Commandの`NotifyCanExecuteChanged()`を呼び出す。

代表例：

```text
ActiveTab変更
    ↓
Save / SaveAs / Close / Find / Replace / Diff操作
    ↓
NotifyCanExecuteChanged()
```

CommandごとにCanExecute判定を重複して管理するのではなく、MainWindowViewModelの状態から一貫して判定する。

---

## 15. 状態更新の原則

実装時は、以下の原則を維持する。

1. UI表示状態はViewModelが管理する。
2. DocumentはWPF/AvalonEditに依存しない。
3. DiffSessionはDiffTab生成時のスナップショットとして扱う。
4. Search状態はDocumentTab単位で保持する。
5. Diff状態はDiffTab単位で保持する。
6. ActiveTabはWorkspaceで一元管理する。
7. Tab削除時には関連イベント購読を解除する。
8. 派生状態`IsEditable`はDocument状態から一貫して算出する。
9. CommandのCanExecuteはViewModel状態と一致させる。
10. Viewにアプリケーション処理ロジックを実装しない。
11. ViewModelからFile I/O、DiffPlex、MessageBoxを直接呼び出さない。
12. AvalonEdit固有処理はBehavior等のPresentation層へ隔離する。

---

## 16. 実装時の確認ポイント

AIコーディングツールへ実装を指示する際は、特に以下を確認する。

* `Tabs`が`ObservableCollection<TabViewModel>`になっていること
* `ActiveTab`が唯一のアクティブTab状態であること
* TabControl.SelectedItemとActiveTabがTwoWayで同期すること
* DocumentTabとDiffTabの状態を基底Tabへ過剰に集約しないこと
* Search状態がDocumentTabごとに独立していること
* Diff状態がDiffTabごとに独立していること
* CurrentDiffIndexが0-basedであること
* DisplayTitleにIsModifiedを付加しないこと
* Tab削除時にイベント購読が残らないこと
* Document編集時にSearchResultsが無効化されること
* Save成功時にIsModifiedがfalseになること
* Save失敗時にIsModifiedを維持すること
* ReadOnly/Loading/SavingからIsEditableを正しく算出すること
* Command状態更新時にNotifyCanExecuteChanged()が呼ばれること

以上をViewModel実装の基本契約とする。
