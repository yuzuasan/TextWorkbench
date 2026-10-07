# TextWorkbench 基本設計書

## 第1部：全体方針・アーキテクチャ・Document設計

## 1. 文書概要

### 1.1 目的

本書は、TextWorkbenchの実装に必要となる基本設計を定義する。

要件定義書で定めた機能要件を実現するため、アプリケーション全体の構造、責務分担、Document管理、Workspace管理、ファイル入出力に関する基本方針を定める。

本設計では、MVPを過度に複雑化させないことを優先しつつ、Phase 5以降の拡張を妨げない構造とする。

### 1.2 対象範囲

本第1部では、以下を対象とする。

* アプリケーション全体構成
* アーキテクチャ
* プロジェクト構成
* Workspace
* Tab
* Document
* Documentの状態管理
* AvalonEditとの責務分離
* 文字コード・改行コードの基本設計
* 保存処理の基本方針

検索・置換、画面詳細、Diff、Command等については後続の基本設計書で定義する。

---

# 2. 基本設計方針

## 2.1 アプリケーション構成

TextWorkbenchは、Windows上で動作するC# / WPF / MVVMによるテキストユーティリティアプリケーションとする。

対象とする主な操作は以下とする。

* テキストファイルの閲覧
* テキストファイルの編集
* 検索・置換
* テキスト加工
* 2ファイル比較

高機能エディタやIDEを目指すのではなく、「テキストファイルを少し確認・編集・加工・比較する」用途に適したアプリケーションとする。

特にDiff比較を主要機能の一つとして位置付ける。

---

# 3. アーキテクチャ

## 3.1 基本方針

MVPでは**単一WPFプロジェクト**とする。

物理的に複数プロジェクトへ分割するのではなく、フォルダおよびNamespaceによって責務を論理的に分離する。

想定構成は以下とする。

```text
TextWorkbench
├─ Presentation
│  ├─ Views
│  ├─ ViewModels
│  ├─ Commands
│  └─ Behaviors
│
├─ Application
│  ├─ Services
│  └─ UseCases
│
├─ Domain
│  ├─ Models
│  ├─ Enums
│  └─ Interfaces
│
└─ Infrastructure
   ├─ File
   ├─ Encoding
   └─ Diff
```

この構成は、プロジェクト規模に対して過度なレイヤー分割を行わず、責務だけを明確にすることを目的とする。

## 3.2 依存方向

基本的な依存方向は以下とする。

```text
View
 ↓
ViewModel
 ↓
Application Service
 ↓
Domain / Infrastructure
```

ViewModelから直接ファイルI/OやDiffライブラリを呼び出す構造にはしない。

例えば、ViewModelから以下を直接実行しない。

```text
File.ReadAllText
File.WriteAllText
DiffPlex API
```

ファイル操作やDiff処理はService等へ分離する。

## 3.3 WPF依存の分離

DomainおよびApplication層は、可能な限りWPFに依存しない構造とする。

特にAvalonEdit固有の処理やWPF固有の表示処理はPresentation側に閉じ込める。

これにより、Documentや検索ロジックなどのアプリケーション固有処理をUI実装から分離する。

---

# 4. 使用ライブラリ

現時点で以下のライブラリを利用する。

| ライブラリ                 | 用途                             |
| --------------------- | ------------------------------ |
| CommunityToolkit.Mvvm | MVVM、ObservableObject、Command等 |
| AvalonEdit            | テキスト編集領域                       |
| DiffPlex              | Diff計算                         |
| DiffPlex.Wpf          | Diff表示・WPF連携                   |

ライブラリの具体的なAPI利用方法は実装時に確認する。

DiffPlex.Wpfについては、左右ペインのスクロール同期などが容易に実現できる場合、MVPへの採用を検討する。

---

# 5. Workspace設計

## 5.1 基本方針

TextWorkbenchでは、複数のDocumentおよびDiffを一つの作業空間で管理する。

この作業空間を**Workspace**とする。

概念構造は以下とする。

```text
MainWindowViewModel
        │
        ▼
    Workspace
        │
        ├─ DocumentTab
        │    ├─ Document
        │    ├─ Document
        │    └─ ...
        │
        ├─ DiffTab
        │    ├─ DiffSession
        │    └─ ...
        │
        └─ ActiveTab
```

DocumentとDiffは、画面遷移によって切り替えるのではなく、同じWorkspace上の「タブ」として扱う。

## 5.2 Workspaceの責務

Workspaceは主に以下を担当する。

* タブ一覧の保持
* ActiveTabの管理
* DocumentTabの追加
* DiffTabの追加
* タブの削除
* Documentの検索
* 既に開いているDocumentのActivate
* アクティブタブの変更

Workspace自身がファイルI/OやDiff計算を直接担当することは避ける。

## 5.3 同一ファイルの二重オープン

同じファイルを複数のDocumentとして開かないことを基本とする。

ファイルを開く際には、パスを正規化して既存Documentを検索する。

```text
Open
 ↓
Path Normalize
 ↓
既存Documentあり？
 ├─ Yes → 既存タブをActivate
 └─ No  → 新しいDocumentを作成
```

既に開かれている場合は、既存Documentを再読み込みするのではなく、原則として既存タブをアクティブ化する。

---

# 6. Tab設計

Workspace上のタブには、少なくとも以下の2種類を設ける。

```text
DocumentTab
DiffTab
```

## 6.1 DocumentTab

通常のテキストファイルを表示・編集するタブ。

```text
DocumentTab
 └─ DocumentViewModel
      └─ DocumentView
           └─ AvalonEdit
```

## 6.2 DiffTab

2つのテキストを比較するタブ。

```text
DiffTab
 └─ DiffViewModel
      └─ DiffView
           ├─ Left
           └─ Right
```

DiffTabは通常のDocumentTabとは異なり、原則として読み取り専用とする。

---

# 7. Document設計

## 7.1 基本概念

TextWorkbenchでは、**1ファイルを1つのDocumentとして扱う**。

Documentはファイルそのものではなく、TextWorkbench上で編集対象となっている論理的なテキスト単位として扱う。

基本モデルは以下とする。

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

## 7.2 各プロパティ

| 項目             | 内容                        |
| -------------- | ------------------------- |
| Id             | Documentを一意に識別するID        |
| FilePath       | 対象ファイルのパス。新規Documentでは未設定 |
| FileName       | 表示用ファイル名                  |
| CurrentContent | 現在編集中のテキスト                |
| Encoding       | 現在の文字コード                  |
| LineEnding     | 保存時に使用する改行コード             |
| IsModified     | 保存後から変更されているか             |
| IsReadOnly     | 編集・通常保存ができない状態か           |
| IsLoading      | 読み込み中か                    |
| IsSaving       | 保存中か                      |

---

# 8. Document状態管理

## 8.1 IsModified

`IsModified`は、最後に保存された状態からDocumentが変更されているかを表す。

編集によって内容が変更された場合は`true`とする。

保存が成功した場合は`false`とする。

保存に失敗した場合は`true`を維持する。

## 8.2 IsReadOnly

`IsReadOnly`は、Documentを通常の編集・保存対象として扱えるかを表す。

`IsModified`とは独立した状態として管理する。

したがって、以下の状態も成立する。

```text
IsReadOnly = true
IsModified = true
```

読み取り専用であることと、変更されていることは別概念として扱う。

## 8.3 Save AsとReadOnly

読み取り専用Documentであっても、「名前を付けて保存」は可能とする。

Save Asが成功した場合は、新しい保存先をDocumentのFilePathとして扱う。

保存後は基本的に以下の状態とする。

```text
IsReadOnly = false
IsModified = false
```

---

# 9. 保存済み内容の管理

Documentには、常時`SavedContent`を保持しない。

現在の編集内容は`CurrentContent`で管理する。

保存済み内容との比較が必要になった場合は、必要時にファイルから読み込む。

例えば将来、以下のDiffを実装する場合、

```text
保存済みファイル
      vs
現在編集中のDocument
```

必要なタイミングでディスク上のファイルを読み込み、`CurrentContent`と比較する。

```text
Disk File
   ↓
読み込み
   ↓
CurrentContentと比較
```

これにより、Document内部に保存済みスナップショットを常時保持する必要をなくす。

---

# 10. DocumentとAvalonEdit

DocumentはWPFおよびAvalonEditを直接参照しない。

基本構造は以下とする。

```text
Document
   ↓
DocumentViewModel
   ↓
DocumentView
   ↓
AvalonEdit
```

AvalonEdit固有の処理はViewまたはBehavior等で処理する。

例えば以下のような処理はPresentation側で扱う。

* カーソル操作
* Selection
* 編集イベント
* 検索結果のハイライト
* AvalonEdit固有APIによる表示制御

Domain/Application側にWPF依存やAvalonEdit依存を持ち込まない。

---

# 11. 新規Document

新規Document作成時は、以下を初期状態とする。

```text
FilePath       = null
CurrentContent = ""
Encoding       = UTF-8
LineEnding     = CRLF
IsModified     = false
IsReadOnly     = false
```

新規Documentはまだファイルとして保存されていない。

タブタイトル等によって、ユーザーが未保存状態を認識できるようにする。

---

# 12. 内部改行コード

Document内部のテキスト表現では、**LFを基本とする**。

```text
CurrentContent
      ↓
     LF
```

ファイル保存時にDocumentの`LineEnding`へ変換する。

```text
CurrentContent (LF)
        ↓
LineEndingへ変換
        ↓
   Encoding
        ↓
     File
```

内部表現をLFへ統一することで、以下の処理を統一しやすくする。

* 検索
* 置換
* Diff
* 行単位処理

---

# 13. 文字コード設計

## 13.1 MVP対応

MVPでは以下の文字コードを対象とする。

### 読み込み

* UTF-8
* UTF-8 BOM
* Shift-JIS

### 保存

* UTF-8を基本とする
* UTF-8 BOM / Shift-JISの保存選択は、工数に余裕がある場合に対応する

## 13.2 EncodingService

文字コード判定は`EncodingService`へ分離する。

基本的な判定方針は以下とする。

```text
BOMあり
 ↓
UTF-8 BOM

BOMなし + UTF-8として妥当
 ↓
UTF-8

それ以外
 ↓
Shift-JIS
```

ただし、文字コード判定には曖昧性があるため、完全な自動判定を保証するものではない。

EncodingServiceは将来的にUTF-16等の文字コードを追加できる構造とする。

---

# 14. 保存処理

## 14.1 基本方針

MVPでは、保存処理中の編集を一時的に禁止する。

これにより、保存開始後にDocument内容が変更されることを防ぐ。

基本フローは以下とする。

```text
SaveCommand
 ↓
保存可能か確認
 ↓
IsSaving = true
 ↓
編集禁止
 ↓
CurrentContent取得
 ↓
改行コード変換
 ↓
Encoding
 ↓
File書き込み
 ↓
成功
 ↓
IsModified = false
 ↓
IsSaving = false
```

## 14.2 保存失敗

保存に失敗した場合は、変更状態を維持する。

```text
IsModified = true
```

保存失敗によって、ユーザーが行った編集内容を未保存状態のまま維持できるようにする。

---

# 15. 非同期処理方針

MVPでは巨大ファイルを主要対象としないため、Open / Save / Diffを必ず非同期化することは要求しない。

まずは以下を優先する。

* 正しいファイルI/O
* 正しいDocument状態管理
* 正しいDiff処理
* シンプルで理解しやすい実装

したがって、MVPでは同期処理を許容する。

一方で、将来的な非同期化を妨げないよう、ファイル操作やDiff処理をViewModelから分離する。

以下はPhase 6以降の検討対象とする。

* 非同期Open
* 非同期Save
* 非同期Diff
* 大容量ファイル対応
* CancellationToken
* UI応答性改善

---

# 16. 基本設計上の確定方針

本第1部では、以下を基本設計方針として採用する。

1. MVPは単一WPFプロジェクトとする。
2. フォルダ・Namespaceによって責務を論理分離する。
3. MVVMを基本アーキテクチャとする。
4. ViewModelから直接ファイルI/OやDiffライブラリを呼び出さない。
5. Workspaceをアプリケーションの作業単位として扱う。
6. DocumentとDiffをWorkspace上のタブとして統一的に管理する。
7. 同一ファイルの二重オープンを基本的に禁止する。
8. 1ファイルを1つのDocumentとして扱う。
9. `IsModified`と`IsReadOnly`を独立した状態として管理する。
10. `SavedContent`をDocumentへ常時保持しない。
11. DocumentはAvalonEditおよびWPFへ依存しない。
12. Document内部の改行コードはLFを基本とする。
13. ファイル保存時に改行コードおよび文字コードへ変換する。
14. MVPではUTF-8 / UTF-8 BOM / Shift-JISの読み込みを対象とする。
15. MVPの保存はUTF-8を基本とする。
16. MVPでは同期処理を許容する。
17. 将来の非同期化を考慮し、I/O処理を適切に分離する。

以上をTextWorkbenchの基本設計における全体構造およびDocument設計の基礎とする。
