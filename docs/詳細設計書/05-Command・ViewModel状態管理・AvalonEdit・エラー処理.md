# TextWorkbench 詳細設計書 第5部：Command・ViewModel状態管理・AvalonEdit・エラー処理

## 1. 本書の対象

本書では、Presentation 層を中心とした以下の詳細設計を定義する。

* MainWindowViewModel の Command
* Command の CanExecute
* Command 状態更新
* Edit メニュー
* AvalonEdit 連携
* Document と Editor の同期
* ReadOnly / Loading / Saving の Editor 状態
* SearchHighlight
* Application Exception
* Notification / Confirmation
* UI と Application 層のエラー境界

ViewModel に業務処理や WPF 固有処理を集約しすぎないことを基本方針とする。

---

# 2. Command の基本方針

MVP では CommunityToolkit.Mvvm の `RelayCommand` を使用する。

`AsyncRelayCommand` は使用しない。

MVP は同期処理を基本とし、大容量ファイル対応などで非同期化が必要になった場合に将来検討する。

Command はユーザー操作の入口として使用し、具体的な処理は UseCase または ViewModel の状態操作へ委譲する。

MainWindow の Menu、QuickActions、その他の操作入口は、可能な限り同一 Command を利用する。

---

# 3. MainWindowViewModel の Command

MainWindowViewModel は以下の主要 Command を提供する。

```text
New
Open
Save
SaveAs
Close
Exit
Search
Replace
Compare
```

検索・置換では以下も提供する。

```text
FindNext
FindPrevious
ReplaceNext
ReplaceAll
```

Diff では以下を提供する。

```text
NextDiff
PreviousDiff
```

Command は、View から直接 FileService や DiffPlex を呼び出すためのものではない。

---

# 4. Command と UseCase の責務

基本的な処理構造は以下とする。

```text
View
 ↓
Command
 ↓
MainWindowViewModel
 ↓
UseCase
 ↓
Application / Infrastructure
```

例：

```text
Open Command
 ↓
OpenDocumentUseCase
 ↓
IFileDialogService
IFileService
IEncodingService
ILineEndingService
 ↓
Document
 ↓
Workspace
```

Compare も同様に、

```text
Compare Command
 ↓
CompareDocumentsUseCase
 ↓
File / Encoding / LineEnding
 ↓
DiffService
 ↓
DiffSession
 ↓
DiffTabViewModel
```

とする。

ViewModel に File I/O や Diff 計算処理を実装しない。

---

# 5. CanExecute

Command の有効状態は、現在の Workspace / ActiveTab / Document / Diff 状態から直接評価する。

重複した状態変数を ViewModel に保持しない。

基本仕様：

```text
New:
    !IsProcessing

Open:
    !IsProcessing

Save:
    Document
    && !IsReadOnly
    && !IsLoading
    && !IsSaving

SaveAs:
    Document
    && !IsLoading
    && !IsSaving

Close:
    ActiveTab != null
    && !IsProcessing

Exit:
    !IsProcessing

Find:
    Document

Replace:
    Document

FindNext:
    Document
    && SearchResults.Count > 0

FindPrevious:
    Document
    && SearchResults.Count > 0

Compare:
    !IsProcessing

NextDiff:
    DiffTab
    && DiffCount > 0

PreviousDiff:
    DiffTab
    && DiffCount > 0
```

Find / Replace は検索文字列が空でも Command 自体は有効とする。

NextDiff / PreviousDiff は、現在位置が先頭・末尾に到達しても CanExecute を false にしない。

境界では操作を no-op とする。

---

# 6. Command 状態の更新

MainWindowViewModel に以下のメソッドを設ける。

```text
RefreshCommandStates()
```

内部で各 Command の、

```text
NotifyCanExecuteChanged()
```

を呼び出す。

状態更新が必要なタイミング：

* ActiveTab 変更
* Document 状態変更
* SearchResults 変更
* DiffSession 変更
* Tab 追加
* Tab 削除
* UseCase 実行完了

例えば Document が ReadOnly になった場合、

```text
Document.IsReadOnly
    ↓
RefreshCommandStates()
    ↓
SaveCommand.NotifyCanExecuteChanged()
    ↓
Save 無効
```

とする。

`CanSave`、`CanClose` などを別プロパティとして二重管理しない。

---

# 7. Edit メニュー

以下の編集操作は MainWindowViewModel の RelayCommand としない。

WPF の `ApplicationCommands` / RoutedCommand を利用する。

```text
編集
├─ 元に戻す → ApplicationCommands.Undo
├─ やり直す → ApplicationCommands.Redo
├─ 切り取り → ApplicationCommands.Cut
├─ コピー → ApplicationCommands.Copy
├─ 貼り付け → ApplicationCommands.Paste
└─ すべて選択 → ApplicationCommands.SelectAll
```

AvalonEdit と WPF の Command Routing / CanExecute を利用する。

ReadOnly Document では、

```text
Cut       → 無効
Paste     → 無効
Copy      → 有効
SelectAll → 有効
```

とする。

Undo / Redo は AvalonEdit の標準機能を利用する。

MVP ではアプリ独自の InputBinding を追加しない。

Clipboard 用の独自 Service も作成しない。

---

# 8. AvalonEdit との境界

`Document` は WPF / AvalonEdit に依存しない。

DocumentView に AvalonEdit の `TextEditor` を配置し、Presentation 層の Behavior によって DocumentTabViewModel と同期する。

主な Behavior：

```text
TextEditorDocumentBehavior
EditorStateBehavior
SearchHighlightBehavior
```

AvalonEdit の型を Domain / Application に渡さない。

---

# 9. TextEditorDocumentBehavior

TextEditor と DocumentTabViewModel のテキスト同期を担当する。

## 9.1 初期表示

```text
DocumentTabViewModel
 ↓
Document.CurrentContent
 ↓
TextEditor
```

Document を開いた直後など、Editor に初期内容を設定する。

このプログラム更新によって `IsModified` が true になってはならない。

---

## 9.2 通常編集

ユーザーが AvalonEdit 上で文字を入力すると、

```text
TextEditor.TextChanged
 ↓
TextEditorDocumentBehavior
 ↓
DocumentTabViewModel
 ↓
Document.UpdateContent()
 ↓
Document.IsModified = true
```

とする。

通常のユーザー編集のたびに、

```text
ViewModel → Editor → ViewModel
```

のような全文再設定を行わない。

Editor が保持している編集状態をそのまま利用する。

---

## 9.3 プログラムによる変更

Replace / Replace All など、ViewModel 側から Document の内容が変更された場合は Editor に反映する。

```text
Replace
 ↓
Document.UpdateContent()
 ↓
TextEditorDocumentBehavior
 ↓
TextEditor
```

この場合も同期処理によって二重に `IsModified` を更新しない。

そのため、プログラム同期中であることを識別する内部フラグを使用する。

概念例：

```text
_isSynchronizing = true
Editor.Text = content
_isSynchronizing = false
```

実装では例外発生時にも同期状態が解除されるようにする。

---

# 10. EditorStateBehavior

Document の編集可否を AvalonEdit に反映する。

```text
Document.IsEditable
 ↓
TextEditor.IsReadOnly
```

Document の `IsEditable` は以下から決まる。

```text
IsEditable =
    !IsReadOnly
    && !IsLoading
    && !IsSaving
```

したがって、

* ReadOnly
* Loading
* Saving

のいずれかで編集不可とする。

処理完了後は Document の状態変化に応じて Editor の ReadOnly 状態を更新する。

ViewModel 側で `TextEditor.IsReadOnly` を直接操作しない。

---

# 11. Caret / Selection

Caret と Selection は Presentation 側の状態とする。

Domain の Document に Caret 位置や Selection 範囲を保持しない。

StatusBar の行・列表示は AvalonEdit の Caret 位置から取得する。

表示は 1 始まりとする。

例：

```text
Ln 1, Col 1
Ln 10, Col 25
```

Document の保存や検索などの Application 処理は Caret / Selection に依存しない。

---

# 12. SearchHighlightBehavior

検索結果の視覚的な表示は Presentation 層で行う。

Behavior は DocumentTabViewModel の、

```text
SearchResults
CurrentSearchIndex
```

を参照する。

処理：

```text
SearchResults
 ↓
全検索結果をハイライト
 ↓
CurrentSearchIndex
 ↓
現在検索結果を追加強調
```

SearchService 自体は AvalonEdit を参照しない。

SearchResult に AvalonEdit 固有型を持たせない。

---

# 13. 検索状態と Editor の同期

検索条件変更時：

```text
SearchText / IsRegex
 ↓
SearchTextUseCase
 ↓
ISearchService
 ↓
SearchResults 更新
 ↓
CurrentSearchIndex 更新
 ↓
SearchHighlightBehavior
```

Document の内容が変更された場合は、既存 SearchResults を破棄する。

```text
Document 更新
 ↓
SearchResults = empty
CurrentSearchIndex = -1
 ↓
SearchHighlightBehavior がハイライト解除
```

検索処理そのものを Behavior に実装しない。

---

# 14. Application Exception

Application 層より下で発生する低レベル例外を、そのまま UI に表示しない。

主要な Application Exception：

```text
FileOperationException
EncodingOperationException
DiffOperationException
```

基本的な流れ：

```text
Infrastructure
 ↓
Application Exception
 ↓
MainWindowViewModel
 ↓
IUserNotificationService
 ↓
MessageBox
```

Application 層から WPF の `MessageBox` を直接呼び出さない。

---

# 15. FileOperationException

ファイル読み書きなどの失敗は `FileOperationException` として扱う。

代表例：

```text
ファイルを開けませんでした。
```

```text
ファイルへのアクセスが拒否されました。
```

```text
ファイルを保存できませんでした。
```

System.IO の具体的な例外内容を、そのまま画面へ表示しない。

---

# 16. EncodingOperationException

対応していない文字コードや、指定 Encoding では保存できない文字が存在する場合に使用する。

Open 時：

```text
対応していない文字コードのため、ファイルを開けません。
```

Save 時：

```text
現在の文字コードでは保存できない文字が含まれています。
```

保存時の Encoding エラーでは、Document の内容や `IsModified` を成功状態へ変更しない。

---

# 17. ReadOnly に対するエラー

ReadOnly Document に通常 Save を実行しようとした場合：

```text
読み取り専用ファイルのため保存できません。
```

ただし Save As は可能とする。

Save As 成功後は、

```text
FilePath 更新
FileName 更新
IsReadOnly = false
IsModified = false
```

として、新しいファイルを通常の編集対象にする。

---

# 18. DiffOperationException

Compare / Diff 処理中に発生した Diff 関連エラーは `DiffOperationException` として扱う。

Compare に失敗した場合、

* DiffTab を作成しない
* Workspace に未完成状態を残さない
* ActiveTab を DiffTab に変更しない

とする。

ユーザーには Diff 処理が失敗したことを通知する。

低レベルな DiffPlex 例外を直接表示しない。

---

# 19. Notification Service

Application から UI 通知を抽象化するため、

```text
IUserNotificationService
```

を使用する。

Presentation の実装：

```text
MessageBoxNotificationService
```

Application / UseCase は具体的な WPF MessageBox を参照しない。

通知内容の生成は Application / Presentation の責務を明確に分け、UI Service は通知を表示する役割に限定する。

---

# 20. Confirmation Service

保存確認など、ユーザーの選択が必要な処理には、

```text
IUserConfirmationService
```

を使用する。

Presentation 実装：

```text
MessageBoxConfirmationService
```

保存確認結果：

```text
Save
DontSave
Cancel
```

API：

```text
ConfirmSaveChanges(string documentName)
```

を基本とする。

---

# 21. Close / Exit のエラー境界

未保存 Document の Close：

```text
CloseDocumentUseCase
 ↓
IsModified ?
 ↓
IUserConfirmationService
 ↓
Save / Don't Save / Cancel
```

Save を選択して保存に失敗した場合、Document は閉じない。

Cancel の場合も閉じない。

Exit でも同じ原則を使用する。

複数 Document が未保存の場合はタブ順に処理し、途中の Save が失敗した場合はアプリケーションを終了しない。

---

# 22. ダイアログ処理

File Dialog は、

```text
IFileDialogService
```

で抽象化する。

WPF 実装：

```text
WpfFileDialogService
```

Open / Save As の Cancel は通常のユーザー操作として扱い、エラー通知を表示しない。

Save As の既存ファイル上書き確認は標準 SaveFileDialog の機能を利用する。

アプリ側で同じ上書き確認を二重に行わない。

---

# 23. UI と Application の境界ルール

以下を禁止事項とする。

### ViewModel

以下を直接行わない。

* File.ReadAllBytes 等の直接 File I/O
* DiffPlex API 呼び出し
* WPF MessageBox 呼び出し
* AvalonEdit の検索処理
* Domain に UI 状態を持ち込む

### Application

以下を直接参照しない。

* `Window`
* `MessageBox`
* `TextEditor`
* AvalonEdit の型
* DiffPlex.Wpf の View 型

### Domain

以下を参照しない。

* WPF
* AvalonEdit
* DiffPlex
* File I/O
* MessageBox

Presentation 固有処理は Presentation に閉じ込める。

---

# 24. 実装上の重要ルール

1. Command はユーザー操作の入口とする。
2. Command から直接 Infrastructure API を呼ばない。
3. CanExecute は現在状態から評価する。
4. `CanSave` 等の重複状態を保持しない。
5. Command 状態変更時は `NotifyCanExecuteChanged()` を行う。
6. Edit 操作は WPF / AvalonEdit 標準 RoutedCommand を利用する。
7. Document は AvalonEdit に依存しない。
8. Editor との同期は Presentation Behavior が担当する。
9. プログラム更新とユーザー編集を区別し、初期同期で IsModified を変更しない。
10. ReadOnly / Loading / Saving は Editor を ReadOnly にする。
11. Caret / Selection は Presentation 状態とする。
12. SearchHighlight は Presentation の Behavior が担当する。
13. Application Exception を UI へ直接表示しない。
14. Application から WPF MessageBox を直接呼び出さない。
15. Save / Close / Exit の失敗時は状態を成功扱いに変更しない。
16. Compare 失敗時は DiffTab を作成しない。
17. UI 固有型を Application / Domain へ漏らさない。
18. MVP では非同期 Command を導入しない。
