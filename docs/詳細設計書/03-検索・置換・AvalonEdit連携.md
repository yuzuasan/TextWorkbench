# TextWorkbench 詳細設計書 第3部：検索・置換・AvalonEdit連携

## 1. 本書の対象

本書では、TextWorkbench MVPにおける以下の詳細設計を定義する。

* 検索状態
* 通常検索
* 正規表現検索
* Find Next / Previous
* 検索結果ハイライト
* AvalonEditとの検索連携
* Replace Next
* Replace All
* 正規表現置換
* 検索結果の無効化
* Search / Replace UI状態

検索処理そのものはWPF / AvalonEditから独立させる。

---

# 2. 検索アーキテクチャ

検索は以下の責務分担とする。

```text
Document.CurrentContent
        ↓
ISearchService
        ↓
SearchResult[]
        ↓
DocumentTabViewModel
        ↓
SearchHighlightBehavior
        ↓
AvalonEdit
```

各責務：

### SearchService

* 通常検索
* Regex検索
* 検索結果の位置計算

### DocumentTabViewModel

* 検索条件の保持
* SearchResultsの保持
* CurrentSearchIndexの保持
* Find Next / Previous
* Replace状態の保持

### SearchHighlightBehavior

* AvalonEditへのハイライト表示
* Current resultのSelection
* Current resultへのスクロール
* ハイライト解除

SearchServiceはUI状態やDocument状態を保持しない。

---

# 3. Search状態

## 3.1 DocumentTabViewModel

各DocumentTabViewModelが自身の検索状態を保持する。

```text
DocumentTabViewModel
├─ SearchText
├─ IsRegex
├─ SearchResults
├─ CurrentSearchIndex
└─ ReplaceNextStartIndex
```

MVPでは以下を持たない。

```text
IsCaseSensitive
IsSelectionOnly
```

大文字・小文字の区別を切り替えるUIは実装しない。

選択範囲のみを対象とする検索も実装しない。

## 3.2 SearchResult

```text
SearchResult
├─ StartIndex
└─ Length
```

`StartIndex`はDocument.CurrentContentに対する0始まりの文字位置とする。

`Length`は一致した文字列の長さを表す。

Capture groupは個別のSearchResultとして扱わない。

---

# 4. ISearchService

## 4.1 インターフェース

```text
ISearchService
    Search(content, condition)
        → IReadOnlyList<SearchResult>
```

検索条件は、検索文字列とRegex使用有無を含むモデルとして扱う。

SearchServiceは以下を知らない。

* DocumentTabViewModel
* Workspace
* AvalonEdit
* WPF
* SearchPanel
* CurrentSearchIndex
* ハイライト状態

## 4.2 通常検索

RegexがOFFの場合は文字列検索を行う。

大文字・小文字区別を指定するUIはMVPでは設けない。

検索結果は全件返す。

例：

```text
content:
abc test abc test

search:
test

result:
[0] StartIndex=4,  Length=4
[1] StartIndex=13, Length=4
```

## 4.3 空検索

検索文字列が空の場合：

```text
SearchResults = []
CurrentSearchIndex = -1
```

検索結果ハイライトも表示しない。

## 4.4 検索条件変更

SearchTextまたはIsRegexが変更された場合：

```text
検索条件変更
 ↓
SearchService.Search()
 ↓
SearchResults更新
 ↓
CurrentSearchIndex = -1
 ↓
SearchHighlightBehavior更新
```

条件変更前のCurrentSearchIndexを引き継がない。

---

# 5. Find Next / Previous

## 5.1 循環動作

検索結果は循環して移動する。

例：

```text
[0] 10-14
[1] 30-34
[2] 50-54
```

Find Next：

```text
0 → 1 → 2 → 0
```

Find Previous：

```text
0 → 2 → 1 → 0
```

## 5.2 初回検索

初期状態：

```text
CurrentSearchIndex = -1
```

Find Next：

```text
-1 → 0
```

Find Previous：

```text
-1 → 最後の結果
```

## 5.3 0件

検索結果が0件の場合：

```text
SearchResults = []
CurrentSearchIndex = -1
```

Find Next / Previousを実行しても状態を変更しない。

---

# 6. 検索結果の表示

検索結果はすべてハイライトする。

ただしCurrentSearchIndexが示す結果は、通常の検索結果とは別の強調表示とする。

```text
SearchResults
    ↓
全件ハイライト

CurrentSearchIndex
    ↓
Current resultを強調
```

具体的な色・Brush等はViewのUI実装で決定する。

Application層にBrushやAvalonEdit型を持ち込まない。

---

# 7. AvalonEdit連携

## 7.1 SearchHighlightBehavior

AvalonEdit固有処理はPresentation層のBehaviorへ隔離する。

```text
SearchHighlightBehavior
├─ SearchResultsを監視
├─ CurrentSearchIndexを監視
├─ ハイライト生成
├─ Current result選択
├─ スクロール
└─ ハイライト解除
```

SearchServiceからAvalonEdit APIを呼び出してはならない。

## 7.2 Current result

CurrentSearchIndexが変更された場合：

```text
CurrentSearchIndex変更
 ↓
SearchHighlightBehavior
 ↓
Current resultをAvalonEdit Selectionへ設定
 ↓
対象位置へスクロール
```

Current resultはAvalonEditのSelectionとして扱う。

スクロールは対象位置が画面上で見える程度とし、画面中央への配置は必須としない。

---

# 8. Document編集と検索状態

検索結果は文字位置を保持しているため、Document.CurrentContentが変更された時点で無効になる。

```text
Document変更
 ↓
SearchResults = []
CurrentSearchIndex = -1
 ↓
SearchHighlightBehavior
 ↓
ハイライト解除
```

MVPでは検索結果のインクリメンタル更新を行わない。

編集後に検索結果を自動補正してはならない。

ユーザーが再度検索した時点で検索結果を再生成する。

---

# 9. Searchパネルの状態

SearchReplacePanelはMainWindowの共通UIとする。

ただし検索状態はDocumentTabViewModelごとに保持する。

例：

```text
Document A
SearchText = "abc"

Document B
SearchText = "test"
```

AからBへ切り替えた場合、Bの検索状態を表示する。

Aの検索状態は保持する。

Documentタブを閉じた場合、そのDocumentTabViewModelとともに検索状態も破棄する。

## 9.1 Searchパネルを閉じる

検索パネルを閉じても検索状態自体は保持する。

ただし画面上のハイライトは消去する。

```text
Panel Close
 ↓
SearchText等は保持
 ↓
表示ハイライトのみ解除
```

再表示時：

* 前回のSearchTextを表示可能
* 文書が編集されていなければ検索結果を利用可能
* 文書編集済みなら既に検索結果が破棄されている

DiffTabをActiveにした場合はSearchパネルを非表示とする。

DiffTabからDocumentTabへ戻った場合、そのDocumentの検索状態を再表示可能とする。

---

# 10. Regex検索

## 10.1 実装

Regex ONの場合は.NET Regexを使用する。

```text
RegexOptions.Multiline
```

を常時有効とする。

その他のRegexOptionsはデフォルトとする。

全文を1つの文字列としてRegexへ渡す。

そのため`^` / `$`はMultilineにより行単位で評価される。

## 10.2 Capture group

Capture groupは検索結果として分離しない。

例えばRegexが複数のCapture groupを持っていても、1回のMatchを1つのSearchResultとして扱う。

```text
Match
 ↓
SearchResult
```

## 10.3 Invalid Regex

正規表現が不正でRegex生成に失敗した場合：

```text
通知
 ↓
SearchResults = []
CurrentSearchIndex = -1
```

Document内容は変更しない。

検索処理そのものをアプリケーションエラーとして終了させない。

## 10.4 Zero-length Match

zero-length matchは有効な検索結果として許可する。

Match列挙時には検索位置を安全に進め、同じ位置を永久に列挙する無限ループを防止する。

---

# 11. Replace

## 11.1 責務分離

検索と置換はServiceを分離する。

```text
ISearchService
IReplaceService
```

IReplaceService：

```text
ReplaceFirst(...)
ReplaceAll(...)
```

置換処理自体もAvalonEditから独立させる。

## 11.2 ReplaceResult

```text
ReplaceResult
├─ Content
├─ ReplacementCount
├─ FirstMatchStartIndex : int?
├─ FirstMatchLength : int?
└─ ReplacementLength : int?
```

`ReplaceNextStartIndex`はReplaceResultに含めない。

これはDocumentTabViewModelが保持するUI操作状態とする。

---

# 12. Replace Next

## 12.1 基本仕様

Replace Nextは1回につき1件のみ置換する。

選択状態を必要としない。

最初のReplace Nextは文書先頭位置0から検索する。

```text
ReplaceNextStartIndex = 0
```

ReplaceFirstには検索開始位置を渡す。

```text
ReplaceFirst(
    content,
    condition,
    replacement,
    startIndex
)
```

`startIndex`以降で最初に一致したものを置換する。

## 12.2 Replace成功

置換成功時：

```text
Document.CurrentContent更新
IsModified = true

SearchResults = []
CurrentSearchIndex = -1
```

自動再検索は行わない。

次回Replace Nextの検索開始位置は、挿入したreplacementの末尾からとする。

```text
置換後
 ↓
replacement末尾
 ↓
次回ReplaceNextStartIndex
```

## 12.3 Zero-length Match

zero-length matchを置換する場合も、同じ位置を次回の検索開始位置として再利用しない。

特に文末位置のzero-length matchについては、同一位置を繰り返し処理しないよう進行位置を保証する。

## 12.4 Replace Next 0件

対象が存在しない場合：

```text
Document変更なし
IsModified変更なし
```

MessageBox等の通知は表示しない。

---

# 13. Regex Replace

## 13.1 Regex OFF

Regex OFFの場合、replacement文字列はliteral文字列として扱う。

`$1`等を特殊展開しない。

## 13.2 Regex ON

Regex ONの場合は.NETの標準replacement semanticsを使用する。

Capture replacementを利用可能とする。

例：

```text
$1
${name}
```

## 13.3 Invalid Regex

Regexが不正の場合：

```text
通知
Document変更なし
```

Search / Replace処理後にDocumentを変更してはならない。

---

# 14. Replace All

## 14.1 対象

Replace AllはDocument.CurrentContent全体を対象とする。

現在のReplaceNextStartIndexやCurrentSearchIndexには依存しない。

## 14.2 0件

対象が0件の場合：

```text
Document変更なし
```

置換結果を通知する。

```text
0件置換しました
```

## 14.3 1件以上

1件以上置換した場合：

```text
Document.CurrentContent更新
IsModified = true
SearchResults = []
CurrentSearchIndex = -1
ReplaceNextStartIndex = 0
```

自動再検索は行わない。

置換完了時に件数を通知する。

```text
3件置換しました
```

---

# 15. Replace後の検索状態

Replace Next / Replace Allのどちらでも、既存SearchResultsをそのまま維持しない。

置換によって文字位置が変化するためである。

```text
Replace
 ↓
SearchResults破棄
CurrentSearchIndex = -1
 ↓
必要に応じてユーザーが再検索
```

自動再検索は行わない。

SearchTextおよびIsRegexは保持する。

---

# 16. Search / Replace UI

検索結果件数は`SearchResults.Count`から算出する。

結果あり：

```text
3件
```

0件：

```text
0件
```

空検索時は件数表示なしでもよい。

Replace Next：

* 成功時MessageBoxなし
* 0件時MessageBoxなし

Replace All：

```text
3件置換しました
```

0件の場合も：

```text
0件置換しました
```

とする。

---

# 17. ViewModelのCommand

DocumentTabViewModelまたはMainWindowViewModelから以下の操作を公開する。

```text
Search
FindNext
FindPrevious
ReplaceNext
ReplaceAll
```

具体的なCommand配置は既存のMainWindow Command構成に合わせる。

Commandから直接AvalonEdit APIを呼び出さない。

検索・置換結果をViewModel状態へ反映し、AvalonEditへの反映はBehaviorへ任せる。

---

# 18. 実装上の禁止事項

以下を実装ルールとして固定する。

1. SearchServiceからAvalonEditを操作しない。
2. ReplaceServiceからAvalonEditを操作しない。
3. SearchServiceにCurrentSearchIndexを持たせない。
4. SearchServiceにSearchText等のUI状態を保持させない。
5. SearchResultにAvalonEditの型を持たせない。
6. DocumentへSearchResultsを持たせない。
7. DocumentへUndo/Redo Stackを持たせない。
8. 文書編集後に古いSearchResultsを使用しない。
9. Replace後に古いSearchResultsを使用しない。
10. Invalid RegexでDocument内容を変更しない。
11. RegexのCapture groupを個別SearchResultとして扱わない。
12. zero-length matchによる無限ループを発生させない。
13. MVPで大文字・小文字区別指定を追加しない。
14. MVPで選択範囲のみ検索を追加しない。
15. Replace後に自動再検索しない。

---

# 19. テスト観点

## SearchService

* 通常検索
* 複数結果
* 0件
* 空検索
* Regex検索
* Multiline Regex
* Capture group
* Invalid Regex
* zero-length match
* StartIndex / Lengthの正確性

## Find Next / Previous

* 初期値-1からNext
* 初期値-1からPrevious
* Nextの循環
* Previousの循環
* 0件
* 1件
* 複数件

## Document編集

* 編集時にSearchResultsが破棄される
* CurrentSearchIndexが-1になる
* ハイライトが解除される
* 再検索で最新Document内容から結果を生成する

## Replace Next

* 先頭から置換
* startIndex以降の最初の一致
* 1回につき1件
* 連続Replace Next
* Replace後SearchResults破棄
* ReplaceNextStartIndex更新
* 0件
* Regex Replace
* Capture replacement
* Invalid Regex
* zero-length match
* 文末zero-length match

## Replace All

* 全件置換
* 0件
* IsModified更新
* SearchResults破棄
* CurrentSearchIndex=-1
* ReplaceNextStartIndex=0
* Regex Replace
* Capture replacement
* Invalid Regex

## Presentation

* 全検索結果ハイライト
* Current resultの強調表示
* Current resultをAvalonEdit Selectionへ設定
* Find Next / Previousで対象位置へスクロール
* SearchPanelを閉じてもSearchTextを保持
* SearchPanelを閉じた状態ではハイライトを解除
* Document切替時にDocumentごとの検索状態を表示
* DiffTabではSearchPanelを非表示
