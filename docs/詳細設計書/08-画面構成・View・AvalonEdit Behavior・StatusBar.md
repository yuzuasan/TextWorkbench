# TextWorkbench 詳細設計書 第8部：画面構成・View・AvalonEdit Behavior・StatusBar

## 1. 本書の対象

本書では、MainWindowを中心としたPresentation層の画面構成、DocumentView、DiffView、SearchReplacePanel、StatusBar、およびAvalonEdit Behaviorの責務と連携方法を定義する。

ViewはUI表示とユーザー操作の受付に限定し、アプリケーション処理はViewModelおよびUseCaseへ委譲する。

---

## 2. MainWindowの構成

MainWindowは以下の構成とする。

```text
MainWindow
├─ Menu
├─ QuickActions
├─ SearchReplacePanel（必要時表示）
├─ TabControl
│  └─ TabItem
│     └─ ContentControl
│        ├─ DocumentView
│        └─ DiffView
└─ StatusBar
```

MainWindowはMainWindowViewModelをDataContextとする。

MainWindow自身にはBusiness Logicを実装しない。

### 2.1 Menu

MenuにはアプリケーションCommandをBindingする。

対象Commandは、New、Open、Save、Save As、Close、Exit、Search、Replace、Compare、Find Next、Find Previous、Replace Next、Replace All、Next Diff、Previous Diffなどとする。

### 2.2 QuickActions

QuickActionsにはMenuと同じCommandを利用する。

同じ操作をMenuとQuickActionsで別々に実装しない。

例えばSave操作の場合、

```text
Menu Save
    └─ SaveCommand

QuickActions Save
    └─ SaveCommand
```

のように、同一Commandを利用する。

---

## 3. TabControl

TabControlはWorkspaceのTab一覧を表示する。

```text
ItemsSource  = Workspace.Tabs
SelectedItem = Workspace.ActiveTab
```

`SelectedItem`はTwoWay Bindingとする。

Tabの種類によって表示するViewを切り替える。

```text
DocumentTabViewModel → DocumentView
DiffTabViewModel      → DiffView
```

この切り替えはDataTemplateまたはDataTemplateSelectorで行う。

View側でTabの種類を判定して、アプリケーション処理を分岐させない。

---

## 4. DocumentView

DocumentViewはDocumentTabViewModelに対応するViewである。

主な役割は以下とする。

* AvalonEditの表示
* Document内容の編集
* 編集可否の反映
* Caret位置の表示に必要な情報提供
* SearchHighlightBehaviorの装着

DocumentViewはFileServiceやSearchServiceなどのApplication Serviceを直接呼び出さない。

### 4.1 AvalonEdit

DocumentView内にAvalonEditのTextEditorを配置する。

```text
DocumentView
└─ TextEditor
   ├─ TextEditorDocumentBehavior
   ├─ EditorStateBehavior
   └─ SearchHighlightBehavior
```

AvalonEdit固有の処理はPresentation層に閉じ込める。

---

## 5. TextEditorDocumentBehavior

TextEditorDocumentBehaviorはDocumentの内容とAvalonEditのTextEditorを同期する。

### 5.1 初期同期

DocumentTabが表示された際、ViewModelが保持するDocument.CurrentContentをEditorへ設定する。

```text
Document.CurrentContent
        ↓
TextEditor.Text
```

初期同期によって発生するTextChangedは、ユーザー編集として扱わない。

そのため、初期同期中は変更通知を抑制するための同期状態をBehavior内部で管理する。

### 5.2 ユーザー編集

ユーザーがEditorを編集した場合はTextChangedを受け取り、ViewModel側のDocumentへ反映する。

```text
TextEditor.TextChanged
        ↓
Document.UpdateContent(...)
        ↓
IsModified = true
```

通常のユーザー編集では、変更のたびにViewModelからEditorへ同じ内容を再設定しない。

これにより、

```text
Editor → ViewModel → Editor → ...
```

という不要な同期ループを防止する。

---

## 6. EditorStateBehavior

EditorStateBehaviorはDocumentの編集状態をAvalonEditへ反映する。

### 6.1 ReadOnly

以下の状態から`IsEditable`を判断する。

```text
IsEditable =
    !IsReadOnly &&
    !IsLoading &&
    !IsSaving
```

Editorへは反対の状態として、

```text
TextEditor.IsReadOnly = !IsEditable
```

を反映する。

これにより、以下の場合はEditorを編集不可とする。

* 読み取り専用ファイル
* 読み込み中
* 保存中

### 6.2 Caret

Caret位置はPresentation層のEditor状態として扱う。

行・列の表示が必要な場合はAvalonEditのCaret位置から取得し、StatusBarへ反映する。

行・列は1始まりで表示する。

例：

```text
Ln 1, Col 1
```

DocumentモデルにCaret位置を保持しない。

---

## 7. SearchHighlightBehavior

SearchHighlightBehaviorは検索結果の視覚表示を担当する。

検索処理そのものはSearchServiceが担当し、検索結果の位置情報はDocumentTabViewModelが保持する。

```text
DocumentTabViewModel
├─ SearchResults
└─ CurrentSearchIndex
        ↓
SearchHighlightBehavior
        ↓
AvalonEdit上のハイライト
```

### 7.1 通常検索結果

`SearchResults`に含まれるすべての検索結果をハイライトする。

### 7.2 CurrentSearchIndex

`CurrentSearchIndex`が示す検索結果は、通常の検索結果とは別の強調表示を行う。

現在結果はAvalonEditのSelectionとして扱う。

また、現在結果がEditor上で見える位置までスクロールする。

中央配置までは必須としない。

### 7.3 検索結果変更

以下の場合はハイライトを更新する。

* SearchText変更
* IsRegex変更
* Find Next
* Find Previous
* 検索結果更新
* Document編集

Document編集時は既存SearchResultsを破棄するため、ハイライトも解除する。

### 7.4 Searchパネルを閉じた場合

SearchReplacePanelを閉じてもDocumentTabViewModelのSearch stateは保持する。

ただし、SearchHighlightBehaviorによる視覚的なハイライトは解除する。

再度SearchReplacePanelを表示した場合は、そのDocumentTabViewModelが保持している検索状態を表示できる。

---

## 8. SearchReplacePanel

SearchReplacePanelはMainWindowの共通UIとして配置する。

検索状態そのものはMainWindowViewModelではなく、アクティブなDocumentTabViewModelが保持する。

```text
MainWindow
└─ SearchReplacePanel
       ↓
Active DocumentTabViewModel
       ├─ SearchText
       ├─ IsRegex
       ├─ SearchResults
       └─ CurrentSearchIndex
```

### 8.1 Document切替

Document AからDocument Bへ切り替えた場合、SearchReplacePanelはBのSearch stateを表示する。

AのSearch stateはAのDocumentTabViewModelに保持される。

```text
Document A → Document B

A:
  Search stateを保持

B:
  BのSearch stateを表示
```

### 8.2 DiffTab

ActiveTabがDiffTabの場合、SearchReplacePanelを非表示とする。

DiffTabにはDocument検索用のSearch stateを適用しない。

### 8.3 Documentへ戻った場合

DiffTabからDocumentTabへ戻った場合、そのDocumentTabが保持しているSearch stateを再表示できる。

---

## 9. DiffView

DiffViewはDiffTabViewModelに対応するViewである。

主な役割は以下とする。

* DiffPlex.Wpfによる差分表示
* 左右ペイン表示
* 差分ハイライト
* 左右ペインのスクロール同期
* CurrentDiffの強調表示
* CurrentDiffへのスクロール

DiffViewはDiffPlexの表示機能を利用してよいが、DiffPlexの比較処理自体は行わない。

比較処理はApplication層のDiffServiceとInfrastructure層のIDiffEngineAdapterが担当する。

---

## 10. DiffViewとDiffTabViewModelの境界

DiffTabViewModelは以下を保持する。

```text
DiffSession
CurrentDiffIndex
DiffCount
```

DiffViewはこれらの状態を利用して表示を更新する。

DiffView側ではDiffPlex APIを利用できるが、DiffPlex固有の結果をApplication層へ返さない。

責務は以下のように分離する。

```text
DiffService
  ↓
DiffResult

DiffTabViewModel
  ↓
DiffSession / CurrentDiffIndex

DiffView
  ↓
DiffPlex.Wpfによる表示
```

---

## 11. DiffのCurrent表示

CurrentDiffIndexが示す差分を、通常の差分表示とは別に強調する。

例えば、

```text
DiffCount = 5
CurrentDiffIndex = 2
```

の場合、3件目の差分をCurrent Diffとして扱う。

StatusBarでは、

```text
差分 3 / 5
```

と表示する。

CurrentDiffIndexが`-1`の場合は現在の差分未選択として扱う。

この場合は、

```text
差分 5件
```

または

```text
- / 5
```

など、現在位置を持たない表示とする。

---

## 12. StatusBar

StatusBarはMainWindowの共通UIとして配置する。

専用のStatusBarViewModelは作成せず、MainWindowViewModelから必要な表示状態を提供する。

ActiveTabの種類によって表示内容を切り替える。

---

## 13. DocumentタブのStatusBar

Documentタブでは、現在のDocument状態を確認できる情報を表示する。

通常：

```text
UTF-8 | CRLF | Ln 10, Col 25
```

変更あり：

```text
UTF-8 | CRLF | Ln 10, Col 25 | 変更あり
```

読み取り専用：

```text
UTF-8 | CRLF | Ln 10, Col 25 | 読み取り専用
```

処理中：

```text
UTF-8 | CRLF | 読み込み中...
```

または、

```text
UTF-8 | CRLF | 保存中...
```

とする。

### 13.1 Encoding表示

Document内部のEncoding enumを以下の表示へ変換する。

| 内部値      | 表示        |
| -------- | --------- |
| Utf8     | UTF-8     |
| Utf8Bom  | UTF-8 BOM |
| ShiftJis | Shift-JIS |

### 13.2 LineEnding表示

| 内部値  | 表示   |
| ---- | ---- |
| CrLf | CRLF |
| Lf   | LF   |
| Cr   | CR   |

### 13.3 行・列

Caret位置から取得した行・列を1始まりで表示する。

例：

```text
Ln 1, Col 1
```

Document自体にはCaret位置を保持しない。

---

## 14. DiffタブのStatusBar

DiffTabでは比較対象と差分状態を表示する。

基本形式：

```text
比較: A.txt (UTF-8) ↔ B.txt (Shift-JIS) | 差分 3 / 10
```

ここで、

* `3`：現在選択されている差分の番号
* `10`：DiffCount

を表す。

比較開始中は、

```text
A.txt ↔ B.txt | 比較中...
```

とする。

現在の差分が選択されていない場合は、

```text
A.txt ↔ B.txt | 差分 10件
```

または、

```text
A.txt ↔ B.txt | - / 10
```

とする。

---

## 15. Viewに実装しない処理

以下の処理をViewへ実装しない。

* FileServiceの呼び出し
* Encoding処理
* LineEnding処理
* SearchServiceの呼び出し
* ReplaceServiceの呼び出し
* DiffServiceの呼び出し
* UseCaseの内部処理
* Documentの保存状態判定
* Diff結果の生成
* Regex処理
* MessageBoxによるApplication処理

ViewはCommandのBinding、表示、AvalonEdit/DiffPlex.WpfなどPresentation固有UIの制御に限定する。

---

## 16. ViewとApplication層の境界

Application層はWPF固有型を参照しない。

特に以下をApplication層へ持ち込まない。

* `Window`
* `MessageBox`
* `TextEditor`
* `TextView`
* `DiffPlex.Wpf`のViewer

一方、DiffPlexの比較エンジンはInfrastructure Adapterへ隔離する。

```text
Presentation
    ↓
Application
    ↓
Infrastructure
```

という依存方向を維持する。

---

## 17. Behaviorの共通ルール

BehaviorはPresentation層に配置する。

MVPでは以下の3種類を基本とする。

```text
TextEditorDocumentBehavior
EditorStateBehavior
SearchHighlightBehavior
```

BehaviorはAvalonEditとの連携を担当するが、Application Serviceの代替にはしない。

また、Behavior自身に検索アルゴリズムやファイル保存処理を実装しない。

---

## 18. 実装時の確認ポイント

AIコーディングツールへ実装を指示する際は、以下を確認する。

* MainWindowはBusiness Logicを持たない
* MenuとQuickActionsは同じCommandを利用する
* TabControl.SelectedItemとWorkspace.ActiveTabをTwoWay Bindingする
* DocumentTabはDocumentViewを表示する
* DiffTabはDiffViewを表示する
* SearchReplacePanelはMainWindowの共通UIとする
* Search stateはDocumentTabViewModel単位で保持する
* DiffTabではSearchReplacePanelを非表示にする
* TextEditorDocumentBehaviorは初期同期を編集として扱わない
* ユーザー編集時はDocumentを更新する
* EditorStateBehaviorはIsEditableをTextEditor.IsReadOnlyへ反映する
* SearchHighlightBehaviorは検索処理自体を実行しない
* SearchHighlightBehaviorは全検索結果とCurrent結果を別々に表示する
* Document編集時は検索ハイライトを解除する
* DiffViewはDiffPlex.Wpfの表示を担当する
* DiffViewで差分計算を実行しない
* StatusBarは専用ViewModelを作成せずMainWindowViewModelから状態を取得する
* 行・列は1始まりで表示する
* ViewからApplication/Infrastructureの具体実装を直接呼び出さない

以上をPresentation層の画面・View・Behavior実装の基本契約とする。
