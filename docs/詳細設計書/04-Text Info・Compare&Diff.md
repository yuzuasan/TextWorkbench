# TextWorkbench 詳細設計書 第4部：Text Info・Compare/Diff

## 1. 本書の対象

本書では、以下の詳細設計を定義する。

* Text Info
* Compare 入力
* Diff 処理
* Diff モデル
* DiffPlex Adapter
* DiffSession
* DiffTabViewModel
* Diff Navigation
* Diff 表示
* Diff 処理中の状態
* Diff StatusBar

本書では、DiffPlex の具体的な API 呼び出し方法そのものではなく、TextWorkbench 内部の責務境界を定義する。

---

# 2. Text Info

Text Info は、現在アクティブな Document の全文情報を表示する機能である。

表示場所は StatusBar ではなく、`File → Text Info` から開くダイアログとする。

対象は全文のみとし、選択範囲の情報表示は MVP では実装しない。

処理フロー：

```text
GetTextInfoUseCase
    ↓
ITextInfoService
    ↓
TextInfo
    ↓
Presentation Dialog
```

## 2.1 TextInfo

```text
TextInfo
├─ LineCount
├─ CharacterCount
├─ ByteCount
├─ Encoding
└─ LineEnding
```

`TextInfo` は UI 用の文字列を保持せず、値として情報を返す。

表示用のフォーマットは Presentation 側で行う。

## 2.2 CharacterCount

CharacterCount は **Unicode code point 数** とする。

.NET の UTF-16 `string.Length` をそのまま使用するとは限らない。

実装では Unicode code point 単位で数える。

## 2.3 ByteCount

ByteCount は、現在の Document を実際に保存した場合のバイト数と一致するよう計算する。

処理：

```text
Document.Content
    ↓
Document.LineEnding に変換
    ↓
Document.Encoding で Encode
    ↓
byte[].Length
```

したがって、Document 内部で LF に統一されていることによる差異を考慮する。

UTF-8 BOM の場合は BOM の 3 bytes を含める。

Encode に失敗した場合は、通常の保存処理と同様に Encoding 関連の Application Exception として扱う。

## 2.4 LineCount

LineCount は論理行数とする。

代表例：

```text
""           → 1
"abc"        → 1
"abc\n"      → 2
"abc\nxyz"   → 2
"abc\nxyz\n" → 3
"a\n\nb"     → 3
```

空文書も 1 行として扱う。

内部改行は LF に統一されているため、LineCount の計算は LF を基準に行う。

## 2.5 ITextInfoService

Application から利用するインターフェース：

```text
ITextInfoService
```

主な責務：

* Document の内容から TextInfo を生成する
* CharacterCount を計算する
* ByteCount を計算する
* LineCount を計算する
* Encoding / LineEnding を取得する

WPF の Dialog や View は参照しない。

---

# 3. Compare の基本方針

MVP の Compare は、**保存済みファイル A と B を比較する機能**とする。

比較対象は現在開いている Document の状態ではなく、比較開始時点のディスク上のファイルである。

したがって、開いている Document に未保存変更が存在していても、その変更内容は Compare 対象にならない。

比較対象ファイルは、Document として開いている必要もない。

処理フロー：

```text
CompareCommand
    ↓
File A 選択
    ↓
File B 選択
    ↓
FileService
    ↓
EncodingService
    ↓
LF Normalize
    ↓
DiffService
    ↓
DiffResult / DiffSession
    ↓
DiffTab
    ↓
ActiveTab
```

A または B の読み込み・Decode に失敗した場合、DiffTab は作成しない。

## 3.1 同一ファイル

同一ファイル同士の比較は許可する。

同一ファイルを比較した場合、通常は差分 0 件となる。

同じファイルペアを再度 Compare した場合でも、既存の DiffTab を再利用しない。

毎回、新しい DiffTab を作成する。

---

# 4. ComparisonSource

Compare 処理では、入力元を抽象化するため `ComparisonSource` を使用する。

```text
ComparisonSource
├─ SourceType
└─ SourceIdentifier
```

MVP では以下のみを使用する。

```text
SourceType = File
SourceIdentifier = FilePath
```

将来的に以下のような入力元を追加できる構造とする。

```text
OpenDocument
DroppedFile
```

CompareDocumentsUseCase は、ファイルダイアログそのものではなく ComparisonSource を入力として処理できる設計とする。

---

# 5. DiffSession

DiffTab は、Compare 開始時点のスナップショットを保持する。

```text
DiffSession
├─ LeftSource
│   ├─ FilePath
│   ├─ DisplayName
│   ├─ Encoding
│   └─ Content
│
├─ RightSource
│   ├─ FilePath
│   ├─ DisplayName
│   ├─ Encoding
│   └─ Content
│
└─ Result
```

`Content` は比較開始時に LF へ正規化された内容とする。

Compare 完了後に元ファイルが変更されても、既存の DiffTab は変更されない。

また、開いている Document の内容が変更されても DiffTab は変更されない。

DiffSession はメモリ上だけに保持する。

MVP では以下を実装しない。

* 永続化
* キャッシュ
* Compare 結果の自動更新

DiffTab を閉じた時点で、その DiffSession は不要になる。

---

# 6. Diff の正式モデル

MVP の Diff は、**文字単位 Diff を正式な差分モデル**とする。

ただし、画面上の対応行やナビゲーションでは行構造を利用する。

処理構造：

```text
DiffPlex
    ↓
IDiffEngineAdapter
    ↓
DiffEngineResult
    ↓
DiffService
    ↓
DiffResult
```

DiffPlex の API 型を Domain / Application 側へ直接漏らさない。

---

# 7. DiffResult

TextWorkbench 独自の Diff モデルを使用する。

```text
DiffResult
├─ LeftFileName
├─ RightFileName
├─ Lines : IReadOnlyList<DiffLine>
└─ DiffCount
```

## 7.1 DiffLine

```text
DiffLine
├─ LeftLineNumber : int?
├─ RightLineNumber : int?
├─ Type : DiffLineType
├─ LeftSegments
└─ RightSegments
```

`DiffLineType`：

```text
Unchanged
Added
Deleted
Changed
```

## 7.2 DiffSegment

```text
DiffSegment
├─ Text
└─ Type : DiffSegmentType
```

`DiffSegmentType`：

```text
Unchanged
Added
Deleted
```

`Changed` は DiffSegment には存在しない。

つまり、Changed は行レベルの状態であり、その行内部の文字差分は `Added / Deleted / Unchanged` の Segment で表現する。

---

# 8. DiffLine の意味

## Unchanged

左右の対応行が存在し、内容が同じ。

## Changed

左右の対応行が存在するが、内容が異なる。

## Added

右側にのみ存在する行。

```text
LeftLineNumber = null
RightLineNumber = N
```

## Deleted

左側にのみ存在する行。

```text
LeftLineNumber = N
RightLineNumber = null
```

null 側は画面上では Placeholder として扱う。

Placeholder は表示専用であり、元ファイルや Document の内容を変更しない。

---

# 9. DiffCount

DiffCount は文字単位の変更数ではない。

以下を満たす DiffLine の数とする。

```text
DiffLine.Type != Unchanged
```

したがって DiffCount は「差分行グループ数」を意味する。

例：

```text
Unchanged
Changed
Changed
Added
Unchanged
```

の場合、

```text
DiffCount = 3
```

となる。

---

# 10. DiffService

Application 側に `IDiffService` / `DiffService` を配置する。

主な責務：

* DiffEngineResult の解釈
* DiffEngineResult → DiffResult 変換
* Diff semantics の確定
* DiffCount の計算

DiffService は DiffPlex の具体的な API 型に依存しない。

DiffPlex との境界は `IDiffEngineAdapter` とする。

---

# 11. IDiffEngineAdapter

Infrastructure に以下を配置する。

```text
IDiffEngineAdapter
```

概念的な API：

```text
Compare(
    string leftText,
    string rightText
)
→ DiffEngineResult
```

`DiffEngineResult` も TextWorkbench 独自モデルとする。

```text
DiffEngineResult
└─ Lines
   └─ DiffEngineLine
      ├─ LeftLineNumber
      ├─ RightLineNumber
      ├─ Type
      ├─ LeftSegments
      └─ RightSegments
```

DiffPlex の型をこの境界より外へ漏らさない。

Adapter は TextWorkbench における以下の意味を持たない。

* DiffCount
* CurrentDiff
* Next / Previous
* UI 上の強調
* StatusBar 表示

それらは Application / Presentation 側の責務とする。

---

# 12. DiffPlex.Wpf の利用

Diff の中央表示には DiffPlex.Wpf の `SideBySideDiffViewer` を利用する。

DiffPlex.Wpf は表示エンジンとして扱う。

Presentation では DiffPlex.Wpf を利用して以下を実現する。

* 左右ペイン表示
* 文字単位の差分表示
* 通常の差分ハイライト
* 左右スクロール同期

一方、Diff の計算 API は Infrastructure Adapter に限定する。

色などの表示設定はコードへ RGB 値を直接記述せず、WPF Resource として管理する。

---

# 13. DiffTabViewModel

```text
DiffTabViewModel
├─ DiffSession
├─ CurrentDiffIndex
├─ DiffCount
├─ NextDiffCommand
└─ PreviousDiffCommand
```

FileName や Encoding を別状態として複製しない。

必要な表示情報は DiffSession から read-only forwarding property として公開してよい。

DiffTabViewModel は DiffPlex の型を保持しない。

---

# 14. Diff Navigation

Navigation の対象は、差分が存在する DiffLine のみとする。

```text
DiffLine.Type != Unchanged
```

`CurrentDiffIndex` は、**現在選択・強調されている差分が、全差分の何番目であるか**を表す。

値は以下の意味を持つ。

```text
-1       = 現在の差分が未選択
0        = 先頭の差分
1        = 2番目の差分
2        = 3番目の差分
...
N-1      = N番目の差分
```

したがって `CurrentDiffIndex + 1` が、ユーザーに表示する「現在の差分番号」となる。

例：

```text
DiffCount = 5
CurrentDiffIndex = 2
```

の場合、

```text
現在の差分 = 3番目
全差分数   = 5件
```

となる。

UI 上では、例えば以下のように表示できる。

```text
差分 3 / 5
```

ここで、

* `3` = 現在選択されている差分が全差分の3番目
* `5` = 全差分数（DiffCount）

を意味する。

## Next

```text
-1 → 0
0 → 1
...
N-2 → N-1
N-1 → N-1
```

最後に到達した後は no-op。

## Previous

```text
-1 → N-1
N-1 → N-2
...
1 → 0
0 → 0
```

最初に到達した後は no-op。

差分が 0 件の場合は no-op とする。

Unchanged の DiffLine は CurrentDiffIndex に含めない。

---

# 15. Current Diff の表示

Next / Previous Diff 実行時には、対象 DiffLine を現在差分として扱う。

Presentation 側で以下を行う。

* 左右両ペインを対象差分付近へスクロール
* 現在差分を追加強調

通常の差分ハイライトと Current Diff の強調表示は分離する。

Added / Deleted により片側の行番号が null となる場合でも、左右の同期基準は DiffLine の表示行構造を使用する。

---

# 16. Diff 処理中

MainWindowViewModel に以下の状態を持つ。

```text
IsDiffProcessing
```

Compare 開始から完了まで `true` とする。

表示例：

```text
A.txt ↔ B.txt | 比較中...
```

MVP では以下を実装しない。

* 進捗バー
* キャンセル
* 非同期 Compare

Compare 成功後に初めて DiffTab を作成する。

つまり、

```text
Compare開始
    ↓
ファイル読み込み
    ↓
Decode
    ↓
Diff計算
    ↓
DiffSession生成
    ↓
DiffTab作成
```

の順序とする。

処理途中で未完成の DiffTab を Workspace に追加しない。

---

# 17. Diff StatusBar

DiffTab の StatusBar には Encoding を含める。

通常時の表示例：

```text
比較: A.txt (UTF-8) ↔ B.txt (Shift-JIS) | 差分 3 / 10
```

ここで、

* `3` = **現在選択・強調されている差分が、全差分の何番目か**
* `10` = **全差分数（DiffCount）**

とする。

つまり、上記の場合は、

```text
現在の差分 = 3番目
全差分数   = 10件
```

を意味する。

現在差分が未選択の場合は、`3 / 10` のような現在位置を表示できないため、例えば以下とする。

```text
比較: A.txt (UTF-8) ↔ B.txt (Shift-JIS) | 差分 10件
```

または UI 実装上、現在位置部分を `- / 10` としてもよい。

この表示形式は Presentation 側の表示仕様として統一する。

比較中は以下のように表示する。

```text
A.txt ↔ B.txt | 比較中...
```

Encoding の表示名は通常の Document StatusBar と同じ表記規則を使用する。

```text
Utf8     → UTF-8
Utf8Bom  → UTF-8 BOM
ShiftJis → Shift-JIS
```

---

# 18. Compare の責務分担

## MainWindowViewModel

担当：

* CompareCommand
* NextDiffCommand
* PreviousDiffCommand
* `IsDiffProcessing`
* Workspace への DiffTab 追加
* ActiveTab 更新

Diff 計算ロジックは持たない。

## CompareDocumentsUseCase

担当：

* ComparisonSource の処理
* A/B ファイル読み込み
* Encoding 判定
* Decode
* LF Normalize
* DiffService 呼び出し
* DiffSession 作成
* 成功結果の返却

## DiffService

担当：

* DiffEngineResult → DiffResult
* Diff semantics
* DiffCount

## DiffTabViewModel

担当：

* DiffSession
* CurrentDiffIndex
* DiffCount
* Next / Previous
* 現在差分の状態通知

DiffPlex API は使用しない。

## DiffView

担当：

* DiffPlex.Wpf Viewer
* 表示
* スクロール
* Current Diff 強調
* 現在差分番号 / 全差分数の表示

---

# 19. Diff の重要な実装ルール

以下を実装上の必須ルールとする。

1. Compare 対象はディスク上の保存済みファイル。
2. 未保存の Document 変更は Compare に反映しない。
3. A/B の読み込みに失敗した場合、DiffTab を作成しない。
4. 比較途中の未完成 DiffTab を Workspace に追加しない。
5. DiffSession は比較開始時点の Snapshot とする。
6. 元ファイルの後続変更で既存 DiffTab を更新しない。
7. 同じファイルペアでも Compare のたびに新しい DiffTab を作る。
8. DiffPlex の計算 API は Infrastructure Adapter に閉じ込める。
9. DiffPlex 型を Domain / Application / ViewModel に漏らさない。
10. DiffCount は差分文字数ではなく差分 DiffLine 数とする。
11. Navigation の単位は差分 DiffLine。
12. `CurrentDiffIndex` は現在選択されている差分の位置を **0-based index** で保持する。
13. ユーザー向けの現在差分番号は `CurrentDiffIndex + 1` とする。
14. Navigation は循環しない。
15. Added / Deleted の null 行番号は表示用 Placeholder として扱う。
16. Current Diff の強調と通常差分の強調を分離する。
17. DiffTab は元 Document の編集状態とは独立する。
18. Diff の表示設定値をコードへ RGB 値として直接記述しない。
