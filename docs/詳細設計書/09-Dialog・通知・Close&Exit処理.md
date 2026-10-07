# TextWorkbench 詳細設計書 第9部：Dialog・通知・Close/Exit処理

## 1. 本書の対象

本書では、TextWorkbenchで使用するDialog、通知、保存確認、およびDocumentのClose・アプリケーションExit処理について、Presentation層とApplication層の責務を定義する。

特に、WPF固有のDialog処理をApplication層から分離し、UseCaseがUI技術へ依存しない構成を明確にする。

---

## 2. Dialogの基本方針

MVPではWPF標準Dialogを利用する。

Application層から以下のWPF APIを直接呼び出さない。

* MessageBox
* OpenFileDialog
* SaveFileDialog
* Application.Current.Shutdown()

これらはPresentation層で実装する。

Application層との境界には以下のInterfaceを使用する。

```text
IFileDialogService
IUserNotificationService
IUserConfirmationService
```

---

## 3. IFileDialogService

ファイル選択Dialogを抽象化する。

### 3.1 Open

```text
ShowOpenFileDialog() → string?
```

戻り値：

* ファイル選択時：選択されたファイルパス
* Cancel時：`null`

Open DialogではMVPとして`*.*`を使用する。

Cancel時にはエラー通知を行わず、Open処理をそのまま終了する。

### 3.2 Save As

```text
ShowSaveFileDialog(string? initialFilePath) → string?
```

戻り値：

* 保存先選択時：選択されたファイルパス
* Cancel時：`null`

既存ファイルを選択した場合の上書き確認は、WPFのSaveFileDialog標準機能に任せる。

アプリケーション側で同じ確認を二重に行わない。

---

## 4. IUserNotificationService

ユーザーへの通知を抽象化する。

Application層ではWPF MessageBoxを直接呼ばず、`IUserNotificationService`を利用する。

Presentation層に以下の実装を配置する。

```text
MessageBoxNotificationService
```

### 4.1 通知対象

代表的な通知は以下とする。

| 状況           | メッセージ                       |
| ------------ | --------------------------- |
| ファイル読み込み失敗   | ファイルを開けませんでした。              |
| アクセス拒否       | ファイルへのアクセスが拒否されました。         |
| 保存失敗         | ファイルを保存できませんでした。            |
| 読み取り専用       | 読み取り専用ファイルのため保存できません。       |
| 対応外Encoding  | 対応していない文字コードのため、ファイルを開けません。 |
| Encoding変換失敗 | 現在の文字コードでは保存できない文字が含まれています。 |

必要に応じてファイル名をメッセージへ含める。

MVPでは基本的にOKボタンのみとする。

低レベルの`System.IO`例外などをそのままユーザーへ表示しない。

---

## 5. IUserConfirmationService

ユーザーによる保存確認を抽象化する。

Presentation層では、

```text
MessageBoxConfirmationService
```

として実装する。

### 5.1 SaveChangesDecision

保存確認の結果は以下の3種類とする。

```text
Save
DontSave
Cancel
```

### 5.2 API

```text
ConfirmSaveChanges(string documentName)
    → SaveChangesDecision
```

Documentを閉じる際に未保存変更がある場合に使用する。

Save Asの上書き確認には使用しない。

---

## 6. Document Close

Close処理は`CloseDocumentUseCase`が担当する。

基本フローは以下とする。

```text
CloseCommand
    ↓
CloseDocumentUseCase
    ↓
対象DocumentのIsModified確認
    ↓
変更なし ─────────→ Close
    ↓
変更あり
    ↓
IUserConfirmationService
    ├─ Save      → SaveDocumentUseCase → 成功 → Close
    ├─ DontSave  → Close
    └─ Cancel    → Closeしない
```

### 6.1 未変更Document

`IsModified == false`の場合、確認Dialogを表示せず、そのまま閉じる。

### 6.2 Save

`Save`が選択された場合、SaveDocumentUseCaseを実行する。

保存に成功した場合のみCloseする。

保存に失敗した場合はCloseしない。

### 6.3 DontSave

`DontSave`が選択された場合、現在の変更を破棄してCloseする。

この場合、保存処理は実行しない。

### 6.4 Cancel

`Cancel`が選択された場合、Close処理を中止する。

---

## 7. CloseとWorkspace

Close成功後にWorkspaceから対象Tabを削除する。

保存確認や保存処理が失敗した段階ではTabを削除しない。

したがって、

```text
保存確認・保存処理
        ↓
Close成功
        ↓
WorkspaceからTab削除
```

という順序を維持する。

ActiveTabを閉じた場合は、第7部で定義したWorkspaceのActiveTab切替規則を使用する。

---

## 8. Application終了

Application終了は`ExitApplicationUseCase`が保存確認と保存処理を担当する。

終了フローは以下とする。

```text
MainWindow Closing
    ↓
MainWindowViewModel
    ↓
ExitApplicationUseCase
    ↓
bool
    ↓
ClosingEventArgs.Cancel
```

`ExitApplicationUseCase`は終了を許可する場合`true`、キャンセルする場合`false`を返す。

実際の`Application.Shutdown()`はPresentation/WPF側のみが行う。

---

## 9. 未変更Documentのみの場合

Workspace内に未保存変更のあるDocumentが存在しない場合、保存確認を行わず終了を許可する。

```text
未変更Documentのみ
    ↓
ExitApplicationUseCase
    ↓
true
```

---

## 10. 未保存Documentが存在する場合

未保存Documentが1つ以上存在する場合は、一括確認を行う。

確認Dialogでは、未保存Documentの一覧をユーザーへ提示する。

例：

```text
未保存の変更があります。

保存して終了しますか？

未保存の変更があるファイル：
・A.txt
・C.txt

[保存して終了] [保存せず終了] [キャンセル]
```

ここで表示するDocument一覧はWorkspaceのTab順とする。

---

## 11. Save & Exit

「保存して終了」が選択された場合、未保存DocumentをWorkspaceのTab順に順次保存する。

```text
A.txt
 ↓
保存成功
 ↓
C.txt
 ↓
保存成功
 ↓
終了許可
```

途中のDocumentの保存に失敗した場合、その時点で処理を停止する。

```text
A.txt → 成功
C.txt → 失敗
        ↓
      終了しない
```

それまでに保存に成功したDocumentは保存済み状態となる。

失敗したDocumentおよび後続Documentは、その時点の状態を維持する。

---

## 12. 無題DocumentのSave & Exit

未保存変更のある無題Documentを「保存して終了」する場合、保存先が存在しないためSave As処理を行う。

```text
無題Document
    ↓
Save As
    ↓
SaveFileDialog
    ↓
保存先決定
    ↓
保存
```

ユーザーがSaveFileDialogをCancelした場合、そのDocumentの保存は失敗扱いとし、アプリケーション終了を許可しない。

後続Documentの保存も実行しない。

---

## 13. Save & ExitでのSave As失敗

無題Documentの保存先選択をCancelした場合、またはSave Asに失敗した場合は終了しない。

既に保存成功したDocumentの状態はそのまま保持する。

例えば、

```text
A.txt       → 保存成功
無題1       → Save As Cancel
B.txt       → 未処理
```

の場合、

* A.txtは保存済み
* 無題1は未保存
* B.txtは未処理
* Applicationは終了しない

とする。

---

## 14. Saveせず終了

「保存せず終了」が選択された場合、保存処理を行わず終了を許可する。

この場合、未保存変更は破棄される。

```text
DontSave
    ↓
Save処理なし
    ↓
ExitApplicationUseCase → true
```

---

## 15. Exitキャンセル

「キャンセル」が選択された場合は終了を許可しない。

```text
Cancel
    ↓
ExitApplicationUseCase → false
    ↓
ClosingEventArgs.Cancel = true
```

Windowはそのまま表示される。

---

## 16. MainWindow Closing

MainWindowのClosingイベントはPresentation層で受け取る。

ViewはClosingイベントからMainWindowViewModelへ終了処理を依頼する。

概念的には以下とする。

```text
MainWindow.Closing
    ↓
MainWindowViewModel.Exit()
    ↓
ExitApplicationUseCase
    ↓
true / false
    ↓
ClosingEventArgs.Cancel
```

View内に保存確認やDocument状態判定のロジックを実装しない。

---

## 17. Application.Shutdownの責務

`Application.Current.Shutdown()`はPresentation/WPF側のみで実行する。

ExitApplicationUseCaseはApplication終了そのものを実行しない。

UseCaseは、

```text
「終了してよいか」
```

を判断して結果を返す。

これによりApplication層はWPF Applicationオブジェクトへ依存しない。

---

## 18. Dialogキャンセルの扱い

DialogのCancelはエラーではない。

以下の場合は通知を表示しない。

* OpenFileDialogのCancel
* SaveFileDialogのCancel
* Close確認でCancel
* Exit確認でCancel

ただし、処理結果としてClose/Exitは実行しない。

---

## 19. ErrorとCancelの区別

エラーとユーザーキャンセルを明確に区別する。

### Cancel

ユーザーが意図的に処理を中止した状態。

例：

```text
OpenFileDialog → Cancel
SaveFileDialog → Cancel
保存確認 → Cancel
```

通知不要。

### Error

処理を継続できない状態。

例：

```text
ファイル読み込み失敗
Encoding変換失敗
ファイル保存失敗
```

`IUserNotificationService`によってユーザーへ通知する。

---

## 20. Application / Presentationの責務

### Application

以下を担当する。

* Closeの処理フロー
* Exitの処理フロー
* IsModifiedの確認
* 保存確認結果の処理
* SaveDocumentUseCaseの呼び出し
* 終了可否の判断

### Presentation

以下を担当する。

* OpenFileDialog
* SaveFileDialog
* MessageBox
* MainWindow Closingイベント
* ClosingEventArgs.Cancel
* Application.Shutdown

この境界を維持し、Application層へWPF APIを持ち込まない。

---

## 21. 実装時の確認ポイント

AIコーディングツールへ実装を指示する際は、以下を確認する。

* IFileDialogServiceを使用してOpen/Save As Dialogを抽象化する
* IUserNotificationServiceを使用してエラー通知を抽象化する
* IUserConfirmationServiceを使用してClose/Exitの保存確認を抽象化する
* Save Asの上書き確認を独自実装しない
* DialogのCancelをエラーとして扱わない
* Close前に保存確認を完了する
* Save失敗時はCloseしない
* DontSaveでは保存処理を行わない
* Exitでは未保存Documentを一括確認する
* Save & ExitはWorkspaceのTab順に保存する
* 保存途中で失敗した場合はそこで停止する
* 既に保存成功したDocumentの状態は維持する
* 無題Documentでは必要に応じてSave Asを実行する
* Save AsのCancel時はExitしない
* ExitApplicationUseCaseは終了可否をboolで返す
* Application層からApplication.Current.Shutdown()を呼ばない
* MainWindowのClosingイベントから終了処理を開始する
* ClosingEventArgs.CancelはPresentation層で設定する

以上をDialog・通知・Close/Exit処理の基本契約とする。
