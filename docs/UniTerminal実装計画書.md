UniTerminal実装計画書
---

# Unity 文字列CLI（Linux風）実装計画書

## 0. 前提環境

* Unity: **Unity 6.3 LTS**
* 配布形態: **UPM package**
* 対象: **文字列ベースのCLI実行基盤**
* 本パッケージは **描画/UI/入力デバイス管理を担当しない**

  * 入力: string
  * 出力: string（stdout / stderr）
* 非同期対応必須

  * 標準: `Task`
  * UniTask 利用可能な環境では **UniTask アダプタを提供**
* Linux CLI の挙動を参考にするが、**対応範囲は明示的に限定する**
* フォルダ構成
  * Packages/jp.xeon.uni-terminal/
    * package.json
    * Runtime
      * jp.xeon.uni-terminal.runtime.asmdef
    * Editor
      * jp.xeon.uni-terminal.editor.asmdef
    * Tests/
      * Editor/
        * jp.xeon.uni-terminal.tests.editor.asmdef
    * Samples/
* namespace は`Xeon.UniTerminal`を基本にする
---

## 1. 入力・トークナイズ仕様

### 1.1 空白・区切り

* 引数区切りは **半角スペースのみ**
* タブ文字は **入力として扱わず、補完処理に使用**
* タブはトークナイズ対象外

### 1.2 クォート

* **シングルクォート `'` / ダブルクォート `"` の両方をサポート**
* クォート内の文字列は1引数として扱う
* 空文字引数をサポート

  * `""` / `''` → 空文字 `""`

### 1.3 エスケープ

* エスケープ文字は `\`
* クォート外:

  * `\` により次の1文字をリテラル化
* シングルクォート内:

  * **エスケープ不可**
* ダブルクォート内:

  * `\"`, `\\` を許可
* `\n`, `\t` 等は **文字変換しない**
* Unicode はそのまま扱う
* コメント構文は **非対応**

### 1.4 改行コード変換

* `\n` → `<br/>` への変換は **出力レイヤのみで行う**
* 実改行(LF)のみ`<br/>`へ変換
* パース・実行・内部データでは変換しない

---

## 2. コマンド・オプション仕様

### 2.1 基本構造

* コマンドは以下を持つ

  * コマンド名
  * 位置引数
  * オプション
  * リダイレクト
  * パイプライン

### 2.2 オプション形式

* 短縮オプション: `-x`
* 非短縮オプション: `--option`
* オプション終了記号 `--` をサポート

  * 以降はすべて位置引数として扱う

### 2.3 短縮オプション束ね

* **bool型オプションのみ束ね可能**

  * `-abc` → `-a -b -c`
* 値が必要な短縮オプションは必ず

  * スペース区切り または `=` を使用
  * `-n10` は **非対応**

### 2.4 オプション値指定

* 値指定は以下をサポート

  * `--name value`
  * `--name=value`
* クォート付き値を許可

  * `--name="a b"`

### 2.5 対応型

* `bool`
* `int`
* `float / double`
* `string`
* `enum`（大文字小文字を区別しない）
* `List<T>`

### 2.6 bool型

* 値指定不可
* 存在すれば `true`
* 存在しなければ `false`

### 2.7 List型

* **繰り返し指定は不可**
* 値は `,` 区切り

  * `--targets=a,b,c`
* クォート保護あり

  * `"a,b"` は1要素
* `--opt=` は空要素（`[""]`）
* 要素型は `string` 以外も許可

### 2.8 多重指定

* scalar型: **最後優先**
* bool型: 指定されていれば true
* List型: 仕様上多重指定不可

### 2.9 unknown option

* **エラー扱い**
* 対象コマンドのヘルプを **stderr** に表示
* パイプライン全体を停止
* exitCode = **2**

---

## 3. パイプ・リダイレクト仕様

### 3.1 パイプ

* 演算子: `|`
* **行単位で接続**
* stdout → 次コマンドの stdin
* stderr はパイプしない

### 3.2 リダイレクト

* `< file` : stdin 差し替え
* `> file` : stdout 上書き
* `>> file`: stdout 追記
* `>` の直後に `|` が来る構文は **エラー**
* `<` と `|` の併用は許可
* 文字コード: UTF-8

---

## 4. パス・ファイル仕様

* HomeDirectory:

  * `Application.persistentDataPath`
* WorkingDirectory:

  * グローバル
  * 実行中に変更可能
* 相対パス:

  * WorkingDirectory 基準
* 絶対パス: 許可
* `..` を含むパス: 許可

---

## 5. Linux互換範囲

* glob (`*`, `?`) 展開: **非対応**
* 環境変数展開 (`$VAR`): 将来拡張
* `~` 展開: 対応
* 制御演算子 (`;`, `&&`, `||`): 非対応
* ヒアドキュメント (`<<`): 非対応
* 2系 (`2>`, `2>&1`): 後回し

---

## 6. 非同期実行モデル

* 標準I/F: `Task`
* UniTask アダプタを提供
* 全コマンドに `CancellationToken` を渡す
* stdin / stdout / stderr は **行単位**

---

## 7. コマンド定義方式

### 7.1 Attribute

#### コマンド

```csharp
[Command(string commandName, string description)]
```

* `ICommand` を実装したクラスにのみ指定可能

#### オプション

```csharp
[Option(string longName, string shortName = "", bool isRequire = false)]
```

* プロパティまたはフィールドに指定
* `longName` は `--option` 形式
* `shortName` は省略可
* デフォルト値は定義時に指定
* `isRequire=true` で未指定時はエラー

### 7.2 コマンド探索

* 利用側が探索対象 Assembly を指定
* 同名コマンドは **最後優先**
* ただしエラーログを出力

---

## 8. ICommand インターフェイス（最小で強い契約）

```csharp
public interface ICommand
{
    string CommandName { get; }
    string Description { get; }

    Task<int> ExecuteAsync(CommandContext context, CancellationToken ct);

    IEnumerable<string> GetCompletions(CompletionContext context);
}
```

* `GetCompletions` は未実装でも可（空列挙可）

---

## 9. Parsed構造（AST / 解析結果）

### 9.1 Parse層（文字列・構文のみ）

* Parse層は **型変換を行わない**
* 構文・トークン・構造のみを扱う

（※ ParsedInput / ParsedPipeline / ParsedCommand / ParsedOptions / ParsedRedirections 等は別紙定義）

---

## 10. Binding層（型変換・検証）

* Option 属性を元に型変換を行う
* 以下をここで検出する

  * unknown option
  * 必須オプション未指定
  * 型変換失敗
* エラー時:

  * exitCode = 2
  * ヘルプを stderr に出力
  * パイプライン全体を停止

---

## 11. エラー分類（計画書追記事項）

**Parse層・Bind層・Runtime層を明確に分離する**

* ParseError

  * 構文エラー（クォート未閉じ、`>`直後の`|`など）
  * タブ文字を検知した場合は exitCode = 2 でエラーとして扱う
* BindError

  * unknown option
  * required option 未指定
  * 型変換失敗
* RuntimeError

  * 実行時例外、I/O失敗など

BindError は常に

* exitCode = 2
* ヘルプを stderr に出力

### コマンド未発見時の扱い

- 入力されたコマンド名が登録済みコマンド一覧に存在しない場合は BindError として扱う
- stderr に以下の情報を出力する
  - `command not found: <commandName>`
  - グローバルヘルプ（登録済みコマンド一覧）
- exitCode は 2 とする
- パイプライン全体を停止する

### exitCode 規約

- 正常終了：exitCode = 0
- ParseError：exitCode = 2（使用法エラー）
- BindError：exitCode = 2（使用法エラー）
- RuntimeError：exitCode = 1

---

## 12. Tab補完仕様

* 対象:

  * コマンド名
  * オプション名
  * パス
* カーソル位置: 末尾固定
* 置換範囲: なし（後方追記のみ）
* 候補一覧を返す
* 連続Tabはサポートせず、1回目の候補表示まで

### 補完候補の順序

- 補完候補の表示順は以下とする
  - コマンド名補完：辞書順（昇順）
  - オプション名補完：辞書順（昇順）
  - パス補完：ファイルシステムから列挙された順序を維持する

---

## 13. 受け入れ基準（AC）

ACは別ファイルに記載するが、最低限以下を含む。

* オプション（bool / scalar / list）の正確な解釈
* unknown option → stderr ヘルプ表示＋exitCode=2
* 行単位パイプ動作
* `<`, `>`, `>>` の正常系
* `>` の後に `|` が来た場合の構文エラー

---

## 14. 実装計画書に追記する重要文（必須）

> Parse層は型変換を行わず、文字列と構文のみを扱う。
> 型変換、必須オプション検証、unknown option 判定はすべて Binding 層で行う。
> unknown option および必須オプション欠落は BindError として扱い、
> exitCode=2 でコマンドヘルプを stderr に出力し、パイプライン全体を停止する。

---


---

# 実装手順

## 0. パッケージ骨格（最初に固定）

1. UPM package の雛形作成

   * `package.json`
   * `Runtime/`（asmdef 作成、Editor依存なし）
   * `Tests/`（EditMode/PlayMode どちらでも可。最低限 EditMode）
2. 名前空間とアセンブリ境界を固定

   * `Company.Terminal.Parsing`
   * `Company.Terminal.Binding`
   * `Company.Terminal.Runtime`

---

## 1. 基盤型（インターフェイス）を先に作る

3. 出力レイヤ（改行変換をここに集約）

   * `IAsyncTextWriter`（`WriteLineAsync(string)` 等）
   * 実装例：`StringBuilderTextWriter`, `FileTextWriter`
   * **`\n` → `<br/>` 変換は writer の内部でのみ実施**
4. 入力レイヤ（行単位）

   * `IAsyncTextReader` もしくは `IAsyncEnumerable<string> StdinLines` を扱うラッパ
5. `CommandContext` と `CompletionContext` の型定義

   * `stdin/stdout/stderr`
   * `WorkingDirectory`, `HomeDirectory`
   * `CancellationToken` を受け渡しできること

---

## 2. Tokenizer（最初にユニットテストを付ける）

6. Token 型（`TokenKind`, `SourceSpan`）を定義
7. Tokenizer 実装

   * スペースのみ区切り
   * `'` / `"` クォート
   * `\` エスケープ（外／ダブル内のみ、シングル内不可）
   * 空文字引数
   * 演算子トークン化：`|`, `<`, `>`, `>>`, `--`
   * Unicodeそのまま
8. Tokenizer のユニットテスト

   * クォート、エスケープ、空文字、`--`、`>>`、演算子分離、Span

---

## 3. Parser（AST生成：Parsed構造を完成させる）

9. Parsed 構造（AST）を確定実装

   * `ParsedInput`, `ParsedPipeline`, `ParsedCommand`
   * `ParsedOptions` / `ParsedOptionOccurrence` / `OptionValue`
   * `ParsedRedirections`
10. Parser 実装（Tokenizer出力 → ParsedAST）

* 優先順位：トークナイズ → リダイレクト結合 → パイプ結合
* `>` 直後に `|` が来る場合は **ParseError**
* `<` と `|` 併用は許可

11. Parser のユニットテスト

* パイプ段数の分割
* リダイレクト付与
* `cmd > out | next` がエラーになること

---

## 4. Attribute定義と Reflection ローダ

12. Attribute 定義

* `CommandAttribute(commandName, description)`
* `OptionAttribute(longName, shortName="", isRequire=false)`

13. `ICommand` インターフェイス定義（ExecuteAsync + GetCompletions）
14. コマンド探索（Assembly指定）

* 対象Assemblyから `[Command]` 付きで `ICommand` 実装クラスを抽出
* 同名コマンドは **最後優先**＋エラーログ

15. Option メタデータ抽出

* `[Option]` 付き field/property の一覧化
* longName（`--xxx`）／shortName（`x`）／required／型情報／デフォルト値

---

## 5. Binder（型変換・検証・ヘルプ自動生成）

16. Binder 入口

* `ParsedCommand` + `CommandRegistry`（探索済みメタ）→ `BoundCommand`

17. unknown option 判定（BindError）

* 対象コマンドの Optionメタと照合
* unknown を検出したら **ヘルプを stderr** に出して停止（exitCode=2）

18. required option 検証（BindError）
19. 型変換

* bool：値指定があれば BindError（exitCode=2）
* enum：大文字小文字無視
* List：`,` split（クォート保護済みトークン前提）、`--opt=` は `[""]`
* 数値：InvariantCulture

20. ヘルプ自動生成（コマンド単位）

* `commandName`, `description`
* オプション一覧（long/short, required, 型, デフォルト）
* unknown/required/変換失敗時に stderr に出せる API を用意

21. Binder のユニットテスト

* unknown option → help + exitCode=2
* required欠落 → help + exitCode=2
* bool値指定禁止
* list split と `--opt=`

---

## 6. Executor（パイプ実行：行単位接続）

22. 実行エンジン（PipelineExecutor）

* `ParsedInput` → Parse → Bind → Execute の流れを統合
* **行単位で stdout→stdin を接続**
* stderr は各コマンドの stderr に出し、パイプしない

23. リダイレクト実装

* `<`：ファイル → stdinLines
* `>`/`>>`：stdout writer を FileWriter に差し替え
* WorkingDirectory 基準、`~` 展開対応

24. Cancellation 対応

* 全コマンド ExecuteAsync に ct を渡す
* パイプライン停止時は以降を実行しない

---

## 7. 補完（Tab）実装

25. CompletionEngine 実装

* 入力末尾から CurrentToken を抽出（スペースのみ区切り）
* Target判定（CommandName / OptionName / Path）
* 候補一覧を返す（置換は「現在補完中トークンを全置換」）
* 連続Tabは非対応（候補表示のみ）

26. 補完のテスト

* コマンド名補完
* `-`/`--` 後のオプション候補
* パス補完（WorkingDirectory基準、`~` 展開）

---

## 8. UniTask アダプタ

27. `Task` ベース実装をラップする UniTask アダプタを提供

* `#if UNITASK` 等でコンパイル条件を分ける
* もしくは別 asmdef に分離（推奨）

---

## 9. 統合テスト／サンプル（任意だが推奨）

28. 最小サンプルコマンドを2〜3個用意（テスト用）

* `echo`
* `grep` 相当（patternでフィルタ）
* `help`（自動ヘルプ呼び出しの導線）

29. 統合テスト

* `cat < in.txt | grep --pattern="foo" > out.txt`
* unknown option で pipeline が止まる

---

## 10. 仕上げ（品質）

30. 例外の取り扱いを統一

* RuntimeError は stderr に出して exitCode=1（など）

31. README（導入・登録・実行・補完I/F）
32. APIを最小化（公開範囲を asmdef / namespace で整理）

---


---

# Acceptance Criteria / Test Cases（Given / When / Then）

## 0. 共通前提

* Given: 入力は文字列で与えられる
* Given: 引数区切りは半角スペースのみ
* Given: タブは補完用途であり、通常入力に含まれない（防御実装の有無は別途ACで定義）
* Given: stdin/stdout/stderr は行単位
* Given: `\n` → `<br/>` 変換は **出力レイヤのみ**
* Given: unknown option は「コマンドヘルプを stderr に出力」「exitCode=2」「パイプライン停止」

---

## 1. Tokenizer AC

### TKN-001 単一スペース区切り

* Given: 入力 `echo a b`
* When: Tokenize する
* Then: トークン列は `["echo","a","b"]` になる

### TKN-002 連続スペース

* Given: 入力 `echo  a   b`
* When: Tokenize する
* Then: トークン列は `["echo","a","b"]` になる

### TKN-003 前後スペース

* Given: 入力 `  echo a  `
* When: Tokenize する
* Then: トークン列は `["echo","a"]` になる

### TKN-010 タブ混入（防御実装を入れる場合）

* Given: 入力 `echo<TAB>a`（`<TAB>`は実タブ文字）
* When: Tokenize/Parse する
* Then: ParseError（使用法エラー）になる（exitCode=2相当）

### TKN-020 ダブルクォート

* Given: 入力 `echo "a b"`
* When: Tokenize する
* Then: トークン列は `["echo","a b"]` になる

### TKN-021 シングルクォート

* Given: 入力 `echo 'a b'`
* When: Tokenize する
* Then: トークン列は `["echo","a b"]` になる

### TKN-022 空文字（ダブル）

* Given: 入力 `echo ""`
* When: Tokenize する
* Then: トークン列は `["echo",""]` になる

### TKN-023 空文字（シングル）

* Given: 入力 `echo ''`
* When: Tokenize する
* Then: トークン列は `["echo",""]` になる

### TKN-030 クォート外エスケープ（スペース）

* Given: 入力 `echo a\ b`
* When: Tokenize する
* Then: トークン列は `["echo","a b"]` になる

### TKN-031 クォート外エスケープ（バックスラッシュ）

* Given: 入力 `echo a\\b`
* When: Tokenize する
* Then: トークン列は `["echo","a\\b"]` になる

### TKN-032 ダブルクォート内 `\"`

* Given: 入力 `echo "a\"b"`
* When: Tokenize する
* Then: トークン列は `["echo","a\"b"]` になる

### TKN-033 ダブルクォート内 `\\`

* Given: 入力 `echo "a\\b"`
* When: Tokenize する
* Then: トークン列は `["echo","a\\b"]` になる

### TKN-034 シングルクォート内エスケープ不可

* Given: 入力 `echo 'a\'b'`
* When: Tokenize/Parse する
* Then: ParseError（使用法エラー）になる（exitCode=2相当）

### TKN-040 `\n` は変換しない（文字として保持）

* Given: 入力 `echo \n`
* When: Tokenize する
* Then: 第2トークンは `"\\n"`（改行に変換されない）

### TKN-050 クォート未閉じ（ダブル）

* Given: 入力 `echo "a`
* When: Tokenize/Parse する
* Then: ParseError（使用法エラー）になる（exitCode=2相当）

### TKN-060 演算子 `|`

* Given: 入力 `a|b`
* When: Tokenize する
* Then: トークン列は `["a","|","b"]` になる

### TKN-062 演算子 `>`

* Given: 入力 `echo a>out.txt`
* When: Tokenize する
* Then: トークン列は `["echo","a",">","out.txt"]` になる

### TKN-063 演算子 `>>`

* Given: 入力 `echo a>>out.txt`
* When: Tokenize する
* Then: トークン列は `["echo","a",">>","out.txt"]` になる

### TKN-064 オプション終了 `--`

* Given: 入力 `cmd -- --notOption`
* When: Tokenize する
* Then: トークン列は `["cmd","--","--notOption"]` になる

---

## 2. Parser AC（AST生成）

### PRS-001 単一コマンド

* Given: 入力 `echo a b`
* When: Parse して AST を生成する
* Then: Commands数は1
* And: CommandName は `echo`
* And: Positionals は `["a","b"]`

### PRS-010 `--` 以降は位置引数

* Given: 入力 `cmd -- --x -y`
* When: Parse する
* Then: `--x` と `-y` は Options ではなく Positionals に入る

### PRS-020 パイプ2段

* Given: 入力 `a | b`
* When: Parse する
* Then: Commands数は2（`a`,`b`）

### PRS-030 stdinリダイレクト

* Given: 入力 `cat < in.txt`
* When: Parse する
* Then: CommandのStdinリダイレクトPathは `in.txt`

### PRS-031 stdout上書き

* Given: 入力 `echo a > out.txt`
* When: Parse する
* Then: StdoutリダイレクトModeは Overwrite、Pathは `out.txt`

### PRS-032 stdout追記

* Given: 入力 `echo a >> out.txt`
* When: Parse する
* Then: StdoutリダイレクトModeは Append、Pathは `out.txt`

### PRS-040 `>`直後の`|`は禁止

* Given: 入力 `echo a > out.txt | next`
* When: Parse する
* Then: ParseError（使用法エラー）になる（exitCode=2相当）

### PRS-041 `>` の後にファイル名がない

* Given: 入力 `echo a >`
* When: Parse する
* Then: ParseError（使用法エラー）になる（exitCode=2相当）

### PRS-043 末尾が`|`

* Given: 入力 `a |`
* When: Parse する
* Then: ParseError（使用法エラー）になる（exitCode=2相当）

---

## 3. Binder AC（属性照合・型変換・必須・ヘルプ）

### BND-010 unknown option（long）

* Given: `select` コマンドが登録され、既知オプションに `--unknown` は存在しない
* And: 入力 `select --unknown=1`
* When: Bind する
* Then: BindError になる
* And: `select` のヘルプが stderr に出力される
* And: exitCode は 2 になる

### BND-020 required option 未指定

* Given: `grep` コマンドの `--pattern` は required
* And: 入力 `grep`
* When: Bind する
* Then: BindError になる
* And: `grep` のヘルプが stderr に出力される
* And: exitCode は 2 になる

### BND-030 bool 値指定禁止（long）

* Given: `--verbose` は bool
* And: 入力 `select --verbose=true`
* When: Bind する
* Then: BindError（使用法エラー）になる（exitCode=2）

### BND-040 短縮束ね（boolのみ）

* Given: `-a -b -c` はすべて bool
* And: 入力 `cmd -abc`
* When: Bind する
* Then: a,b,c は true になる

### BND-050 `-n10` 非対応

* Given: `-n` は値を取るオプション
* And: 入力 `cmd -n10`
* When: Bind する
* Then: 使用法エラー（BindError or ParseError）になる（exitCode=2相当）

### BND-051 `-n 10` は許可

* Given: `-n` は int
* And: 入力 `cmd -n 10`
* When: Bind する
* Then: n は 10 になる

### BND-060 enum 大小文字無視

* Given: `--mode` は enum
* And: 入力 `cmd --mode=FAST`
* When: Bind する
* Then: `fast` と同一の enum 値に変換される

### BND-070 数値変換失敗

* Given: `--count` は int
* And: 入力 `cmd --count=abc`
* When: Bind する
* Then: BindError（使用法エラー）になる（exitCode=2）

### BND-080 List 分割（`,`）

* Given: `--targets` は List
* And: 入力 `select --targets=a,b,c`
* When: Bind する
* Then: targets は `["a","b","c"]` になる

### BND-081 List クォート保護

* Given: `--targets` は List
* And: 入力 `select --targets="a,b"`
* When: Bind する
* Then: targets は `["a,b"]` になる

### BND-082 List 空要素

* Given: `--targets` は List
* And: 入力 `select --targets=`
* When: Bind する
* Then: targets は `[""]` になる

### BND-083 List 繰り返し指定不可

* Given: `--targets` は List
* And: 入力 `select --targets=a --targets=b`
* When: Bind する
* Then: BindError（使用法エラー）になる（exitCode=2）

### BND-090 `--`以降はオプション扱いしない

* Given: `--targets` は既知オプション
* And: 入力 `cmd -- --targets=a,b`
* When: Bind する
* Then: `--targets=a,b` は位置引数として残り、unknown option エラーにならない

---

## 4. Executor AC（行単位パイプ・停止・stderr）

### EXE-001 行単位パイプ

* Given: `echo` は複数行を stdout に出せる
* And: `grep` は stdin を行単位で受け取りフィルタする
* And: 入力 `echo foo | grep --pattern=foo`
* When: Execute する
* Then: grep は `foo` 行を stdout に出力する
* And: exitCode は 0 になる

### EXE-020 unknown optionでパイプ停止

* Given: `grep` は `--unknown` を持たない
* And: 入力 `echo foo | grep --unknown=1 | echo bar`
* When: Execute する
* Then: `grep` で BindError になり、以降の `echo bar` は実行されない
* And: stderr に `grep` ヘルプが出力される
* And: exitCode は 2 になる

### EXE-030 Cancellation

* Given: 長時間動作するコマンド `longrun` が存在する
* And: 入力 `longrun | grep --pattern=a`
* When: 実行中に CancellationToken を cancel する
* Then: パイプラインは早期終了する
* And: 以降ステージは実行されない（またはキャンセルされる）

---

## 5. リダイレクト／パス AC（`~`、WD、絶対、..、UTF-8）

### IO-001 `<` stdin差し替え

* Given: `in.txt` に `foo` と `bar` がUTF-8で保存されている
* And: 入力 `cat < in.txt`
* When: Execute する
* Then: stdout に `foo` と `bar` が行として出力される

### IO-010 `>` 上書き

* Given: 出力先 `out.txt` が存在してもよい
* And: 入力 `echo foo > out.txt`
* When: Execute する
* Then: `out.txt` の内容は `foo` のみになる（上書き）

### IO-011 `>>` 追記

* Given: `out.txt` に `a` が存在する
* And: 入力 `echo b >> out.txt`
* When: Execute する
* Then: `out.txt` は末尾に `b` が追記される

### IO-030 `~` 展開

* Given: HomeDirectory は `Application.persistentDataPath`
* And: `~/in.txt` が存在する
* When: `cat < ~/in.txt` を Execute する
* Then: persistentDataPath 配下の in.txt を参照する

### IO-040 WorkingDirectory 相対パス

* Given: WorkingDirectory が `X` に設定されている
* And: `X/in.txt` が存在する
* When: `cat < in.txt` を Execute する
* Then: `X/in.txt` を参照する

### IO-050 絶対パス許可

* Given: `/abs/path/in.txt` が存在する（環境に応じて準備）
* When: `cat < /abs/path/in.txt` を Execute する
* Then: 絶対パスを参照できる

### IO-051 `..` 許可

* Given: `../in.txt` が存在する
* When: `cat < ../in.txt` を Execute する
* Then: `..` を含むパスを参照できる

---

## 6. 出力レイヤ AC（`\n`→`<br/>`）

### OUT-001 変換は出力レイヤのみ

* Given: 内部データ（トークンやParsed/Bound）では改行変換を行わない
* When: stdout/stderr writer が文字列を出力する
* Then: 実改行（LF）が含まれる場合のみ `<br/>` に変換される

---

## 7. Completion（Tab補完）AC

### CMP-001 コマンド名補完

* Given: 登録コマンドに `grep` がある
* And: 入力末尾が `gr`
* When: CompletionEngine を呼ぶ
* Then: 候補一覧に `grep` が含まれる

### CMP-010 オプション名補完（long）

* Given: `grep` の既知オプションに `--pattern` がある
* And: 入力末尾が `grep --p`
* When: CompletionEngine を呼ぶ
* Then: 候補一覧に `--pattern` が含まれる

### CMP-020 パス補完

* Given: WorkingDirectory配下に `in.txt` がある
* And: 入力末尾が `cat < i`
* When: CompletionEngine を呼ぶ
* Then: 候補一覧に `in.txt`（または `./in.txt`）が含まれる

### CMP-030 置換規約（全置換）

* Given: 入力末尾が `grep --pat`
* When: `--pattern` を確定する
* Then: 末尾トークン `--pat` は `--pattern` に置換される

---

## 8. エラー統合 AC

### ERR-001 値不足（値必須オプション）

* Given: `--pattern` は値必須
* When: `grep --pattern` を Bind する
* Then: BindError（exitCode=2）となり、ヘルプが stderr に出力される

### ERR-030 コマンド未発見

* Given: `noSuchCmd` は登録されていない
* When: `noSuchCmd` を Bind/Execute する
* Then: 使用法エラーとして扱われる（exitCode=2相当）
* And: stderr にエラー文（またはグローバルヘルプ）が出力される

---

## 付記：仕様固定が必要な点（ACとして明示する場合）

* タブ文字を入力として受け取った場合の扱い（推奨：ParseError/exitCode=2）
* `\n`→`<br/>` の対象は「実改行（LF）のみ」とする（推奨）

---
