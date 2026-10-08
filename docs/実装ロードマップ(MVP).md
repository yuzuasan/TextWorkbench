## 全体像

MVPを次の**28ステップ**に分ける。

| Phase | Step | 作業ステップ                         | 完了時の状態              |
| ----- | ---: | ------------------------------ | ------------------- |
| 1     |    1 | WPFプロジェクト・ライブラリ基盤              | ビルド・起動できる           |
| 1     |    2 | Domainモデル基盤                    | Document等が定義される     |
| 1     |    3 | Tab / Workspace基盤              | Tabを追加・切替できる        |
| 1     |    4 | MainWindow基本UI                 | アプリ骨格が見える           |
| 1     |    5 | Command基盤・通知基盤                 | Commandを実行できる       |
| 1     |    6 | Phase 1統合・動作確認                 | 基盤完成                |
| 2     |    7 | File / Encoding / LineEnding基盤 | ファイルを正しく解釈できる       |
| 2     |    8 | New / Open実装                   | 新規Document・ファイルを開ける |
| 2     |    9 | AvalonEdit連携                   | テキストを編集できる          |
| 2     |   10 | Modified状態管理                   | 未保存状態を管理できる         |
| 2     |   11 | Save実装                         | 上書き保存できる            |
| 2     |   12 | Save As実装                      | 別名保存できる             |
| 2     |   13 | Close / 未保存確認                  | Tabを安全に閉じられる        |
| 2     |   14 | Exit処理                         | 未保存を考慮して終了できる       |
| 2     |   15 | Phase 2統合・動作確認                 | テキストエディタ完成          |
| 3     |   16 | SearchService                  | 検索ロジックが動く           |
| 3     |   17 | 検索UI・ハイライト                     | 検索結果が画面に出る          |
| 3     |   18 | Find Next / Previous           | 検索結果を移動できる          |
| 3     |   19 | ReplaceService                 | 置換ロジックが動く           |
| 3     |   20 | Replace / Replace All UI       | 置換できる               |
| 3     |   21 | Text Info                      | テキスト情報を確認できる        |
| 3     |   22 | Regex検索・置換                     | 正規表現が使える            |
| 3     |   23 | Phase 3統合・動作確認                 | 検索・置換・Text Info完成   |
| 4     |   24 | Diffモデル・DiffService            | 差分計算できる             |
| 4     |   25 | DiffPlex Adapter               | DiffPlexと接続できる      |
| 4     |   26 | DiffTab / Diff表示               | 2ファイル比較できる          |
| 4     |   27 | Diff Navigation                | 次/前の差分へ移動できる        |
| 4     |   28 | MVP統合・受入確認                     | TextWorkbench MVP完成 |

---

# Phase 1：アプリ基盤

MVP Phase 1は約5時間で、「今後の機能を追加できるアプリの骨格」を完成させる位置づけです。

### Step 1：WPFプロジェクト・ライブラリ基盤

* WPFプロジェクト作成
* CommunityToolkit.Mvvm
* AvalonEdit
* DiffPlex
* DiffPlex.Wpf
* 基本フォルダ構成
* namespace整理
* Git初期コミット

**完了条件：**

> 空のTextWorkbenchが起動し、ビルドが通る。

---

### Step 2：Domainモデル基盤

実装：

* `Document`
* `Tab`関連の基本モデル/Enum
* `TextEncoding`
* `LineEnding`
* `SearchResult`
* `TextInfo`
* Diff関連モデル

詳細設計ではDocumentをUI/AvalonEditから独立したDomainモデルとして扱います。

**完了条件：**

> UIに依存しない基本モデルがコンパイルでき、単体テストを書ける状態。

---

### Step 3：Tab / Workspace基盤

実装：

* `Workspace`
* `TabViewModel`
* `DocumentTabViewModel`
* `DiffTabViewModel`
* `ObservableCollection<TabViewModel>`
* `ActiveTab`

Tabは `DocumentTabViewModel / DiffTabViewModel` の2種類とする設計です。

**完了条件：**

> プログラムからDocumentTabを追加するとTabとして認識され、ActiveTabを変更できる。

---

### Step 4：MainWindow基本UI

実装：

* MainWindow
* Menu
* TabControl
* StatusBar
* 基本レイアウト
* DocumentTabの仮表示

**完了条件：**

> 「アプリらしい画面」が表示され、Tabを切り替えられる。

---

### Step 5：Command基盤・通知基盤

実装：

* `RelayCommand`
* MainWindowViewModel
* `IUserNotificationService`
* MessageBox実装
* Command CanExecute基盤

CommandはCommunityToolkit.Mvvmの`RelayCommand`を使い、Command自身がI/OやMessageBoxを直接扱わない設計です。

**完了条件：**

> Menu → Command → ViewModel → 処理、という基本経路が動く。

---

### Step 6：Phase 1統合

ここは新機能を増やさず、

* 起動
* Tab追加
* Tab切替
* Tab削除
* Command
* エラー通知

を一通り確認。

**Phase 1完成。**

---

# Phase 2：ファイル・編集

Phase 2終了時点では「普通のテキストエディタとして使える」ことが目標です。

### Step 7：File / Encoding / LineEnding基盤

実装：

* `IFileService`
* `IEncodingService`
* `ILineEndingService`
* `FileService`
* `EncodingService`
* `LineEndingService`
* UTF-8
* UTF-8 BOM
* Shift-JIS
* CRLF / LF / CR

詳細設計でもFile I/O、Encoding、LineEndingを独立Serviceとして扱っています。

**完了条件：**

> バイト列 ⇔ テキスト ⇔ Document内部形式の変換を単体で検証できる。

---

### Step 8：New / Open実装

実装：

* `NewDocumentUseCase`
* Open File Dialog
* `OpenDocumentUseCase`
* 新規Document作成
* ファイル読み込み
* Encoding判定
* 改行コード判定
* ReadOnly判定
* Document生成
* 同一ファイル重複防止

同じファイルを複数Documentとして開かず、既存TabをActiveにする仕様です。

**完了条件：**

> 新規Documentを作成でき、`.txt`を開くと新しいDocumentTabに内容が表示される。

---

### Step 9：AvalonEdit連携

実装：

* DocumentView
* AvalonEdit
* `TextEditorDocumentBehavior`
* Document → Editor
* Editor → Document

AvalonEdit固有処理はPresentation側Behaviorへ閉じ込める設計です。

**完了条件：**

> 開いたファイルを普通に編集できる。

---

### Step 10：Modified状態管理

実装：

* `IsModified`
* `IsEditable`
* TextChanged検知
* Save後のModified解除
* Loading中/保存中の編集禁止
* ReadOnly制御

**完了条件：**

> 編集すると未保存状態になり、保存すると解除される。

---

### Step 11：Save実装

実装：

* SaveDocumentUseCase
* LF → Document.LineEnding
* Encoding
* FileService
* 保存成功/失敗
* ReadOnly制御

保存失敗時は`IsModified`を維持する仕様です。

**完了条件：**

> 開く → 編集 → Ctrl相当のSave → ファイルへ保存、が成立。

---

### Step 12：Save As実装

実装：

* SaveFileDialog
* SaveDocumentAsUseCase
* 新規Documentの保存
* 保存先重複チェック
* ReadOnly DocumentからSave As

ReadOnly DocumentでもSave Asは可能で、成功後は編集可能になる設計です。

**完了条件：**

> 別名保存ができ、保存後は新しいFilePathを持つDocumentになる。

---

### Step 13：Close / 未保存確認

実装：

* CloseDocumentUseCase
* `IUserConfirmationService`
* Save / Don't Save / Cancel
* Save失敗時Close中止

**完了条件：**

> 未保存Documentを閉じようとしたとき、安全に確認・保存・キャンセルできる。

---

### Step 14：Exit処理

実装：

* ExitApplicationUseCase
* MainWindow Closing
* 複数Documentの保存
* Save As Cancel
* 保存途中の失敗
* DiffTabを保存対象外とする

Exit時の保存順序・失敗時の終了中止も詳細設計で明確化されています。

**完了条件：**

> 未保存ファイルを残したまま誤って終了しない。

---

### Step 15：Phase 2統合

ここでは機能追加より、シナリオテスト。

```text
起動
 ↓
新規Document
 ↓
入力
 ↓
Save As
 ↓
編集
 ↓
Save
 ↓
Close
 ↓
Open
```

**Phase 2完成。**

ここで一度Gitタグを打ってもいいと思います。

`mvp-phase2-editor`

---

# Phase 3：検索・置換

検索結果はDomain/Application側では`StartIndex / Length`として扱い、AvalonEditへの表示変換はPresentation側Behaviorが担当する設計です。

### Step 16：SearchService

実装：

* `ISearchService`
* `SearchService`
* 通常検索
* `SearchResult`
* 複数結果
* 境界処理

**完了条件：**

> UIなしで検索結果を取得できる。

---

### Step 17：検索UI・ハイライト

実装：

* Search Panel
* SearchText
* SearchHighlightBehavior
* 全結果ハイライト
* CurrentResult強調

**完了条件：**

> 検索文字列を入力すると、該当箇所がEditor上で視覚的に分かる。

---

### Step 18：Find Next / Previous

実装：

* FindNext
* FindPrevious
* CurrentSearchIndex
* Selection
* Scroll
* 境界処理

**完了条件：**

> 次/前の検索結果へ移動できる。

---

### Step 19：ReplaceService

実装：

* `IReplaceService`
* `ReplaceService`
* ReplaceNext
* ReplaceAll
* 件数

**完了条件：**

> UIなしで安全に置換処理を実行できる。

---

### Step 20：Replace / Replace All UI

実装：

* Replace入力欄
* ReplaceNext
* ReplaceAll
* 置換件数表示

**完了条件：**

> 画面上から1件/全件置換できる。

---

### Step 21：Text Info

実装：

* `ITextInfoService`
* `TextInfoService`
* `GetTextInfoUseCase`
* Text Info Dialog
* LineCount
* CharacterCount
* ByteCount
* Encoding
* LineEnding

Text InfoはDocumentの現在内容に関する情報を表示する。

詳細設計では、Text InfoをStatusBarには表示せず、PresentationのDialogで表示する。

ByteCountは現在のEncodingとLineEndingで保存した場合のバイト数として算出する。

CharacterCountはUnicode code point数、LineCountは論理行数として扱う。

**完了条件：**

> 現在のDocumentについて、行数・文字数・Byte数・Encoding・LineEndingを確認できる。

---

### Step 22：Regex検索・置換

実装：

* Regex検索
* Regex置換
* 不正Regexエラー
* Zero-length match対策
* Capture group

この部分は通常検索とは分けて実装した方が、問題発生時に切り分けやすいです。詳細設計でも不正RegexによってDocumentを変更しないことなどが明記されています。

**完了条件：**

> 正規表現検索・置換が通常検索と同じUIから利用できる。

---

### Step 23：Phase 3統合

シナリオ：

```text
Open
 ↓
Search
 ↓
Find Next
 ↓
Find Previous
 ↓
Replace
 ↓
Replace All
 ↓
Text Info
 ↓
Save
```

**Phase 3完成。**

ここで、

`mvp-phase3-search`

として区切れます。

---

# Phase 4：Diff

DiffはMVPの主役であり、DiffTabとして扱う設計です。基本設計ではMVPの比較対象を主として保存済みFile A / File Bとしています。

### Step 24：Diffモデル・DiffService

実装：

* `DiffResult`
* `DiffLine`
* `DiffSegment`
* `DiffService`
* Diff件数
* CurrentDiffIndex

**完了条件：**

> UIなしでA/BのDiff結果を取得できる。

---

### Step 25：DiffPlex Adapter

実装：

* `IDiffEngineAdapter`
* DiffPlex Adapter
* DiffPlex → 独自Diffモデル変換

DiffPlexのAPIはInfrastructure Adapterへ閉じ込め、Application側はDiffPlex型を直接扱わない方針です。

**完了条件：**

> DiffServiceからDiffPlexの具体APIを意識せずDiff計算できる。

---

### Step 26：DiffTab / Diff表示

実装：

* CompareCommand
* ファイルA/B選択
* 両ファイル読み込み
* DiffSession
* DiffTab
* Side-by-Side表示
* Added / Deleted / Changed

比較結果はDiffSessionとして比較開始時点のSnapshotを保持する設計です。

**完了条件：**

> 2ファイルを選択してDiffTabを開き、左右に比較結果を表示できる。

---

### Step 27：Diff Navigation

実装：

* NextDiff
* PreviousDiff
* CurrentDiffIndex
* 差分位置へのスクロール
* CurrentDiffハイライト
* 先頭/末尾境界

**完了条件：**

> 差分を順番に確認できる。

---

### Step 28：MVP統合・受入確認

最後は新機能を追加せず、最重要シナリオを通します。

```text
起動
 ↓
Open
 ↓
Edit
 ↓
Search
 ↓
Replace
 ↓
Text Info
 ↓
Save
 ↓
Compare
 ↓
Diff
 ↓
Next / Previous Diff
```

さらに、

* Open失敗
* Save失敗
* ReadOnly
* Save As Cancel
* Close Cancel
* Exit Cancel
* 不正Regex
* Diff失敗

など「**失敗したときに状態が壊れないか**」を重点確認します。詳細設計でも、この観点が優先テストとして明示されています。

**ここでMVP完成。**
