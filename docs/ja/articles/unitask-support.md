# UniTaskサポート

UniTerminalは、高パフォーマンスな非同期操作のために[UniTask](https://github.com/Cysharp/UniTask)とのオプション統合を提供します。

## 概要

UniTaskサポートは、UniTaskがプロジェクトにインストールされていると**自動的に有効**になります。追加の設定は必要ありません。

UniTaskが検出されると、`UNI_TERMINAL_UNI_TASK_SUPPORT` シンボルが自動的に定義されます。

## インストール

### UniTaskをインストール

Package Manager経由でUniTaskをプロジェクトに追加:

```
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

またはmanifest.json経由:

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

UniTerminalは自動的にUniTaskを検出してサポートを有効にします。

## UniTask実行の使用

### 基本的な使用方法

```csharp
using Cysharp.Threading.Tasks;
using Xeon.UniTerminal;

public class UniTaskExample : MonoBehaviour
{
    private Terminal _terminal;

    void Start()
    {
        _terminal = new Terminal(
            Application.dataPath,
            Application.dataPath,
            true
        );

        ExecuteCommand().Forget();
    }

    async UniTaskVoid ExecuteCommand()
    {
        var stdout = new UniTaskStringBuilderTextWriter();
        var stderr = new UniTaskStringBuilderTextWriter();

        var exitCode = await _terminal.ExecuteUniTaskAsync(
            "hierarchy -r",
            stdout,
            stderr
        );

        Debug.Log(stdout.ToString());
    }
}
```

### UniTask テキストライター

UniTerminalはUniTask用の特殊なテキストライターを提供します:

| クラス | 説明 |
|-------|------|
| `UniTaskStringBuilderTextWriter` | StringBuilderに書き込み |
| `UniTaskListTextWriter` | List<string>に書き込み |

```csharp
// StringBuilder出力
var stdout = new UniTaskStringBuilderTextWriter();
await terminal.ExecuteUniTaskAsync("echo Hello", stdout, stderr);
string result = stdout.ToString();

// List出力（行単位）
var listWriter = new UniTaskListTextWriter();
await terminal.ExecuteUniTaskAsync("hierarchy -r", listWriter, stderr);
foreach (var line in listWriter.Lines)
{
    Debug.Log(line);
}
```

## UniTaskコマンドの作成

UniTaskを使用するコマンドには `IUniTaskCommand` を実装します:

```csharp
using Cysharp.Threading.Tasks;
using Xeon.UniTerminal;
using System.Collections.Generic;
using System.Threading;

[Command("download", "リソースをダウンロード")]
public class DownloadCommand : IUniTaskCommand
{
    [Option("url", "u", Description = "ダウンロードするURL")]
    public string Url;

    public string CommandName => "download";
    public string Description => "リソースをダウンロード";

    public async UniTask<ExitCode> ExecuteAsync(
        UniTaskCommandContext context,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(Url))
        {
            await context.Stderr.WriteLineAsync("Error: URLが必要です", ct);
            return ExitCode.UsageError;
        }

        await context.Stdout.WriteLineAsync($"ダウンロード中: {Url}", ct);

        // UniTaskの非同期操作を使用
        using var request = UnityWebRequest.Get(Url);
        await request.SendWebRequest().ToUniTask(cancellationToken: ct);

        if (request.result == UnityWebRequest.Result.Success)
        {
            await context.Stdout.WriteLineAsync(request.downloadHandler.text, ct);
            return ExitCode.Success;
        }
        else
        {
            await context.Stderr.WriteLineAsync($"Error: {request.error}", ct);
            return ExitCode.RuntimeError;
        }
    }

    public IEnumerable<string> GetCompletions(CompletionContext context)
    {
        yield break;
    }
}
```

## UniTaskCommandContext

`UniTaskCommandContext` はUniTask互換のI/Oを提供します:

| プロパティ | 型 | 説明 |
|----------|------|------|
| `Stdin` | `IUniTaskTextReader` | 入力ストリーム |
| `Stdout` | `IUniTaskTextWriter` | 出力ストリーム |
| `Stderr` | `IUniTaskTextWriter` | エラーストリーム |
| `PositionalArguments` | `IReadOnlyList<string>` | 位置引数 |
| `WorkingDirectory` | `string` | 現在の作業ディレクトリ |
| `HomeDirectory` | `string` | ホームディレクトリ |
| `Terminal` | `Terminal` | Terminalインスタンス |

## インターフェースリファレンス

### IUniTaskTextWriter

```csharp
public interface IUniTaskTextWriter
{
    UniTask WriteAsync(string value, CancellationToken ct = default);
    UniTask WriteLineAsync(string value, CancellationToken ct = default);
    UniTask WriteLineAsync(CancellationToken ct = default);
}
```

### IUniTaskTextReader

```csharp
public interface IUniTaskTextReader
{
    UniTask<string> ReadLineAsync(CancellationToken ct = default);
    UniTask<string> ReadToEndAsync(CancellationToken ct = default);
}
```

## 標準とUniTaskコマンドの混在

UniTerminalは標準の `ICommand` と `IUniTaskCommand` の両方をシームレスに処理します:

```csharp
// 両方のタイプを登録
terminal.Registry.Register<StandardCommand>();  // ICommand
terminal.Registry.Register<UniTaskCommand>();   // IUniTaskCommand

// UniTaskで実行 - 両方とも動作
await terminal.ExecuteUniTaskAsync("standard-cmd | unitask-cmd", stdout, stderr);
```

## パフォーマンスの利点

UniTaskは以下を提供します:

- **ゼロアロケーション** async/await
- **より良いパフォーマンス** Task ベースのasyncより
- **Unity最適化** のタイミングとスケジューリング
- **キャンセルサポート** Unityライフサイクルとの統合

## 条件付きコンパイル

UniTaskの有無に関わらず動作するコードを書く必要がある場合:

```csharp
#if UNI_TERMINAL_UNI_TASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

public class ConditionalExample : MonoBehaviour
{
#if UNI_TERMINAL_UNI_TASK_SUPPORT
    async UniTaskVoid Start()
    {
        var stdout = new UniTaskStringBuilderTextWriter();
        var stderr = new UniTaskStringBuilderTextWriter();
        await _terminal.ExecuteUniTaskAsync("help", stdout, stderr);
    }
#else
    async void Start()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        await _terminal.ExecuteAsync("help", stdout, stderr, destroyCancellationToken);
    }
#endif
}
```

## ベストプラクティス

1. **UIにはUniTaskを使用** - ターミナル出力表示のパフォーマンス向上
2. **キャンセルを活用** - CancellationTokenを適切に渡す
3. **適切なライターを使用** - 行単位の処理には `UniTaskListTextWriter`
4. **ブロックしない** - `.Result` や `.Wait()` の代わりに `await` を使用
