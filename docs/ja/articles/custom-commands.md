# カスタムコマンド

UniTerminalで独自のコマンドを作成して登録する方法を説明します。

## 基本的なコマンド構造

すべてのコマンドは `ICommand` インターフェースを実装する必要があります:

```csharp
using Xeon.UniTerminal;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[Command("mycommand", "コマンドの説明")]
public class MyCommand : ICommand
{
    public string CommandName => "mycommand";
    public string Description => "コマンドの説明";

    public async Task<ExitCode> ExecuteAsync(
        CommandContext context,
        CancellationToken ct)
    {
        await context.Stdout.WriteLineAsync("Hello from MyCommand!", ct);
        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context)
    {
        yield break;
    }
}
```

## オプションの追加

`[Option]` 属性を使用してコマンドラインオプションを定義します:

```csharp
[Command("greet", "ユーザーに挨拶する")]
public class GreetCommand : ICommand
{
    [Option("name", "n", Description = "挨拶する名前")]
    public string Name;

    [Option("times", "t", Description = "挨拶する回数")]
    public int Times = 1;

    [Option("uppercase", "u", Description = "大文字で出力")]
    public bool Uppercase;

    public string CommandName => "greet";
    public string Description => "ユーザーに挨拶する";

    public async Task<ExitCode> ExecuteAsync(
        CommandContext context,
        CancellationToken ct)
    {
        var greeting = $"Hello, {Name ?? "World"}!";

        if (Uppercase)
            greeting = greeting.ToUpper();

        for (int i = 0; i < Times; i++)
        {
            await context.Stdout.WriteLineAsync(greeting, ct);
        }

        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context)
    {
        yield break;
    }
}
```

**使用方法:**

```bash
greet                          # Hello, World!
greet -n Alice                 # Hello, Alice!
greet --name=Bob --times=3     # Hello, Bob! (3回)
greet -n Unity -t 2 -u         # HELLO, UNITY! (2回)
```

## オプションの型

### サポートされる型

| 型 | 例 | 使用方法 |
|------|---------|-------|
| `string` | `"hello"` | `--option=value` |
| `int` | `42` | `--count=42` |
| `float` | `3.14` | `--value=3.14` |
| `bool` | `true/false` | `--flag`（存在 = true） |
| `Vector2` | `1,2` | `--pos=1,2` |
| `Vector3` | `1,2,3` | `--pos=1,2,3` |
| `Color` | `1,0,0,1` | `--color=1,0,0,1` |

### 位置引数

オプションフラグのない引数は、`context.PositionalArguments` に位置引数として渡されます:

```csharp
[Command("move", "位置に移動")]
public class MoveCommand : ICommand
{
    public string CommandName => "move";
    public string Description => "位置に移動";

    public async Task<ExitCode> ExecuteAsync(
        CommandContext context,
        CancellationToken ct)
    {
        // move /Player 1,2,3
        // context.PositionalArguments[0] = "/Player"
        // context.PositionalArguments[1] = "1,2,3"

        if (context.PositionalArguments.Count < 2)
        {
            await context.Stderr.WriteLineAsync(
                "Usage: move <object> <position>", ct);
            return ExitCode.UsageError;
        }

        var objectPath = context.PositionalArguments[0];
        var position = context.PositionalArguments[1];

        // ... 実装

        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context)
    {
        yield break;
    }
}
```

### サブコマンドパターン

サブコマンドを持つコマンドでは、最初の位置引数をサブコマンドとして使用し、残りを引数として処理できます:

```csharp
// 例: go create MyObject -p Cube
// → PositionalArguments[0] = "create" (サブコマンド)
// → PositionalArguments[1] = "MyObject" (引数)

public async Task<ExitCode> ExecuteAsync(
    CommandContext context,
    CancellationToken ct)
{
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

## 標準入力からの読み取り

パイプラインで前のコマンドからの入力を処理します:

```csharp
[Command("count", "行数または単語数をカウント")]
public class CountCommand : ICommand
{
    [Option("words", "w", Description = "行の代わりに単語をカウント")]
    public bool CountWords;

    public string CommandName => "count";
    public string Description => "行数または単語数をカウント";

    public async Task<ExitCode> ExecuteAsync(
        CommandContext context,
        CancellationToken ct)
    {
        int count = 0;
        string line;

        while ((line = await context.Stdin.ReadLineAsync(ct)) != null)
        {
            if (CountWords)
            {
                count += line.Split(
                    new[] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries
                ).Length;
            }
            else
            {
                count++;
            }
        }

        await context.Stdout.WriteLineAsync(count.ToString(), ct);
        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context)
    {
        yield break;
    }
}
```

**使用方法:**

```bash
hierarchy -r | count           # オブジェクト数をカウント
echo "Hello World" | count -w  # 単語数をカウント（2）
cat file.txt | count           # ファイルの行数をカウント
```

## タブ補完

`GetCompletions` を実装してコンテキストに応じた補完候補を提供します:

```csharp
[Command("load", "シーンをロード")]
public class LoadSceneCommand : ICommand
{
    [Option("scene", "s", Description = "シーン名")]
    public string SceneName;

    public string CommandName => "load";
    public string Description => "シーンをロード";

    public async Task<ExitCode> ExecuteAsync(
        CommandContext context,
        CancellationToken ct)
    {
        // ... 実装
        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context)
    {
        // --sceneオプションの補完かどうかをチェック
        if (context.CurrentOption == "scene" ||
            context.CurrentOption == "s")
        {
            // 利用可能なシーン名を返す
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);

                if (name.StartsWith(context.PartialValue,
                    StringComparison.OrdinalIgnoreCase))
                {
                    yield return name;
                }
            }
        }
    }
}
```

## コマンドの登録

### 手動登録

```csharp
var terminal = new Terminal(
    Application.dataPath,
    Application.dataPath,
    registerBuiltInCommands: true
);

// 個別のコマンドを登録
terminal.Registry.Register<GreetCommand>();
terminal.Registry.Register<CountCommand>();
terminal.Registry.Register<LoadSceneCommand>();
```

### アセンブリ登録

アセンブリからすべてのコマンドを登録:

```csharp
// 現在のアセンブリからすべてのコマンドを登録
terminal.Registry.RegisterFromAssembly(
    typeof(MyCommand).Assembly
);

// 複数のアセンブリから登録
terminal.Registry.RegisterFromAssembly(
    typeof(GameCommands).Assembly
);
terminal.Registry.RegisterFromAssembly(
    typeof(DebugCommands).Assembly
);
```

## 終了コード

適切な終了コードを返します:

| コード | 定数 | 使用場面 |
|------|----------|-------------|
| 0 | `ExitCode.Success` | コマンドが正常に完了 |
| 1 | `ExitCode.UsageError` | 引数または使用方法が無効 |
| 2 | `ExitCode.RuntimeError` | 実行時エラーが発生 |

```csharp
public async Task<ExitCode> ExecuteAsync(
    CommandContext context,
    CancellationToken ct)
{
    if (context.PositionalArguments.Count == 0)
    {
        await context.Stderr.WriteLineAsync(
            "Error: 必須の引数がありません", ct);
        return ExitCode.UsageError;
    }

    try
    {
        // ... 処理を実行
        return ExitCode.Success;
    }
    catch (Exception ex)
    {
        await context.Stderr.WriteLineAsync(
            $"Error: {ex.Message}", ct);
        return ExitCode.RuntimeError;
    }
}
```

## CommandContext プロパティ

| プロパティ | 型 | 説明 |
|----------|------|------|
| `Stdin` | `IAsyncTextReader` | 入力ストリーム |
| `Stdout` | `IAsyncTextWriter` | 出力ストリーム |
| `Stderr` | `IAsyncTextWriter` | エラーストリーム |
| `PositionalArguments` | `IReadOnlyList<string>` | 位置引数 |
| `WorkingDirectory` | `string` | 現在の作業ディレクトリ |
| `HomeDirectory` | `string` | ホームディレクトリ |
| `Terminal` | `Terminal` | Terminalインスタンス |

## ベストプラクティス

1. **async/awaitを適切に使用** - メインスレッドをブロックしない
2. **キャンセルをサポート** - `ct.IsCancellationRequested` をチェック
3. **エラーはStderrに書き込む** - Stdoutはデータ出力用に保持
4. **正しい終了コードを返す** - 適切なパイプライン処理のため
5. **補完を実装** - より良いユーザーエクスペリエンス
6. **コマンドは集中させる** - 1つのコマンド、1つの目的
