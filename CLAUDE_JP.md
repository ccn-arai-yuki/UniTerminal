# CLAUDE.md

このファイルは、Claude Code (claude.ai/code) がこのリポジトリのコードを扱う際のガイダンスを提供します。

## コーディングガイドライン（Unity / C# – Claude向け）

このプロジェクトは **Unity と C#** をベースにしています。
以下のルールは、**Unityプロジェクトにおける可読性、パフォーマンス、デバッグ、長期的なメンテナンス**に関連する一般的な問題を防ぐために存在します。

コードを生成する際は、これらのルールの文字通りの意味だけでなく、**その意図に従い、尊重する必要があります**。

### 命名規則

* プライベートメソッドに `_` プレフィックスを使用**しない**。

**理由**

* C#ではアクセス修飾子（`private`、`public` など）で可視性を明確に表現できる。
* Unityプロジェクトでは、`_` プレフィックス付きの名前は一時変数、内部ハック、自動生成コードと混同されやすい。
* リファクタリング時のノイズを避け、IDEの自動補完の可読性を向上させる。

### ネストの制限

* `namespace` を除き、ネストは**最大3レベル**まで。
* 単一のネストブロック（`{}`）は**100行を超えない**。

  * 超える場合は、**意味のあるメソッドにロジックを分割する**。

**理由**

* MonoBehaviourクラスには多くのライフサイクルメソッドが含まれており、深いネストは動作の把握を困難にする。
* 深くネストされたコードは、デバッグ、ブレークポイント、スタックトレースを複雑にする。
* 実行フローだけでなく、**意図を伝える**コードを促進する。

### switch文の制限

* `switch` の `case` 内では、以下を**使用しない**：

  * `if`
  * `for`
  * `foreach`
  * `switch`

* 各 `case` は `break` を除いて**最大5行**まで。

**理由**

* `switch` 文は**明示的な状態または値の分岐**のみを表現すべき。
* `case` ブロック内にロジックを埋め込むと、制御フローと状態遷移が隠れる。
* 複雑な動作は**専用のメソッドに委譲**し、各 `case` をシンプルで読みやすく保つ。

### 設計の優先順位

実装の決定を行う際は、常に以下の順序で優先する：

1. **可読性**
2. **パフォーマンス**
3. **堅牢性**
4. **拡張性**

**理由**

* Unityプロジェクトでは、頻繁なイテレーション、チューニング、デバッグが必要になることが多い。
* GCアロケーション、フレームタイミング、プラットフォームの制約により、Unityではパフォーマンスが早期に重要になる。
* 最適化されているが読めないコードは、すぐにメンテナンスリスクになる。
* 拡張性は重要だが、明確さやランタイムパフォーマンスを犠牲にしてはならない。

### 制御文のスタイル

* `if`、`for`、`foreach` が1行で書ける場合でも、**必ず2行で書く**。
* 波括弧 `{}` の省略は許可されるが、**1行の制御文は禁止**。
* 非自明なロジックには波括弧を推奨。
* このルールは `switch` 文に適用され、`switch` 式には適用されない。

#### 例

```csharp
if (flag)
    return;

foreach (var element in array)
    Process(element);
```

**理由**

* Unityのデバッグでは、制御ブロック内にログやブレークポイントを追加することが頻繁にある。
* ロジックを拡張する際に波括弧を追加し忘れることによる将来のバグを防ぐ。
* よりクリーンな差分を生成し、コードレビューの明確さを向上させる。

## プロジェクト概要

UniTerminalは、LinuxライクなCLI実行フレームワークを提供するUnityパッケージです。パイプ（`|`）やリダイレクト（`>`、`>>`、`<`）などのシェル機能を持つ文字列ベースのコマンドをパースして実行します。

- **Unityバージョン**: 6000.0以上が必要（プロジェクトは6000.3.2f1を使用）
- **ターゲットフレームワーク**: .NET Standard 2.1、C# 9.0
- **パッケージパス**: `Packages/jp.xeon.uni-terminal/`
- **オプション**: UniTaskサポート（`UNI_TERMINAL_UNI_TASK_SUPPORT` defineで自動検出）

## ビルド＆テストコマンド

これはUnityプロジェクトです。ビルドとテストにはUnity EditorまたはUnity CLIを使用してください。

### テストの実行

**Unity Editor経由:**
- Window > General > Test Runner
- Edit Modeタブでユニットテスト
- Play Modeタブで統合テスト

**CLI経由:**
```bash
# Edit Modeテスト
Unity -runTests -testPlatform editmode -projectPath .

# Play Modeテスト
Unity -runTests -testPlatform playmode -projectPath .
```

### テスト構成
- `Packages/jp.xeon.uni-terminal/Tests/Editor/` - ユニットテスト（Parser、Tokenizer、Binder、Commands）
- `Packages/jp.xeon.uni-terminal/Tests/Runtime/` - Play Modeテスト（GameObject操作）

## アーキテクチャ

### コマンド実行パイプライン

```
Terminal.ExecuteAsync(input)
    → Parser.Parse()        # トークン化してParsedPipelineを構築
    → Binder.Bind()         # コマンド名の解決、オプションの検証
    → PipelineExecutor      # パイプでコマンドをチェーン、リダイレクト処理
    → Command.ExecuteAsync  # 個別コマンドの実行
    → ExitCode
```

### `Packages/jp.xeon.uni-terminal/Runtime/Scripts/` の主要ディレクトリ

| ディレクトリ | 目的 |
|-----------|---------|
| `Core/` | IAsyncTextReader/Writerインターフェース、CommandContext、ExitCode、PathUtility |
| `Commands/` | ICommandインターフェース、CommandRegistry、CommandAttribute、OptionAttribute |
| `BuiltInCommands/` | ファイル操作（echo、cat、grep、ls、cd、pwd、find、less、diff） |
| `UnityCommands/` | シーン操作（hierarchy、go、transform、component、property） |
| `Parsing/` | Parser、Tokenizer、Token型、ParsedPipeline構造体 |
| `Binding/` | Binder、BoundCommand、CommandBindingContext |
| `Execution/` | PipelineExecutor、ExecutionResult |
| `UI/` | UniTerminal MonoBehaviour、OutputWriter |
| `FlyweightScrollView/` | CircularBufferによる仮想スクロール（1000行リングバッファ） |
| `UniTask/` | UniTask非同期サポート（条件付きコンパイル） |

### カスタムコマンドの作成

```csharp
[Command("mycommand", "説明")]
public class MyCommand : ICommand
{
    [Option("message", "m", Description = "メッセージ")]
    public string Message;

    public string CommandName => "mycommand";
    public string Description => "説明";

    public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
    {
        await context.Stdout.WriteLineAsync(Message ?? "Hello!", ct);
        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context) => Enumerable.Empty<string>();
}

// 登録: terminal.Registry.Register<MyCommand>();
```

### サブコマンドパターン

`go`、`component`、`property` などのコマンドはサブコマンドを使用：
```csharp
var subCommand = context.PositionalArguments[0].ToLower();
var args = context.PositionalArguments.Skip(1).ToList();
return subCommand switch
{
    "create" => await CreateAsync(context, args, ct),
    "delete" => await DeleteAsync(context, args, ct),
    _ => ExitCode.UsageError
};
```

### テキストI/O抽象化

- `IAsyncTextReader`: FileTextReader、ListTextReader、EmptyTextReader
- `IAsyncTextWriter`: FileTextWriter、ListTextWriter、StringBuilderTextWriter、OutputWriter
- パイプラインサポート: `< input.txt`、`> output.txt`、`>> append.txt`、`| command`

### UIコンポーネント

`UniTerminal.cs`（MonoBehaviour）の役割：
- キーボード入力（Tab=補完、Up/Down=履歴、Enter=実行）
- 仮想スクロール用のFlyweightVerticalScrollView
- 固定サイズログ保存用のCircularBuffer

## アセンブリ定義

- `jp.xeon.uni-terminal.runtime.asmdef` - メインライブラリ
- `jp.xeon.uni-terminal.editor.asmdef` - エディタ専用コード
- `jp.xeon.uni-terminal.tests.runtime.asmdef` - ランタイムテスト
- `jp.xeon.uni-terminal.tests.editor.asmdef` - エディタテスト

## ExitCode値

- `ExitCode.Success` (0) - 正常終了
- `ExitCode.UsageError` (1) - 使用方法/引数エラー
- `ExitCode.RuntimeError` (2) - ランタイムエラー

## 言語

コードベースのコメントとドキュメントには日本語を使用しています。README.mdは日本語です。
