# UniTask Support

UniTerminal provides optional integration with [UniTask](https://github.com/Cysharp/UniTask) for high-performance async operations.

## Overview

UniTask support is **automatically enabled** when UniTask is installed in your project. No additional configuration is required.

When UniTask is detected, the `UNI_TERMINAL_UNI_TASK_SUPPORT` symbol is defined automatically.

## Installation

### Install UniTask

Add UniTask to your project via Package Manager:

```
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

Or via manifest.json:

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

UniTerminal will automatically detect UniTask and enable support.

## Using UniTask Execution

### Basic Usage

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

### UniTask Text Writers

UniTerminal provides specialized text writers for UniTask:

| Class | Description |
|-------|-------------|
| `UniTaskStringBuilderTextWriter` | Writes to StringBuilder |
| `UniTaskListTextWriter` | Writes to List<string> |

```csharp
// StringBuilder output
var stdout = new UniTaskStringBuilderTextWriter();
await terminal.ExecuteUniTaskAsync("echo Hello", stdout, stderr);
string result = stdout.ToString();

// List output (line by line)
var listWriter = new UniTaskListTextWriter();
await terminal.ExecuteUniTaskAsync("hierarchy -r", listWriter, stderr);
foreach (var line in listWriter.Lines)
{
    Debug.Log(line);
}
```

## Creating UniTask Commands

Implement `IUniTaskCommand` for commands that use UniTask:

```csharp
using Cysharp.Threading.Tasks;
using Xeon.UniTerminal;
using System.Collections.Generic;
using System.Threading;

[Command("download", "Download a resource")]
public class DownloadCommand : IUniTaskCommand
{
    [Option("url", "u", Description = "URL to download")]
    public string Url;

    public string CommandName => "download";
    public string Description => "Download a resource";

    public async UniTask<ExitCode> ExecuteAsync(
        UniTaskCommandContext context,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(Url))
        {
            await context.Stderr.WriteLineAsync("Error: URL required", ct);
            return ExitCode.UsageError;
        }

        await context.Stdout.WriteLineAsync($"Downloading: {Url}", ct);

        // Use UniTask's async operations
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

The `UniTaskCommandContext` provides UniTask-compatible I/O:

| Property | Type | Description |
|----------|------|-------------|
| `Stdin` | `IUniTaskTextReader` | Input stream |
| `Stdout` | `IUniTaskTextWriter` | Output stream |
| `Stderr` | `IUniTaskTextWriter` | Error stream |
| `Arguments` | `IReadOnlyList<string>` | Positional arguments |
| `WorkingDirectory` | `string` | Current working directory |
| `HomeDirectory` | `string` | Home directory |
| `Terminal` | `Terminal` | Terminal instance |

## Interface Reference

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

## Mixing Standard and UniTask Commands

UniTerminal seamlessly handles both standard `ICommand` and `IUniTaskCommand`:

```csharp
// Register both types
terminal.Registry.Register<StandardCommand>();  // ICommand
terminal.Registry.Register<UniTaskCommand>();   // IUniTaskCommand

// Execute with UniTask - both work
await terminal.ExecuteUniTaskAsync("standard-cmd | unitask-cmd", stdout, stderr);
```

## Performance Benefits

UniTask provides:

- **Zero allocation** async/await
- **Better performance** than Task-based async
- **Unity-optimized** timing and scheduling
- **Cancellation support** integrated with Unity lifecycle

## Conditional Compilation

If you need to write code that works with or without UniTask:

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

## Best Practices

1. **Use UniTask for UI** - Better performance for terminal output display
2. **Leverage cancellation** - Pass CancellationToken properly
3. **Use appropriate writers** - `UniTaskListTextWriter` for line-by-line processing
4. **Don't block** - Use `await` instead of `.Result` or `.Wait()`
