# TextWorkbench 基本設計書 第2部：画面・検索/置換・Diff・Command設計

## 1. 本書の位置付け

本書は TextWorkbench の基本設計の第2部として、画面構成、検索・置換、Diff、Command の設計を定義する。

TextWorkbench は高機能なIDEやコードエディタを目指さず、「少し確認する」「少し編集する」「検索・置換する」「2ファイルを比較する」といった日常的なテキスト操作を軽量に行えるツールとする。

画面上では Document と Diff を同一の Workspace 上の「Tab」として扱い、現在アクティブな Tab に対応する View を表示する。

---

## 2. MainWindow 構成

MainWindow は以下の領域から構成する。

```text
+--------------------------------------------------+
| メニュー                                           |
+--------------------------------------------------+
| クイック操作                                       |
+--------------------------------------------------+
| 検索 / 置換領域（必要時のみ表示）                  |
+--------------------------------------------------+
| タブ                                               |
+--------------------------------------------------+
| アクティブビュー                                   |
|                                                  |
| DocumentView / DiffView                           |
|                                                  |
+--------------------------------------------------+
| ステータスバー                                     |
+--------------------------------------------------+
```

### 2.1 メニュー

アプリケーションの主要操作をメニューとして提供する。

MVP で対象とする主な操作は以下。

* 新規作成
* 開く
* 保存
* 名前を付けて保存
* 閉じる
* 終了
* 検索
* 置換
* 比較
* 次の差分
* 前の差分
* 次の検索結果
* 前の検索結果

メニューの各操作は、後述する Command と共通化する。

### 2.2 クイック操作

メニュー操作のうち、頻繁に使用する操作をボタン等で直接実行できる領域とする。

MVP の候補は以下。

* 新規作成
* 開く
* 保存
* 名前を付けて保存
* 検索
* 置換
* 比較

クイック操作は独自に処理を持たず、対応する Command を呼び出す。

### 2.3 タブ

Workspace に登録されたタブを表示する。

タブには以下の2種類がある。

* DocumentTab
* DiffTab

DocumentTab は通常のテキスト編集を行い、DiffTab は2ファイルの比較結果を表示する。

アクティブなタブに対応する View をアクティブビュー領域へ表示する。

### 2.4 ステータスバー

現在の状態を補助的に表示する。

Document では以下を表示対象とする。

* 文字コード
* 改行コード
* 現在行
* 現在列
* 変更状態
* 読み取り専用状態
* 保存・読み込み等の状態

Diff では必要に応じて以下を表示する。

* Diff の現在位置
* 比較対象に関する情報
* その他の Diff 状態

具体的な表示項目・レイアウトは実装時に ViewModel の公開プロパティを基準として決定する。

---

## 3. 検索・置換 UI

検索・置換 UI は MainWindow 内の非モーダル領域として配置する。

初期状態では非表示とし、検索または置換の操作によって表示する。

基本形は以下とする。

```text
検索:   [________________] [正規表現] [←] [→] [×]
置換:   [________________]              [置換] [すべて置換]
```

### 3.1 検索機能

MVP では以下を提供する。

* 文字列検索
* 正規表現検索
* 次の検索結果へ移動
* 前の検索結果へ移動
* 検索結果の全件ハイライト
* 現在選択中の検索結果を強調表示

検索 UI を閉じると、検索用ハイライトも解除する。

検索履歴は MVP の対象外とする。

### 3.2 検索オプション

MVP の検索オプションは以下とする。

```text
検索オプション
├─ 検索文字列
└─ 正規表現を使用するか
```

将来拡張候補として以下を想定する。

* 大文字・小文字を区別する
* 単語単位で検索する
* 文書末尾から文書先頭への折り返し検索を行う
* 検索履歴を保持する
* 検索方向を指定する

MVP では将来拡張候補を実装対象に含めない。

---

## 4. SearchService

検索処理は SearchService に集約し、View や AvalonEdit から独立させる。

```text
SearchService
    ↓
Document.CurrentContent
    ↓
SearchResult[]
```

SearchService は WPF および AvalonEdit に依存しない。

### 4.1 SearchResult

検索結果は以下の情報を持つ。

```text
SearchResult
├─ StartIndex
└─ Length
```

StartIndex は Document.CurrentContent 内の検索開始位置、Length は一致した文字列の長さを表す。

SearchService は検索条件に一致する結果を全件返す。

### 4.2 正規表現

正規表現を使用しない場合は通常の文字列検索、使用する場合は正規表現検索として処理する。

正規表現の具体的な実装には .NET の正規表現機能を使用する。

不正な正規表現が入力された場合の UI 上のエラー表示については、SearchService が UI を直接操作せず、エラー情報を上位層へ返せる構造とする。

---

## 5. 検索結果の表示

検索ロジックと検索結果の描画を分離する。

```text
SearchService
    ↓
SearchResult[]
    ↓
DocumentViewModel / Presentation
    ↓
AvalonEdit / Behavior
```

SearchService は「どこが一致したか」を計算するだけとし、検索結果をどのように色付け・強調表示するかは Presentation 層で担当する。

### 5.1 全件ハイライト

検索結果は原則として全件をハイライトする。

現在選択中の検索結果は、その他の検索結果と区別できる強調表示とする。

これにより、検索文字列を入力した時点で文書内の一致箇所を把握できる操作感を実現する。

### 5.2 CurrentContent 変更時

Document.CurrentContent が変更された場合、以前に計算した SearchResult は位置情報が古くなる可能性がある。

そのため、検索対象 Document の内容が変更された場合は、既存の検索結果を無効化し、必要に応じて検索を再実行してハイライトを更新する。

検索 UI を閉じた場合は検索結果のハイライトを解除する。

---

## 6. 置換設計

置換は検索処理を基盤として実装する。

MVP では以下を提供する。

* 1件置換
* すべて置換
* 正規表現による置換
* 置換件数の表示

### 6.1 1件置換

現在の検索結果を対象として1件を置換する。

置換によって Document.CurrentContent が変更された場合、Document.IsModified を true にする。

置換後は検索結果の位置が変化する可能性があるため、検索結果を更新する。

### 6.2 すべて置換

検索条件に一致する箇所を一括置換する。

置換した件数を UI に表示できるようにする。

置換対象が存在しない場合は、変更を発生させず、置換件数 0 とする。

### 6.3 正規表現による置換

正規表現を使用する場合は正規表現による検索・置換を行う。

検索と置換で使用する条件は同一の検索状態を基礎とし、検索処理と置換処理で条件解釈が食い違わないようにする。

---

## 7. Diff 機能

Diff は2つのテキストを比較するための機能とする。

MVP では主として以下の比較を対象とする。

```text
ファイルA
  vs
ファイルB
```

比較結果は Document とは別の DiffTab として Workspace に追加する。

DiffTab は読み取り専用とし、比較結果を編集することはできない。

### 7.1 DiffTab

DiffTab は少なくとも以下の情報を保持する。

* 比較元 A
* 比較元 B
* 比較結果
* 現在の差分位置

Diff の具体的な内部表現は DiffPlex の API に直接依存させず、Application / Domain 側で扱うためのモデルを用意する。

Presentation 層では、そのモデルを AvalonEdit 等の表示機構へ変換する。

### 7.2 Diff ライブラリ

Diff 処理には DiffPlex を使用する。

WPF 上での Diff 表示には DiffPlex.Wpf の利用を想定する。

ただし、DiffPlex の API 呼び出しを ViewModel や View に直接記述せず、Infrastructure 層に閉じ込める。

基本的な依存方向は以下とする。

```text
DiffView
    ↓
DiffViewModel
    ↓
Application
    ↓
Diff Interface
    ↓
Infrastructure / DiffPlex
```

### 7.3 Diff の読み取り専用性

DiffTab は比較結果を表示するためのタブであり、通常の Document の編集状態を持たない。

そのため、Document の IsReadOnly とは別に「DiffView 自体が編集不可」という UI 上の性質として扱う。

---

## 8. Diff の移動操作

Diff の差分位置を順番に移動できるようにする。

Command として以下を提供する。

* 次の差分
* 前の差分

Diff の現在位置を Workspace / DiffTab の状態として管理し、現在の差分位置を View に反映する。

差分が存在しない場合、次の差分 / 前の差分は実質的に何も行わない。

---

## 9. Command 設計

ユーザー操作は可能な限り Command として統一する。

MVP の主要 Command は以下とする。

```text
NewCommand
OpenCommand
SaveCommand
SaveAsCommand
CloseCommand
ExitCommand

FindCommand
ReplaceCommand
NextSearchCommand
PreviousSearchCommand

CompareCommand
NextDiffCommand
PreviousDiffCommand
```

Menu、クイック操作、キーボードショートカット等から同じ Command を呼び出す構成とする。

### 9.1 Command と状態

Command は現在の Workspace / ActiveTab の状態を考慮して CanExecute を判定する。

例：

```text
Save
 └─ ActiveTab が保存可能な Document か

SaveAs
 └─ ActiveTab が Document か

NextSearch
 └─ 検索状態が有効か

NextDiff
 └─ ActiveTab が DiffTab かつ差分が存在するか
```

Command 内に UI 操作を直接記述せず、必要な処理は Application Service / UseCase に委譲する。

---

## 10. 画面層と処理層の責務

画面処理とアプリケーション処理を明確に分離する。

### Presentation

担当するもの：

* WPF View
* ViewModel
* Command
* AvalonEdit
* 検索結果のハイライト表示
* 現在検索結果の強調表示
* Diff の視覚的表示
* UI の表示・非表示
* ユーザー入力の受け取り

### Application

担当するもの：

* 開く
* 保存
* 名前を付けて保存
* 検索
* 置換
* 比較
* Workspace 操作に伴うユースケース

### Domain

担当するもの：

* Document
* Tab に関係するドメインモデル
* SearchResult
* Diff に関係するモデル
* 各種状態・契約

### Infrastructure

担当するもの：

* ファイル I/O
* 文字コード処理
* DiffPlex を使用した比較処理

この分離により、SearchService や Diff 処理を UI 技術から独立させる。

---

## 11. MVP での画面・検索・Diff 方針

MVP では機能を必要最小限に留める。

### 実装対象

* MainWindow
* メニュー
* クイック操作
* Document / Diff のタブ表示
* Document View
* Diff View
* ステータスバー
* 検索 / 置換 UI
* 通常検索
* 正規表現検索
* 検索結果全件ハイライト
* 次 / 前の検索結果への移動
* 1件置換
* すべて置換
* 正規表現による置換
* Diff
* 次の差分 / 前の差分
* 基本的な Command / CanExecute

### MVP では実装しないもの

* 検索履歴
* 高度な検索オプション
* 高度な Diff 編集
* Diff 結果の編集
* 大規模ファイル向けの特別な最適化
* 非同期処理・キャンセル機構を前提とした複雑な実装

これらは MVP の基本構造を壊さず、将来追加できるように設計する。

---

## 12. 基本設計上の確定方針

第2部では以下を基本方針として確定する。

1. MainWindow をメニュー / クイック操作 / 検索・置換 / タブ / アクティブビュー / ステータスバーで構成する。
2. Document と Diff を同一 Workspace のタブとして扱う。
3. SearchService は WPF / AvalonEdit に依存しない。
4. SearchResult は StartIndex / Length で表現する。
5. 検索結果は全件ハイライトし、現在結果を別途強調する。
6. CurrentContent 変更時は検索結果を無効化・更新する。
7. 1件置換 / すべて置換は検索処理を基盤として実装する。
8. Diff は DiffTab として表示し、編集不可とする。
9. DiffPlex の利用は Infrastructure 層に閉じ込める。
10. メニュー / クイック操作 / ショートカット等は可能な限り共通 Command を利用する。
11. Command の CanExecute は ActiveTab や検索 / Diff の状態を考慮する。
12. UI 固有処理と Application / Domain の処理を分離する。
