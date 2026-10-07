# TextWorkbench 詳細設計書 第2部：ファイルI/O・Encoding・LineEnding・Open/Save/Save As

## 1. 本書の対象

本書では、TextWorkbench MVPにおける以下の詳細設計を定義する。

* ファイルI/O
* ファイルダイアログ
* Encoding判定・変換
* LineEnding判定・変換
* Open
* Save
* Save As
* ReadOnly
* ファイルI/Oエラー処理

対象は通常サイズのテキストファイルとする。Open / SaveはMVPでは同期処理を基本とし、大容量ファイル向けの非同期化は将来検討とする。

---

# 2. File Service

## 2.1 IFileService

ファイルシステムへのアクセスはInfrastructure層のIFileServiceに隔離する。

```text
IFileService
├─ bool Exists(string filePath)
├─ FileReadResult ReadAllBytes(string filePath)
└─ void WriteAllBytes(string filePath, byte[] bytes)
```

ViewModelおよびDocumentからSystem.IOを直接利用してはならない。

## 2.2 FileReadResult

```text
FileReadResult
├─ Content
└─ IsReadOnly
```

`Content`は読み込んだファイルのbyte列を保持する。

`IsReadOnly`は読み込み対象ファイルのReadOnly状態を表す。

InfrastructureではSystem.IO等の低レベル例外をApplication向け例外へ変換する。

代表的なApplication例外：

```text
FileOperationException
EncodingOperationException
```

低レベルSystem.IO例外をそのままUIへ表示しない。

## 2.3 通常Save時のExists確認

通常Saveでは、保存前に`Exists()`を必須で呼び出さない。

```text
Document
 ↓
現在のFilePath
 ↓
Encode
 ↓
WriteAllBytes
```

そのため、Documentを開いた後に外部からファイルが削除されても、現在Documentの内容を使って通常Saveで再作成できる。

---

# 3. File Dialog

## 3.1 IFileDialogService

PresentationとファイルダイアログのWPF APIを分離する。

```text
IFileDialogService
├─ string? ShowOpenFileDialog()
└─ string? ShowSaveFileDialog(string? initialFilePath)
```

WPF実装では以下を利用する。

```text
ShowOpenFileDialog()
    → OpenFileDialog

ShowSaveFileDialog()
    → SaveFileDialog
```

## 3.2 Open Dialog

Openダイアログは全ファイルを対象とする。

```text
Filter = *.*
```

拡張子による入力制限はMVPでは設けない。

ユーザーがCancelした場合は`null`を返す。

Cancelはエラーではないため、通知を表示しない。

## 3.3 Save Dialog

Save AsではSaveFileDialogの標準上書き確認を利用する。

Application側で独自の上書き確認ダイアログを表示しない。

---

# 4. Encoding

## 4.1 MVP対応Encoding

MVPで扱う文字コードは以下の3種類とする。

```text
TextEncoding
├─ Utf8
├─ Utf8Bom
└─ ShiftJis
```

表示名：

```text
Utf8     → UTF-8
Utf8Bom  → UTF-8 BOM
ShiftJis → Shift-JIS
```

UTF-16 / UTF-32等はMVPでは非対応とする。

## 4.2 IEncodingService

```text
IEncodingService
├─ DecodedText Decode(byte[] bytes)
└─ byte[] Encode(string text, TextEncoding encoding)
```

DecodedText：

```text
DecodedText
├─ Text
└─ Encoding
```

`Decode()`は入力byte列を判定・デコードし、テキストと判定したEncodingを返す。

`Encode()`は指定Encodingへ変換する。

## 4.3 CodePagesEncodingProvider

Shift-JISを利用するため、`CodePagesEncodingProvider`をApp / Composition Rootで一度登録する。

登録処理を各Serviceで個別に実行しない。

## 4.4 読み込み時のEncoding判定

判定順序を固定する。

```text
byte[]
 ↓
BOM確認
 ├─ UTF-8 BOM
 │    ↓
 │  UTF-8 BOM
 │
 ├─ UTF-16 / UTF-32等
 │    ↓
 │  非対応
 │
 └─ BOMなし
      ↓
   strict UTF-8
      ↓ 成功
   UTF-8
      ↓ 失敗
   strict Shift-JIS
      ↓ 成功
   Shift-JIS
      ↓ 失敗
   非対応形式 / 文字コード
```

### 4.4.1 UTF-8 BOM

UTF-8 BOMを検出した場合は`Utf8Bom`として扱う。

### 4.4.2 UTF-8

BOMが存在せず、strict UTF-8として正常にデコードできた場合は`Utf8`とする。

### 4.4.3 Shift-JIS

UTF-8として成立しなかった場合、strict Shift-JISとしてデコードを試行する。

Shift-JIS判定には曖昧性があるため、MVPではヒューリスティックな判定を許容する。

### 4.4.4 空ファイル

0 byteのファイルはUTF-8の空文書として扱う。

```text
Content = ""
Encoding = Utf8
```

### 4.4.5 非対応形式

対応するEncodingとしてデコードできない場合は、対応外形式／文字コードとして扱う。

UTF-16 / UTF-32等の既知の未対応BOMは、Shift-JIS判定より先に非対応として扱う。

MVPでは高度なBinary判定は行わない。

---

# 5. Encoding保存

## 5.1 strict Encoding

保存時は現在DocumentのEncodingを使用する。

Encodingできない文字が存在する場合、文字を`?`等へ置換して保存してはならない。

```text
Encode失敗
 ↓
EncodingOperationException
 ↓
Save失敗
 ↓
IsModified = true
```

代表的な通知：

```text
現在の文字コードでは保存できない文字が含まれています。
```

---

# 6. LineEnding

## 6.1 LineEnding enum

```text
LineEnding
├─ CrLf
├─ Lf
└─ Cr
```

表示名：

```text
CrLf → CRLF
Lf   → LF
Cr   → CR
```

## 6.2 内部LineEnding

Documentの`CurrentContent`は常にLFに正規化して保持する。

```text
CRLF ─┐
LF   ─┼→ CurrentContent = LF
CR   ─┘
```

Documentは元ファイルの改行文字列をそのまま保持しない。

## 6.3 LineEnding検出

読み込み時はテキストを左から走査し、最初に検出した改行コードをDocument.LineEndingとする。

```text
最初の改行 = CRLF
    → LineEnding = CrLf

最初の改行 = LF
    → LineEnding = Lf

最初の改行 = CR
    → LineEnding = Cr
```

改行コードが混在している場合も、最初に検出した形式を採用する。

ファイル内に改行が1つも存在しない場合はCRLFとする。

## 6.4 保存時LineEnding

保存時は内部LFをDocument.LineEndingへ変換する。

```text
CurrentContent(LF)
        ↓
Document.LineEnding
        ↓
Encode
        ↓
WriteAllBytes
```

例：

```text
LineEnding = CrLf
    LF → CRLF

LineEnding = Lf
    LF → LF

LineEnding = Cr
    LF → CR
```

---

# 7. Open処理

## 7.1 UseCase

Openは`OpenDocumentUseCase`が担当する。

依存サービス：

```text
OpenDocumentUseCase
├─ IFileDialogService
├─ IFileService
├─ IEncodingService
└─ ILineEndingService
```

処理フロー：

```text
OpenDocumentUseCase
 ↓
IFileDialogService.ShowOpenFileDialog()
 ↓
ファイルパス取得
 ↓
IFileService.ReadAllBytes()
 ↓
IEncodingService.Decode()
 ↓
ILineEndingService.Detect()
 ↓
内部改行をLFへ正規化
 ↓
Document生成・設定
 ↓
Document.IsModified = false
 ↓
Workspace / Tabへ追加
```

## 7.2 読み込み中のDocument

Open開始時は一時Documentを生成し、`IsLoading=true`とする。

ただし、読み込み完了するまではWorkspaceおよびTabへ追加しない。

```text
Open開始
 ↓
temporary Document
IsLoading = true
 ↓
Read / Decode / LineEnding検出
 ↓
成功
 ↓
IsLoading = false
 ↓
Workspaceへ追加
```

## 7.3 Open失敗

読み込み・Encoding判定・改行処理のいずれかで失敗した場合：

```text
一時Document破棄
Tab作成なし
Application例外をPresentationへ返す
通知表示
```

失敗したファイルのために空のDocument Tabを作成してはならない。

## 7.4 Open成功時のDocument状態

Open成功時：

```text
FilePath      = 正規化済み絶対パス
FileName      = ファイル名
CurrentContent = LF正規化済み内容
Encoding      = 判定結果
LineEnding    = 検出結果
IsModified    = false
IsReadOnly    = ファイル状態
IsLoading     = false
IsSaving      = false
```

---

# 8. Save処理

## 8.1 SaveのCanExecute

通常Save可能条件：

```text
Document != null
&& !Document.IsReadOnly
&& !Document.IsLoading
&& !Document.IsSaving
```

`IsModified`はCanExecute条件に含めない。

したがって、未変更Documentに対してもSave Command自体は実行可能とする。

## 8.2 新規DocumentのSave

`FilePath == null`のDocumentにSaveを実行した場合、Save As相当の処理を行う。

```text
Save
 ↓
FilePathなし
 ↓
Save As
 ↓
SaveFileDialog
```

## 8.3 Save処理フロー

既存FilePathを持つDocument：

```text
SaveDocumentUseCase
 ↓
IsSaving = true
 ↓
CurrentContent
 ↓
LineEnding変換
 ↓
Encoding
 ↓
IFileService.WriteAllBytes()
 ↓
成功
```

成功時：

```text
IsSaving = false
IsModified = false
```

失敗時：

```text
IsSaving = false
IsModified = true
```

保存失敗時にDocumentの内容を破棄したり、FilePathを変更したりしてはならない。

## 8.4 外部変更

MVPでは外部変更監視を行わない。

ファイルが外部で変更されていても、現在Documentが保持している内容を通常Saveでそのまま書き込む。

外部変更確認ダイアログ等は設けない。

---

# 9. Save As処理

## 9.1 CanExecute

Save Asは以下の場合に実行可能とする。

```text
Document != null
&& !Document.IsLoading
&& !Document.IsSaving
```

`IsReadOnly`は条件に含めない。

したがってReadOnly DocumentでもSave As可能とする。

## 9.2 Save Asフロー

```text
SaveDocumentAsUseCase
 ↓
IFileDialogService.ShowSaveFileDialog(currentFilePath)
 ↓
Cancel?
 ├─ Yes → 処理終了
 └─ No
      ↓
新しいFilePathを取得
      ↓
Workspace内の別Documentと重複確認
      ↓
Encode / LineEnding変換
      ↓
WriteAllBytes
      ↓
成功
      ↓
Document.FilePath更新
Document.IsReadOnly = false
Document.IsModified = false
```

## 9.3 Save AsのEncoding / LineEnding

MVPではSave As時にEncodingやLineEndingを選択するUIを設けない。

現在Documentが保持している設定をそのまま使用する。

```text
Document.Encoding
Document.LineEnding
```

## 9.4 Save As先の重複

Save As先が、別のDocument Tabですでに開かれているファイルの場合は拒否する。

比較はDocumentのFilePathと同じ正規化ルールを使用する。

同一Document自身の現在FilePathを指定した場合は「別Document」ではないため、通常の上書き動作を許可する。

## 9.5 Save As成功後

成功時：

```text
FilePath = 新しいパス
FileName = 新しいファイル名
IsReadOnly = false
IsModified = false
```

Tabタイトルも新しいファイル名へ切り替わる。

Save As成功後は編集可能になる。

## 9.6 SaveFileDialogの上書き確認

Save As先に既存ファイルがある場合は、WPF SaveFileDialogの標準上書き確認を利用する。

Application側で別の上書き確認を追加しない。

---

# 10. ReadOnly

ReadOnly Documentの状態：

```text
IsEditable = false
IsSavable = false
Save As = true
```

つまり、

* 編集不可
* 通常Save不可
* Save As可能

とする。

Save As成功後は`IsReadOnly=false`となり、通常の編集・Saveが可能になる。

Loading / Saving中もEditorは編集不可とする。

---

# 11. エラー処理

## 11.1 Application例外

主要なApplication例外：

```text
FileOperationException
EncodingOperationException
```

低レベル例外をUIへ直接表示しない。

## 11.2 代表的な通知

読み込み失敗：

```text
ファイルを開けませんでした。
```

アクセス拒否：

```text
ファイルへのアクセスが拒否されました。
```

保存失敗：

```text
ファイルを保存できませんでした。
```

ReadOnlyで通常Save：

```text
読み取り専用ファイルのため保存できません。
```

対応外Encoding：

```text
対応していない文字コードのため、ファイルを開けません。
```

Encoding変換不能：

```text
現在の文字コードでは保存できない文字が含まれています。
```

可能な場合はファイル名をメッセージに含める。

MVPでは基本的にOKボタンのみとし、Retryや詳細エラー表示は設けない。

## 11.3 UIとの境界

Application層からWPFのMessageBoxを直接呼び出してはならない。

通知は以下のインターフェースを介する。

```text
IUserNotificationService
```

Presentation側の実装：

```text
MessageBoxNotificationService
```

---

# 12. 実装上の禁止事項

以下を実装ルールとして固定する。

1. ViewModelからSystem.IOを直接呼ばない。
2. DocumentからSystem.IOを直接呼ばない。
3. DocumentからWPF APIを呼ばない。
4. Open成功前にWorkspaceへTabを追加しない。
5. Open失敗時に空のTabを残さない。
6. 内部Document内容をCRLF等で保持せず、LFに統一する。
7. Save時に文字コードを勝手に変更しない。
8. Encoding不能文字を置換して保存しない。
9. Save Asで別Documentが開いているFilePathを使用しない。
10. ReadOnly Documentに通常Saveを実行しない。
11. Save As成功前にFilePathを変更しない。
12. Save失敗時にIsModified=falseへ変更しない。
13. Save Asの上書き確認をApplication側で二重実装しない。
14. 外部変更監視をMVPへ持ち込まない。

---

# 13. Phase 2テスト観点

最低限、以下を自動テストまたは手動テストで確認する。

## Open

* UTF-8を正しく読み込める
* UTF-8 BOMを正しく読み込める
* Shift-JISを正しく読み込める
* EncodingがDocumentへ保持される
* CRLF / LF / CRを正しく検出できる
* 混在改行では最初の改行形式を採用する
* 改行なしではCRLFとなる
* 内部内容がLFへ正規化される
* FilePathが正規化済み絶対パスになる
* 読み込み直後IsModified=false
* ReadOnly状態が反映される
* 0 byteファイルを空UTF-8文書として開ける
* 対応外EncodingではTabを作成しない
* Open失敗時にTabを作成しない

## Save

* UTF-8で正しく保存できる
* UTF-8 BOMで正しく保存できる
* Shift-JISで正しく保存できる
* CRLF / LF / CRへ正しく変換して保存できる
* Save成功でIsModified=false
* Save失敗でIsModified=true
* 外部削除されたファイルをSaveで再作成できる
* Encoding不能文字で保存失敗する
* ReadOnlyでは通常Saveできない
* 未変更でもSave可能

## Save As

* 無題DocumentをSave Asできる
* 既存DocumentをSave Asできる
* ReadOnly DocumentをSave Asできる
* Save As成功でFilePathが更新される
* Save As成功でFileNameが更新される
* Save As成功でIsReadOnly=falseになる
* Save As成功でIsModified=falseになる
* Save As成功後に編集可能になる
* 別Documentが開いているFilePathへのSave Asを拒否する
* 同一Document自身のFilePathへのSave Asは許可する
* Cancel時にDocument状態を変更しない

## File I/O

* 存在しないファイルのOpen失敗を通知できる
* アクセス拒否を適切に通知できる
* 書き込み失敗時にDocument状態を維持できる
* 低レベル例外をUIへ直接表示しない
