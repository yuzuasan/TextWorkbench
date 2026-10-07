# TextWorkbench 詳細設計書 第6部：サービス・UseCase・Composition Root・テスト設計

## 1. 本書の対象

本書では、TextWorkbench の Application / Infrastructure / Presentation の実装境界を明確にする。

対象：

* Service Interface
* Application Service
* UseCase
* UseCase 依存関係
* Composition Root
* プロジェクト構成
* テストプロジェクト
* Fake Service による Application テスト
* レイヤー間の依存ルール

MVP では DI コンテナを導入せず、`App` を Composition Root として手動でオブジェクトを構築する。

---

# 2. Service と UseCase の責務

## 2.1 UseCase

UseCase はユーザー操作単位の処理フローを担当する。

例：

```text
OpenDocumentUseCase
SaveDocumentUseCase
CloseDocumentUseCase
CompareDocumentsUseCase
```

UseCase は複数の Service を組み合わせて、一連の処理を完了させる。

UseCase 自体は WPF の View や MessageBox を直接操作しない。

---

## 2.2 Service

Service は、UseCase から利用される再利用可能な具体的処理を担当する。

例：

```text
IEncodingService
ILineEndingService
ISearchService
IReplaceService
IDiffService
```

Service は、担当範囲を超えた UI 状態やユーザー操作を管理しない。

---

# 3. Service Interface 一覧

MVP では以下の Interface を使用する。

```text
IFileService
IFileDialogService
IEncodingService
ILineEndingService
ISearchService
IReplaceService
IDiffService
IDiffEngineAdapter
IUserNotificationService
IUserConfirmationService
ITextInfoService
```

実装配置：

```text
Infrastructure
├─ FileService
├─ EncodingService
├─ LineEndingService
└─ DiffPlexAdapter

Application
├─ SearchService
├─ ReplaceService
├─ DiffService
└─ TextInfoService

Presentation
├─ WpfFileDialogService
├─ MessageBoxNotificationService
└─ MessageBoxConfirmationService
```

---

# 4. IFileService

ファイルの低レベル I/O を抽象化する。

概念 API：

```text
Exists(string filePath)
ReadAllBytes(string filePath)
WriteAllBytes(string filePath, byte[] bytes)
```

`IFileService` は Encoding や LineEnding を解釈しない。

Read は byte 配列を取得するだけとする。

Write も byte 配列をそのまま保存する。

ファイル操作で発生した低レベル例外は、Infrastructure 内で Application が扱える例外へ変換する。

---

# 5. IFileDialogService

WPF の File Dialog を抽象化する。

概念 API：

```text
ShowOpenFileDialog() → string?
ShowSaveFileDialog(string? initialFilePath) → string?
```

Open の標準フィルターは MVP では `*.*` とする。

ユーザーが Cancel した場合は `null` を返す。

Cancel はエラーではないため、通知を表示しない。

実装：

```text
WpfFileDialogService
```

は Presentation 層に配置する。

---

# 6. IEncodingService

文字コードの Decode / Encode を担当する。

概念 API：

```text
Decode(byte[] bytes, TextEncoding encoding)
    → DecodedText

Encode(string text, TextEncoding encoding)
    → byte[]
```

MVP 対応 Encoding：

```text
Utf8
Utf8Bom
ShiftJis
```

Decode 時の Encoding 判定は EncodingService が担当する。

保存時は strict な Encoding を使用し、表現できない文字を `?` などへ置換しない。

Encode できない場合は `EncodingOperationException` とする。

Shift-JIS を利用するため、`CodePagesEncodingProvider` は Composition Root で一度登録する。

---

# 7. ILineEndingService

改行コードの検出・正規化・変換を担当する。

対応：

```text
CrLf
Lf
Cr
```

概念処理：

```text
Detect(string text)
NormalizeToLf(string text)
ConvertFromLf(string text, LineEnding lineEnding)
```

Document 内部では常に LF を使用する。

Open：

```text
読み込み
 ↓
LineEnding 検出
 ↓
LF Normalize
```

Save：

```text
Document.Content
 ↓
Document.LineEnding に変換
 ↓
Encoding
 ↓
WriteAllBytes
```

混在改行の場合は、最初に検出した改行コードを Document.LineEnding とする。

改行がない場合は CRLF とする。

---

# 8. ISearchService

検索処理のみを担当する。

概念 API：

```text
Search(
    string content,
    string searchText,
    bool isRegex
)
→ IReadOnlyList<SearchResult>
```

`SearchResult`：

```text
SearchResult
├─ StartIndex
└─ Length
```

Service は以下を保持しない。

* CurrentSearchIndex
* 検索パネル状態
* AvalonEdit
* UI ハイライト
* スクロール状態

Regex 検索では .NET Regex を利用し、`RegexOptions.Multiline` を常時指定する。

不正 Regex は Application 側で扱える例外へ変換する。

---

# 9. IReplaceService

置換処理を担当する。

概念 API：

```text
ReplaceFirst(
    string content,
    string searchText,
    string replacement,
    bool isRegex,
    int startIndex
)
→ ReplaceResult

ReplaceAll(
    string content,
    string searchText,
    string replacement,
    bool isRegex
)
→ ReplaceResult
```

`ReplaceResult`：

```text
ReplaceResult
├─ Content
├─ ReplacementCount
├─ FirstMatchStartIndex : int?
├─ FirstMatchLength : int?
└─ ReplacementLength : int?
```

`ReplaceNextStartIndex` は Service に保持せず、`DocumentTabViewModel` が管理する。

Regex OFF では replacement を literal として扱う。

Regex ON では .NET 標準の replacement semantics を使用する。

例：

```text
$1
${name}
```

などの capture replacement を利用できる。

---

# 10. IDiffEngineAdapter

DiffPlex との境界となる Infrastructure Interface。

概念 API：

```text
Compare(
    string leftText,
    string rightText
)
→ DiffEngineResult
```

`DiffEngineResult` は TextWorkbench 独自モデルとする。

DiffPlex の具体的な型を Domain / Application / ViewModel に返さない。

Adapter は以下を担当しない。

* DiffCount
* CurrentDiffIndex
* Next / Previous
* UI 強調
* StatusBar

これらは TextWorkbench 側で管理する。

---

# 11. IDiffService

Application 側の Diff 処理を抽象化する。

責務：

* DiffEngineResult の解釈
* DiffResult への変換
* DiffLine / DiffSegment の意味付け
* DiffCount の算出

処理：

```text
IDiffEngineAdapter
 ↓
DiffEngineResult
 ↓
DiffService
 ↓
DiffResult
```

DiffPlex の型はこの境界から外へ漏らさない。

---

# 12. ITextInfoService

現在の Document 内容から TextInfo を生成する。

概念 API：

```text
GetTextInfo(Document document)
    → TextInfo
```

TextInfo：

```text
TextInfo
├─ LineCount
├─ CharacterCount
├─ ByteCount
├─ Encoding
└─ LineEnding
```

ByteCount は実保存時と同じ変換経路を利用する。

```text
Content
 ↓
LineEnding変換
 ↓
Encoding
 ↓
byte[].Length
```

CharacterCount は Unicode code point 数とする。

---

# 13. IUserNotificationService

Application からユーザー通知を抽象化する。

概念 API：

```text
ShowInformation(string message)
ShowWarning(string message)
ShowError(string message)
```

Presentation 実装：

```text
MessageBoxNotificationService
```

Application 層は WPF `MessageBox` を直接参照しない。

---

# 14. IUserConfirmationService

ユーザー確認を抽象化する。

代表 API：

```text
ConfirmSaveChanges(string documentName)
    → SaveChangesDecision
```

結果：

```text
Save
DontSave
Cancel
```

Presentation 実装：

```text
MessageBoxConfirmationService
```

Save As の上書き確認は `IUserConfirmationService` ではなく、標準 SaveFileDialog の機能を利用する。

---

# 15. UseCase 一覧

MVP の UseCase：

```text
NewDocumentUseCase
OpenDocumentUseCase
SaveDocumentUseCase
SaveDocumentAsUseCase
CloseDocumentUseCase
ExitApplicationUseCase
SearchTextUseCase
ReplaceTextUseCase
CompareDocumentsUseCase
GetTextInfoUseCase
```

UseCase は「ユーザー操作の処理単位」として整理する。

---

# 16. UseCase 依存関係

## Open

```text
OpenDocumentUseCase
 ├─ IFileDialogService
 ├─ IFileService
 ├─ IEncodingService
 └─ ILineEndingService
```

## Save

```text
SaveDocumentUseCase
 ├─ IFileService
 ├─ IEncodingService
 └─ ILineEndingService
```

## Save As

```text
SaveDocumentAsUseCase
 ├─ IFileDialogService
 ├─ IFileService
 ├─ IEncodingService
 └─ ILineEndingService
```

## Close

```text
CloseDocumentUseCase
 ├─ IUserConfirmationService
 └─ SaveDocumentUseCase
```

## Exit

```text
ExitApplicationUseCase
 ├─ IUserConfirmationService
 └─ SaveDocumentUseCase
```

## Search

```text
SearchTextUseCase
 └─ ISearchService
```

## Replace

```text
ReplaceTextUseCase
 └─ IReplaceService
```

## Compare

```text
CompareDocumentsUseCase
 ├─ IFileService
 ├─ IEncodingService
 ├─ ILineEndingService
 └─ IDiffService
```

## Text Info

```text
GetTextInfoUseCase
 └─ ITextInfoService
```

---

# 17. Composition Root

MVP では DI コンテナを使用しない。

`App` を Composition Root とし、アプリケーション起動時に依存オブジェクトを手動構築する。

構築順序：

```text
App
 ↓
Infrastructure Services
 ↓
Application Services
 ↓
Presentation Services
 ↓
UseCases
 ↓
Workspace
 ↓
MainWindowViewModel
 ↓
MainWindow
```

具体的な構成：

```text
App
├─ Infrastructure
│  ├─ FileService
│  ├─ EncodingService
│  ├─ LineEndingService
│  └─ DiffPlexAdapter
│
├─ Application Services
│  ├─ SearchService
│  ├─ ReplaceService
│  ├─ DiffService
│  └─ TextInfoService
│
├─ Presentation Services
│  ├─ WpfFileDialogService
│  ├─ MessageBoxNotificationService
│  └─ MessageBoxConfirmationService
│
├─ UseCases
├─ Workspace
└─ MainWindowViewModel
     ↓
   MainWindow
```

---

# 18. Composition Root の責務

App は依存関係を組み立てる。

ただし、以下の処理ロジックを App に実装しない。

* Open 処理
* Save 処理
* Search 処理
* Replace 処理
* Diff 計算
* Close 判定

App の責務は「誰が誰に依存するか」を確定することに限定する。

また、Shift-JIS 対応に必要な `CodePagesEncodingProvider` の登録をアプリ起動時に一度だけ行う。

---

# 19. プロジェクト構成

ソリューション：

```text
TextWorkbench.sln
├─ TextWorkbench
└─ TextWorkbench.Tests
```

アプリケーション側：

```text
TextWorkbench
├─ Presentation
│  ├─ Views
│  ├─ ViewModels
│  ├─ Behaviors
│  └─ Services
│
├─ Application
│  ├─ Services
│  ├─ Interfaces
│  └─ UseCases
│
├─ Domain
│  ├─ Models
│  └─ Enums
│
└─ Infrastructure
   ├─ File
   ├─ Encoding
   ├─ LineEnding
   └─ Diff
```

---

# 20. レイヤー依存ルール

基本的な責務境界：

```text
Presentation
    ↓
Application
    ↓
Domain
```

Infrastructure は Application が定義した Interface の実装を提供する。

重要ルール：

1. Domain は WPF に依存しない。
2. Domain は AvalonEdit に依存しない。
3. Domain は DiffPlex に依存しない。
4. Application は WPF View を参照しない。
5. Application は AvalonEdit を参照しない。
6. Application は DiffPlex の具体型を参照しない。
7. ViewModel は直接 File I/O を行わない。
8. ViewModel は直接 DiffPlex API を呼ばない。
9. Presentation 固有処理は Presentation に閉じ込める。
10. Infrastructure の具体実装は Composition Root で注入する。

---

# 21. テストプロジェクト

テストプロジェクト：

```text
TextWorkbench.Tests
```

テストでは、可能な箇所について Fake Service を利用する。

目的は、WPF や実ファイルシステムなどの外部要因と Application ロジックを分離して検証することである。

---

# 22. Domain テスト

Document を中心に以下を検証する。

* 新規 Document の初期値
* FilePath
* Encoding
* LineEnding
* IsModified
* IsReadOnly
* IsLoading
* IsSaving
* IsEditable
* 編集時の状態変更
* Save 成功時の状態変更
* 状態遷移時のガード条件

特に、

```text
IsEditable =
    !IsReadOnly
    && !IsLoading
    && !IsSaving
```

を保証する。

---

# 23. Application テスト

UseCase は Fake Service を注入して処理フローを検証する。

例：

```text
OpenDocumentUseCase
 ├─ FakeFileService
 ├─ FakeEncodingService
 └─ FakeLineEndingService
```

検証対象：

* 正常系
* Service 失敗時
* Exception 変換
* Document 状態
* Tab 作成前の失敗
* Save 失敗時の状態保持

Open / Diff については、処理成功前に Workspace 用の結果を確定しないことを確認する。

---

# 24. Search / Replace テスト

最低限、以下を検証する。

### Search

* 通常検索
* 複数一致
* 0件
* Regex
* Invalid Regex
* Multiline
* Capture group を個別結果にしない
* zero-length match

### Replace

* Replace Next
* Replace All
* 連続 Replace Next
* 0件
* Regex Replace
* capture replacement
* Invalid Regex
* zero-length match
* IsModified 更新
* Replace 後の SearchResults 無効化
* 自動再検索を行わないこと

特に zero-length match では、無限ループや同一位置の永久再置換が発生しないことを検証する。

---

# 25. Diff テスト

DiffService は `Fake IDiffEngineAdapter` を使用して単体テストする。

検証対象：

* DiffEngineResult → DiffResult
* DiffLine
* DiffSegment
* Unchanged
* Added
* Deleted
* Changed
* DiffCount
* null 行番号
* 文字単位 Segment
* 行単位 Navigation 用モデル

DiffTabViewModel では、

* CurrentDiffIndex 初期値
* NextDiff
* PreviousDiff
* 境界での no-op
* 差分 0 件
* 非循環動作

を検証する。

DiffPlexAdapter 自体については、実際の DiffPlex との統合テストを別途行う。

---

# 26. TextInfo テスト

以下を最低限検証する。

### LineCount

```text
""           → 1
"abc"        → 1
"abc\n"      → 2
"abc\nxyz"   → 2
"abc\nxyz\n" → 3
"a\n\nb"     → 3
```

### CharacterCount

Unicode code point 単位であることを検証する。

### ByteCount

以下を検証する。

* UTF-8
* UTF-8 BOM
* Shift-JIS
* CRLF
* LF
* CR

実際の保存時に生成される byte 数と一致することを確認する。

---

# 27. Presentation テスト

主な対象：

```text
MainWindowViewModel
Workspace
DocumentTabViewModel
DiffTabViewModel
```

検証項目：

* ActiveTab の切り替え
* Tab 追加 / 削除
* 同一ファイル二重 Open 防止
* Command CanExecute
* Command 状態更新
* Document / Diff の切り替え
* Search state の Document 単位保持
* Diff navigation

WPF Behavior のテストは必要最小限とする。

---

# 28. 実装時の優先テスト

実装途中で特に優先するテストは以下。

1. Document 状態
2. Open / Save / Save As
3. Close / Exit
4. Search
5. Replace
6. DiffService
7. Diff Navigation
8. TextInfo
9. MainWindowViewModel
10. AvalonEdit Behavior

UI の見た目より、まず Domain / Application の状態遷移と責務境界をテストで固定する。

---

# 29. 最終的な実装原則

以下を MVP の実装ルールとして固定する。

1. UseCase はユーザー操作単位の処理フローを担当する。
2. Service は個別の再利用可能な処理を担当する。
3. Infrastructure は外部 API / OS / ファイル等との境界を担当する。
4. Presentation は WPF / AvalonEdit 固有処理を担当する。
5. App は Composition Root として依存関係を構築する。
6. MVP では DI コンテナを導入しない。
7. Interface によって Application と Infrastructure を分離する。
8. DiffPlex の具体型を Application / Domain に漏らさない。
9. WPF の具体型を Application / Domain に漏らさない。
10. File I/O は `IFileService` 経由とする。
11. Encoding は `IEncodingService` 経由とする。
12. LineEnding は `ILineEndingService` 経由とする。
13. Search / Replace はそれぞれ独立した Service とする。
14. Diff 計算は `IDiffEngineAdapter` を境界とする。
15. UI 通知は `IUserNotificationService` 経由とする。
16. ユーザー確認は `IUserConfirmationService` 経由とする。
17. テストでは Fake Service を利用して Application 処理を独立検証する。
18. Open / Diff は成功するまで Workspace に Tab を追加しない。
19. Save / Close / Exit の失敗時に成功状態へ遷移させない。
20. Domain / Application のテストを優先し、WPF 固有テストは必要最小限とする。
