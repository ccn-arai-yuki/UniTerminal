# UniTerminal

Unity向けのLinuxライクなCLI実行フレームワークです。文字列ベースのコマンドを解析・実行し、パイプラインやリダイレクトなどのシェル機能をサポートします。

## 特徴

- **Linuxライクなコマンド構文**: パイプ (`|`)、リダイレクト (`>`, `>>`, `<`) をサポート
- **豊富な組み込みコマンド**: ファイル操作、テキスト処理、Unity固有のコマンドを提供
- **拡張可能**: カスタムコマンドを簡単に追加可能
- **非同期実行**: async/awaitによる非同期コマンド実行
- **UniTaskサポート**: UniTaskを使用した高パフォーマンスな非同期処理（オプション）
- **タブ補完**: コマンドやパスの補完機能
- **FlyweightScrollView**: 大量のログ表示に対応した仮想スクロールビュー
- **Ctrl+Cキャンセル**: 長時間実行コマンドの中断機能

## 動作要件

- Unity 6000.0 以上
- （オプション）UniTask 2.0 以上

## ドキュメント

詳細なコマンドリファレンスやAPIドキュメントは以下を参照してください：

- **リファレンス**: https://araiyuhki.github.io/UniTerminal_Reference/index.html

## インストール

### Package Manager経由

1. Window > Package Manager を開く
2. 「+」ボタン > 「Add package from git URL...」を選択
3. 以下のURLを入力:
```
https://github.com/AraiYuhki/UniTerminal.git?path=Packages/jp.xeon.uni-terminal
```

### manifest.json経由

`Packages/manifest.json` に以下を追加:
```json
{
  "dependencies": {
    "jp.xeon.uni-terminal": "https://github.com/AraiYuhki/UniTerminal.git?path=Packages/jp.xeon.uni-terminal"
  }
}
```

### UniTaskサポートを有効にする

UniTaskがプロジェクトにインストールされている場合、自動的にUniTaskサポートが有効になります。

1. UniTaskをインストール（OpenUPM経由推奨）:
```
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

2. UniTerminalがUniTaskを検出すると、`UNI_TERMINAL_UNI_TASK_SUPPORT` シンボルが自動定義されます

## 基本的な使い方

### Terminalの初期化

```csharp
using Xeon.UniTerminal;

// Terminalインスタンスを作成
var terminal = new Terminal(
    workingDirectory: Application.dataPath,
    homeDirectory: Application.dataPath,
    registerBuiltInCommands: true  // 組み込みコマンドを登録
);
```

### コマンドの実行

```csharp
using Xeon.UniTerminal;

// 出力用のIAsyncTextWriter
var stdout = new StringBuilderTextWriter();
var stderr = new StringBuilderTextWriter();

// コマンドを実行
var exitCode = await terminal.ExecuteAsync("echo Hello, World!", stdout, stderr);

// 結果を取得
Debug.Log(stdout.ToString());  // "Hello, World!"
```

### UniTaskを使用したコマンド実行

```csharp
using Cysharp.Threading.Tasks;
using Xeon.UniTerminal;

// UniTask版の非同期実行
var exitCode = await terminal.ExecuteUniTaskAsync("echo Hello!", stdout, stderr);
```

### パイプラインの使用

```csharp
// コマンドをパイプでつなげる
await terminal.ExecuteAsync("cat myfile.txt | grep --pattern=error | less", stdout, stderr);
```

### リダイレクト

```csharp
// ファイルへの出力
await terminal.ExecuteAsync("echo Hello > output.txt", stdout, stderr);

// ファイルへの追記
await terminal.ExecuteAsync("echo World >> output.txt", stdout, stderr);

// ファイルからの入力
await terminal.ExecuteAsync("grep --pattern=pattern < input.txt", stdout, stderr);
```

### 変数の使用

```csharp
// 変数を設定
await terminal.ExecuteAsync("set NAME=Player1", stdout, stderr);

// 変数を参照
await terminal.ExecuteAsync("echo $NAME", stdout, stderr);  // "Player1"

// 変数一覧を表示
await terminal.ExecuteAsync("env", stdout, stderr);

// 変数を削除
await terminal.ExecuteAsync("unset NAME", stdout, stderr);
```

## 組み込みコマンド

UniTerminalには多数の組み込みコマンドが用意されています。各コマンドの詳細なオプションや使用例については、[リファレンスドキュメント](https://araiyuhki.github.io/UniTerminal_Reference/index.html)を参照してください。

### コマンド一覧

| カテゴリ | コマンド |
|----------|----------|
| ファイル操作 | `pwd`, `cd`, `ls`, `cat`, `find`, `less`, `diff`, `head`, `tail` |
| テキスト処理 | `echo`, `grep` |
| ユーティリティ | `help`, `history`, `clear`, `set`, `unset`, `env` |
| Unity操作 | `hierarchy`, `go`, `transform`, `component`, `property`, `scene`, `log` |
| アセット管理 | `asset`, `assetdb`, `adr`, `res` |

コマンドのヘルプを確認するには、`help <コマンド名>` を実行してください：

```bash
help ls
help hierarchy
```

## カスタムコマンドの作成

### 基本的なコマンド

```csharp
using Xeon.UniTerminal;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

[Command("greet", "Greet the user")]
public class GreetCommand : ICommand
{
    [Option("name", "n", Description = "Name to greet")]
    public string Name;

    public string CommandName => "greet";
    public string Description => "Greet the user";

    public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
    {
        var name = Name ?? "World";
        await context.Stdout.WriteLineAsync($"Hello, {name}!", ct);
        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context)
    {
        yield break;
    }
}
```

### コマンドの登録

```csharp
// 手動で登録
terminal.Registry.Register<GreetCommand>();

// または、アセンブリから自動登録
terminal.Registry.RegisterFromAssembly(typeof(GreetCommand).Assembly);
```

### 使用例

```bash
greet              # "Hello, World!"
greet -n Alice     # "Hello, Alice!"
greet --name=Bob   # "Hello, Bob!"
```

### UniTask対応コマンド

UniTaskを使用する場合は `IUniTaskCommand` インターフェースを実装します:

```csharp
using Cysharp.Threading.Tasks;
using Xeon.UniTerminal;

[Command("delay", "Wait for specified time")]
public class DelayCommand : IUniTaskCommand
{
    [Option("ms", "m", Description = "Delay in milliseconds")]
    public int Milliseconds = 1000;

    public string CommandName => "delay";
    public string Description => "Wait for specified time";

    public async UniTask<ExitCode> ExecuteAsync(UniTaskCommandContext context, CancellationToken ct)
    {
        await context.Stdout.WriteLineAsync($"Waiting {Milliseconds}ms...", ct);
        await UniTask.Delay(Milliseconds, cancellationToken: ct);
        await context.Stdout.WriteLineAsync("Done!", ct);
        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context)
    {
        yield break;
    }
}
```

### 位置引数とサブコマンド

オプション以外の引数は `context.PositionalArguments` で取得できます：

```csharp
public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
{
    // 例: mycommand create something
    // → PositionalArguments = ["create", "something"]

    if (context.PositionalArguments.Count == 0)
    {
        await context.Stderr.WriteLineAsync("サブコマンドを指定してください", ct);
        return ExitCode.UsageError;
    }

    var subCommand = context.PositionalArguments[0].ToLower();
    var args = context.PositionalArguments.Skip(1).ToList();

    return subCommand switch
    {
        "create" => await CreateAsync(context, args, ct),
        "delete" => await DeleteAsync(context, args, ct),
        _ => ExitCode.UsageError
    };
}
```

詳細なコマンド作成方法については、[カスタムコマンドガイド](https://araiyuhki.github.io/UniTerminal_Reference/articles/custom-commands.html)を参照してください。

## 終了コード

| コード | 説明 |
|--------|------|
| `ExitCode.Success` (0) | 正常終了 |
| `ExitCode.UsageError` (1) | 使用方法エラー |
| `ExitCode.RuntimeError` (2) | 実行時エラー |

## ライセンス

MIT OR Apache-2.0（デュアルライセンス）

詳細は [LICENSE.md](Packages/jp.xeon.uni-terminal/LICENSE.md) を参照してください。

## 作者

Xeon
