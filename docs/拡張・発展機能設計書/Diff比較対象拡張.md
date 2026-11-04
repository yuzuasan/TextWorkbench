# TextWorkbench Diff比較対象拡張 設計書

## 1. 本書の対象

本書では、既存の Compare / Diff 機能を拡張し、比較対象として以下を選択できるようにするための設計を定義する。

- 開いている Document
- 保存済みファイル

既存の Diff 処理、DiffResult、DiffPlex Adapter、Diff Navigation などの基本仕様は、既存の詳細設計書「Text Info・Compare/Diff」の設計を引き継ぐ。

本書は新規の拡張設計であり、既存の詳細設計書を直接書き換えるものではない。拡張後の仕様については本書を参照する。

## 2. 基本方針

### 2.1 比較対象

| 比較元 | 比較に使用する内容 |
|---|---|
| 開いている Document | 比較実行時点の Document.CurrentContent |
| 保存済みファイル | 比較実行時点でディスクから読み込んだ内容 |

開いている Document は、未保存の編集内容を含む現在の内容を比較対象とする。

一度も保存されていない Document（無題 Document）も比較対象にできる。

保存済みファイルは、開いているかどうかにかかわらずファイル選択ダイアログから選択できる。すでに Document として開いているファイルを保存済みファイルとして選択した場合も、Document の編集内容ではなくディスク上の内容を使用する。

同一の実ファイルを「開いている Document」と「保存済みファイル」の両方として選択することを許可する。この場合、エディタ上の未保存内容とディスク上の内容を比較できる。

### 2.2 左右の選択

左右の比較元は独立して選択する。

- 左右それぞれに、開いている Document または保存済みファイルを指定できる。
- 左右で同じ比較元を指定することを許可する。
- 左右のどちらか一方だけを先に選択してもよい。
- 両方が選択されるまで「比較」ボタンを無効にする。
- 初期状態では左右とも未選択とする。初期 Document や初期ファイルは自動選択しない。

## 3. 比較元選択ダイアログ

### 3.1 表示方式

Compare コマンドから比較元選択ダイアログを表示する。

ダイアログはモーダル表示（ShowDialog）とする。ダイアログ表示中はメインウィンドウを操作できない。

ダイアログには左側と右側の選択領域を1つずつ配置する。各領域は独立した選択状態を保持する。

### 3.2 各側の選択 UI

各側に以下を配置する。

- 開いている Document を選択する ComboBox
- 保存済みファイルを選択するボタン
- 現在選択されている比較元の表示領域

Document は ComboBox から選択する。保存済みファイルは既存のファイル選択ダイアログを使用する。

比較元の種類は、選択内容の表示で明確に区別する。

表示例：

- 開いているドキュメント：無題1
- 開いているドキュメント：sample.txt
- 保存済みファイル：sample.txt

保存済み Document または保存済みファイルについては、フルパスをツールチップ等で確認できるようにする。無題 Document はパスを持たないため、Document の表示タイトルを表示する。

未保存変更の有無を示す表示は追加しない。

### 3.3 Document の選択可能条件

Workspace 内の DocumentTabViewModel を選択候補とする。

以下の状態の Document は選択不可とする。

- IsLoading が true
- IsSaving が true

選択候補が存在しない場合は Document ComboBox を無効にする。空状態を説明する専用メッセージは表示しない。保存済みファイルの選択は引き続き可能とする。

本機能では非同期の読み込み・保存処理自体は追加しない。IsLoading / IsSaving の状態を選択可否の判定に使用する。

### 3.4 選択の置き換え

各側では、最後に選択した比較元の種類を有効な選択とする。

- Document を選択した場合、その側の保存済みファイル選択を解除する。
- 保存済みファイルを選択した場合、その側の Document 選択を解除する。
- ファイル選択ダイアログをキャンセルした場合、既存の選択状態を維持する。

左右の選択状態は互いに影響しない。

### 3.5 ダイアログの確定・キャンセル

- 「比較」ボタンは左右両方の選択が有効な場合のみ有効とする。
- 「比較」を押すと選択内容を確定し、ダイアログを閉じる。
- キャンセルまたは閉じる操作では比較を実行しない。
- ダイアログを閉じた後に CompareDocumentsUseCase を実行する。

## 4. ComparisonSource

既存の ComparisonSource を拡張し、比較元の種類を識別できるようにする。

概念モデル：

```text
ComparisonSource
├─ SourceType
└─ SourceIdentifier
```

SourceType は以下を扱う。

| SourceType | 意味 | SourceIdentifier |
|---|---|---|
| File | 保存済みファイル | 正規化された絶対ファイルパス |
| OpenDocument | 開いている Document | Document.Id |

同じファイルを開いている Document として指定した場合は OpenDocument、ファイル選択ダイアログから指定した場合は File として扱う。ファイルパスが一致していても SourceType によって比較元を区別する。

## 5. IDocumentContentProvider

### 5.1 目的

Application 層の CompareDocumentsUseCase が、比較実行時点の Document を取得するための Application Interface を設ける。

### 5.2 インターフェース

Application 層に IDocumentContentProvider を配置する。

取得結果は Domain の Document オブジェクトとする。

概念 API：

```csharp
Document GetDocument(Guid documentId);
```

実際の戻り値・例外仕様は既存コードの命名および例外方針に合わせる。

### 5.3 実装と依存関係

IDocumentContentProvider の実装は Presentation 側に配置し、Workspace から Document.Id に一致する DocumentTabViewModel を検索して、その Document を返す。

Application は IDocumentContentProvider と Domain の Document のみに依存する。Application から Workspace、DocumentTabViewModel、WPF Window など Presentation 固有型を参照しない。

Composition Root で実装を生成し、CompareDocumentsUseCase に注入する。

### 5.4 最新内容の取得

Document の内容を選択ダイアログ表示時や ComboBox 選択時にコピーして保持してはならない。

CompareDocumentsUseCase の実行時に IDocumentContentProvider から Document を取得し、その時点の CurrentContent、Encoding、LineEnding を使用する。

これにより、比較元を選択した後、ダイアログを確定するまでの間に Document の内容が更新された場合でも、比較実行時点の最新内容を比較できる。

本設計ではダイアログをモーダル表示するため、ダイアログ表示中にメインウィンドウから対象タブを閉じる操作は発生しない。

## 6. CompareDocumentsUseCase

既存の CompareDocumentsUseCase を拡張し、SourceType に応じて比較元を取得する。

### 6.1 File の場合

既存処理を維持する。

1. SourceIdentifier をファイルパスとして検証する。
2. ファイルパスを正規化する。
3. FileService でディスク上のファイルを読み込む。
4. EncodingService で Decode する。
5. LineEndingService で内部表現の LF に正規化する。
6. ファイル名、パス、Encoding、Content を比較元情報として保持する。

開いている Document が同じパスを持っていても、Document.CurrentContent は使用しない。

### 6.2 OpenDocument の場合

1. SourceIdentifier から Document.Id を取得する。
2. IDocumentContentProvider で比較実行時点の Document を取得する。
3. Document.CurrentContent を比較内容として取得する。
4. Document.Encoding と Document.LineEnding を比較元情報として取得する。
5. Document の表示名を取得する。保存済み Document は FileName、無題 Document は表示タイトルを使用する。
6. 保存済み Document の FilePath は比較元情報として保持する。無題 Document の FilePath は null とする。

Document.CurrentContent はすでに LF に正規化されているため、ファイル読み込み時のような再 Decode や LF Normalize は行わない。

### 6.3 共通処理

左右それぞれの比較元を取得した後は、既存の DiffService を使用して比較し、DiffSession を生成する。

左右いずれかの取得、読み込み、Decode、比較処理に失敗した場合は DiffSession / DiffTab を作成しない。エラーは既存の Application Exception および通知機構に従って扱う。

## 7. DiffSession.DiffSource

DiffSession は従来どおり比較実行時点のスナップショットを保持する。比較元が Document の場合も同様であり、比較完了後の Document 編集によって既存 DiffSession を更新しない。

無題 Document を扱えるよう、DiffSource の FilePath は null を許容する。

概念モデル：

```text
DiffSource
├─ SourceType
├─ SourceIdentifier
├─ FilePath?
├─ DisplayName
├─ Encoding
└─ Content
```

各プロパティの意味：

| プロパティ | 意味 |
|---|---|
| SourceType | File または OpenDocument |
| SourceIdentifier | File は正規化された絶対パス、OpenDocument は Document.Id |
| FilePath | 保存済みファイルまたは保存済み Document のパス。無題 Document は null |
| DisplayName | 画面表示に使用するファイル名または Document 表示タイトル |
| Encoding | 比較元の Encoding |
| Content | 比較実行時点の LF 正規化済み内容 |

DiffSession は比較実行時点の値を保持する。後から元ファイルが変更されたり、元 Document が編集・保存・閉じられたりしても、既存 DiffTab の内容・表示情報は変更しない。

## 8. DiffView のヘッダー表示

DiffView の左右ヘッダーには、比較元の種類と表示名を日本語で表示する。

| 比較元 | ヘッダー表示 |
|---|---|
| 無題 Document | 開いているドキュメント：無題1 |
| 保存済み Document | 開いているドキュメント：sample.txt |
| 保存済みファイル | 保存済みファイル：sample.txt |

SourceType の内部値は英語のままとし、表示用文字列への変換は Presentation 側で行う。

保存済み Document および保存済みファイルのフルパスは、ヘッダーのツールチップ等から確認できるようにする。無題 Document にはパスがないため、表示タイトルのみとする。

未保存変更の有無を示す表示は追加しない。

## 9. Compare の実行フロー

```text
CompareCommand
    ↓
比較元選択ダイアログをモーダル表示
    ↓
左右の比較元を選択
    ↓
両側の選択を確定
    ↓
ダイアログを閉じる
    ↓
CompareDocumentsUseCase.Execute
    ├─ File → ディスクから読み込み
    └─ OpenDocument → IDocumentContentProvider から Document を取得
    ↓
左右の比較元情報を生成
    ↓
DiffService
    ↓
DiffSession（比較実行時点の Snapshot）
    ↓
DiffTabViewModel
    ↓
Workspace.AddTab
```

比較に成功した場合のみ DiffTab を Workspace に追加する。失敗時は既存の通知機構でエラーを通知する。

## 10. 既存設計からの変更点

| 対象 | 既存 | 拡張後 |
|---|---|---|
| SourceType | File | File / OpenDocument |
| 比較元選択 | 左右それぞれファイル選択 | 左右それぞれ Document またはファイル |
| Document の内容 | 比較対象外 | 比較実行時点の CurrentContent |
| 無題 Document | 比較対象外 | 比較可能 |
| DiffSource.FilePath | 必須 | null 許容 |
| DiffSource の識別 | ファイルパス | SourceType と SourceIdentifier |
| DiffView ヘッダー | ファイル名中心 | 比較元の種類を日本語で併記 |

## 11. 対象外

本拡張では以下を実装しない。

- Document の非同期読み込み・非同期保存処理
- Compare の非同期実行、進捗表示、キャンセル
- DiffSession の永続化、キャッシュ、自動更新
- Document の変更状態（未保存変更あり／なし）のヘッダー表示
- 比較元選択ダイアログ内での Document 内容編集
- 既存 DiffTab の比較元再読み込み・再比較機能

## 12. 実装時の注意事項

1. Application 層から Presentation 層の Workspace / ViewModel / WPF 型を参照しない。
2. IDocumentContentProvider は Application Interface とし、実装は Presentation 側に置く。
3. 比較実行時点の Document を取得し、選択時点の内容をスナップショットとして使わない。
4. File は常にディスク上の内容を使用し、同一パスの Document が開いていても置き換えない。
5. Document.CurrentContent は LF 正規化済みであることを前提とし、再 Decode / Normalize しない。
6. 無題 Document の FilePath は null として扱い、パスを前提とした処理を追加しない。
7. DiffSession は比較実行時点のスナップショットとし、後続の編集・保存・タブ終了から独立させる。
8. SourceType の内部値と UI の日本語表示を分離する。
9. 左右の比較元選択状態は独立して管理する。
10. ファイル選択キャンセル時は、その側の現在の選択状態を維持する。
11. 単体テストの追加は現時点では保留する。