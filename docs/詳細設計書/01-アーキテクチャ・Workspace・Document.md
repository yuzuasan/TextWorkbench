# TextWorkbench 詳細設計書 第1部

## アーキテクチャ・Workspace・Document

## 1. 文書概要

### 1.1 目的

本書は、TextWorkbenchの実装に必要な詳細設計を定義する。

本書はAIコーディングツールへの入力情報として利用することを前提とし、各クラス・責務・状態・処理規則を実装可能な粒度で定義する。

### 1.2 対象

本部では以下を定義する。

* アプリケーションの論理構成
* プロジェクト構成
* 依存方向
* Composition Root
* Workspace
* Tab
* Document
* DocumentTabViewModel
* 起動時の初期状態

---

# 2. アーキテクチャ

## 2.1 基本方針

TextWorkbenchはC# / WPF / MVVMで構築する。

MVPでは単一WPFアプリケーションプロジェクトを基本とする。ただし、責務を明確にするため、論理的に以下の4層へ分離する。

```text
Presentation
    ↓
Application
    ↓
Domain
    ↑
Infrastructure
```

基本的な処理経路は以下とする。

```text
View
 ↓
ViewModel
 ↓
Application UseCase / Service
 ↓
Domain / Infrastructure
```

ViewModelから直接ファイルI/OやDiffPlex APIを呼び出してはならない。

WPFおよびAvalonEdit固有の処理はPresentation層に閉じ込める。

DiffPlexのAPI呼び出しはInfrastructure層に閉じ込め、Application / DomainからDiffPlexへ直接依存しない。

## 2.2 プロジェクト構成

MVPでは以下の構成とする。

```text
TextWorkbench.sln
├─ TextWorkbench
│  ├─ Presentation/
│  │  ├─ Views/
│  │  ├─ ViewModels/
│  │  ├─ Behaviors/
│  │  └─ Services/
│  │
│  ├─ Application/
│  │  ├─ Services/
│  │  ├─ Interfaces/
│  │  └─ UseCases/
│  │
│  ├─ Domain/
│  │  ├─ Models/
│  │  └─ Enums/
│  │
│  └─ Infrastructure/
│     ├─ File/
│     ├─ Encoding/
│     ├─ LineEnding/
│     └─ Diff/
│
└─ TextWorkbench.Tests
```

## 2.3 Composition Root

MVPではDIコンテナを使用しない。

`App`をComposition Rootとし、アプリケーション起動時に必要なサービス、UseCase、ViewModelを手動で生成して依存関係を構築する。

概念的には以下の構成とする。

```text
App
 ├─ Infrastructure Service
 │   ├─ FileService
 │   ├─ EncodingService
 │   ├─ LineEndingService
 │   └─ DiffEngineAdapter
 │
 ├─ Application UseCase
 │   ├─ NewDocumentUseCase
 │   ├─ OpenDocumentUseCase
 │   ├─ SaveDocumentUseCase
 │   ├─ SaveDocumentAsUseCase
 │   └─ ...
 │
 └─ MainWindowViewModel
      └─ MainWindow
```

Composition Root以外のクラスで、Infrastructure実装を直接newする構成は避ける。

---

# 3. Workspace

## 3.1 責務

Workspaceはアプリケーション全体の作業領域を表し、開いているTabと現在選択されているTabを管理する。

Workspaceが担当する処理は以下とする。

* Tab一覧の保持
* ActiveTabの保持
* Tabの追加
* Tabの削除
* ActiveTabの変更
* 開いているDocumentの検索
* 同一ファイルの二重オープン防止

Workspace自身はファイルI/Oやテキスト検索などの処理を行わない。

## 3.2 保持する状態

```text
ObservableCollection<TabViewModel> Tabs
TabViewModel? ActiveTab
```

`Tabs`は表示順を保持する。

`ActiveTab`がnullの場合、現在アクティブなTabは存在しない。

## 3.3 Tabの種類

TabはViewModelの基底型として扱い、MVPでは以下の2種類を持つ。

```text
TabViewModel
├─ DocumentTabViewModel
└─ DiffTabViewModel
```

DocumentとDiffを別のタブ管理機構として実装せず、同一Workspace上のTabとして統一する。

## 3.4 TabControlとの連携

MainWindowのTabControlはWorkspaceと以下のBindingを行う。

```text
TabControl.ItemsSource
    ↕
Workspace.Tabs

TabControl.SelectedItem
    ↕
Workspace.ActiveTab
```

`SelectedItem`と`ActiveTab`はTwoWay Bindingとする。

Tabの種類に応じてWPF DataTemplateを使用し、表示Viewを切り替える。

```text
DocumentTabViewModel → DocumentView
DiffTabViewModel     → DiffView
```

---

# 4. Document重複管理

## 4.1 基本ルール

同一ファイルを複数のDocumentとして開かない。

ファイルを開く場合は、Workspace内に同じファイルを表すDocumentが存在するか確認する。

```text
Open
 ↓
パス正規化
 ↓
既存Document検索
 ├─ 存在する → 既存TabをActiveにする
 └─ 存在しない → 新規Documentを作成
```

## 4.2 パス正規化

Document.FilePathには正規化済みの絶対パスを保持する。

比較時にはWindows環境を考慮し、以下を使用する。

```csharp
StringComparison.OrdinalIgnoreCase
```

相対パスと絶対パスをそのまま比較してはならない。

## 4.3 既存Documentの検索

Workspaceは開いているDocumentを走査し、正規化済みFilePathが一致するDocumentを検索する。

既存Documentが見つかった場合、新しいDocumentやTabを作成せず、そのDocumentのTabをActiveTabにする。

---

# 5. TabViewModel

## 5.1 基底クラス

TabViewModelはTabとして共通して必要なPresentation状態を保持する。

最低限、以下を想定する。

```text
TabViewModel
├─ DocumentTabViewModel
└─ DiffTabViewModel
```

Tabの共通処理として、少なくとも表示タイトルと閉じるための状態を扱える構造とする。

Document固有の状態やDiff固有の状態をTabViewModelへ持ち込まない。

---

# 6. Document

## 6.1 責務

Documentは「1つのテキストファイル、または未保存のテキスト文書」を表すDomainモデルである。

DocumentはWPF、AvalonEdit、ViewModelを認識してはならない。

Documentは現在のテキスト内容とファイルに関する状態を保持する。

## 6.2 状態

Documentは以下を保持する。

```text
Document
├─ Id
├─ FilePath
├─ FileName
├─ CurrentContent
├─ Encoding
├─ LineEnding
├─ IsModified
├─ IsReadOnly
├─ IsLoading
└─ IsSaving
```

### Id

Documentを一意に識別するためのID。

### FilePath

正規化済み絶対パス。

新規未保存Documentではnull。

### FileName

FilePathから取得できるファイル名。

未保存DocumentではPresentation側で「無題N」を表示するため、Document自身が表示用タイトルを生成する必要はない。

### CurrentContent

現在編集中のテキスト。

内部では改行コードをLFに統一する。

### Encoding

現在Documentで使用する文字コード。

MVPでは以下を対象とする。

```text
Utf8
Utf8Bom
ShiftJis
```

### LineEnding

保存時に使用する改行形式。

```text
CrLf
Lf
Cr
```

### IsModified

保存後から編集が行われたかを表す。

通常編集が発生した場合trueにする。

MVPではUndoによって保存時の内容と同じ状態に戻った場合でも、自動的にfalseへ戻さない。

Save成功時にfalseへ戻す。

### IsReadOnly

現在のDocumentが編集可能かどうかとは別に、ファイルが読み取り専用として扱われる状態を保持する。

ReadOnly Documentでは通常編集と通常Saveを禁止する。

Save Asは許可する。

### IsLoading

Documentの読み込み処理中であることを表す。

### IsSaving

Documentの保存処理中であることを表す。

## 6.3 SavedContentを保持しない

DocumentはSavedContentを常時保持しない。

保存済み内容との比較が必要になった場合は、必要に応じてディスク上のファイルを読み込む。

保存時ハッシュもMVPでは保持しない。

これによりDocumentの状態管理を単純化する。

---

# 7. Documentの編集可否

Documentの編集可能状態は以下で定義する。

```text
IsEditable =
    !IsReadOnly
    && !IsLoading
    && !IsSaving
```

以下の場合は編集不可とする。

* ReadOnly
* Loading中
* Saving中

ReadOnly DocumentでもSave Asは可能とする。

Save Asが成功した場合、新しい保存先をFilePathへ設定し、IsReadOnly=falseとして編集可能にする。

---

# 8. 新規Document

新規Documentは`NewDocumentUseCase`によって生成する。

初期状態は以下とする。

```text
FilePath = null
CurrentContent = ""
Encoding = UTF-8
LineEnding = CRLF
IsModified = false
IsReadOnly = false
IsLoading = false
IsSaving = false
```

新規Documentはまだファイルへ保存されていない。

---

# 9. DocumentTabViewModel

DocumentTabViewModelはDocumentをPresentation層から扱うためのViewModelである。

責務は以下とする。

* Documentの状態をViewへ公開する
* AvalonEditとの連携を管理する
* Documentの編集状態をUIへ通知する
* Search状態を保持する
* Document関連Commandの実行窓口となる

Documentそのものに`INotifyPropertyChanged`を実装しない。

UIへのPropertyChanged通知はDocumentTabViewModel側で行う。

Documentの状態変更は、Documentの状態を直接書き換えるのではなく、定義した操作を通して行う。

例：

```text
UpdateContent()
MarkAsSaved()
SetFileInfo()
```

---

# 10. 無題Documentの表示

FilePathを持たないDocumentはPresentation側で「無題N」と表示する。

例：

```text
無題1
無題2
無題3
```

Nは単調増加とする。

タブを閉じても番号を再利用しない。

アプリケーションを再起動した場合は1から開始する。

FilePathを持つDocumentではファイル名を表示する。

```text
FilePathあり → FileName
FilePathなし → 無題N
```

IsModifiedをタイトル文字列へ埋め込まない。

未保存状態は別のUI要素で表示する。

---

# 11. 起動時の初期状態

TextWorkbench起動時には`NewDocumentUseCase`を利用して新規Documentを1つ生成する。

初期状態：

```text
Tabs = [無題1]
ActiveTab = 無題1
```

起動直後から編集可能とする。

ファイルを開いていない状態でも、ユーザーはそのままテキスト入力を開始できる。

---

# 12. DocumentとAvalonEditの境界

DocumentはAvalonEditを直接参照しない。

構造は以下とする。

```text
Document
 ↓
DocumentTabViewModel
 ↓
DocumentView
 ↓
AvalonEdit
```

AvalonEdit固有の処理はPresentation層で実装する。

例えば以下はDocumentの責務ではない。

* TextEditorへの参照保持
* カーソル位置
* Selection
* AvalonEditのUndoStack
* SearchHighlight
* Scroll操作

これらはView / ViewModel / Behavior側で扱う。

Undo / RedoについてもDocumentが独自のUndoStackを保持せず、AvalonEditの標準機能を利用する。

---

# 13. 本部の実装上の重要ルール

実装時は以下を遵守する。

1. ViewModelから直接ファイルI/Oを実行しない。
2. DocumentからWPF/AvalonEditへ依存しない。
3. DocumentからInfrastructure実装へ直接依存しない。
4. 同一ファイルを複数Documentとして開かない。
5. Document.FilePathは正規化済み絶対パスとする。
6. Document内部の改行はLFに統一する。
7. SavedContentを常時保持しない。
8. IsModifiedとIsReadOnlyを独立した状態として扱う。
9. ReadOnlyでもSave Asは許可する。
10. Loading中・Saving中は編集不可とする。
11. DiffもDocumentと同じWorkspace上のTabとして扱う。
12. UI固有処理をDomainへ持ち込まない。
13. MVPではDIコンテナを導入しない。
14. Composition Rootで依存関係を構築する。
15. 起動時には「無題1」を1つ生成する。
