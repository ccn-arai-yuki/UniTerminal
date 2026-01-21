# Getting Started

This guide will help you get UniTerminal up and running in your Unity project.

## Requirements

- Unity 6000.0 or later
- (Optional) UniTask 2.0+ for enhanced async support

## Installation

### Option 1: Unity Asset Store

1. Open **Window > Package Manager**
2. Select **My Assets** tab
3. Search for "UniTerminal"
4. Click **Import**

### Option 2: Git URL

1. Open **Window > Package Manager**
2. Click **+** > **Add package from git URL...**
3. Enter:

```
https://github.com/AraiYuhki/UniTerminal.git?path=Packages/jp.xeon.uni-terminal
```

### Option 3: manifest.json

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "jp.xeon.uni-terminal": "https://github.com/AraiYuhki/UniTerminal.git?path=Packages/jp.xeon.uni-terminal"
  }
}
```

## Quick Start

### 1. Create a Terminal Instance

```csharp
using Xeon.UniTerminal;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class TerminalExample : MonoBehaviour
{
    private Terminal _terminal;

    void Awake()
    {
        _terminal = new Terminal(
            workingDirectory: Application.dataPath,
            homeDirectory: Application.dataPath,
            registerBuiltInCommands: true
        );
    }
}
```

### 2. Execute Commands

```csharp
async Task RunCommand(CancellationToken ct)
{
    var stdout = new StringWriter();
    var stderr = new StringWriter();

    var exitCode = await _terminal.ExecuteAsync(
        "echo Hello, UniTerminal!",
        stdout,
        stderr,
        ct
    );

    if (exitCode == ExitCode.Success)
    {
        Debug.Log(stdout.ToString());
    }
    else
    {
        Debug.LogError(stderr.ToString());
    }
}
```

### 3. Use Pipelines

```csharp
// Chain multiple commands
await _terminal.ExecuteAsync(
    "hierarchy -r | grep Player",
    stdout, stderr, ct
);
```

### 4. Use Redirects

```csharp
// Output to file
await _terminal.ExecuteAsync(
    "hierarchy -r > hierarchy.txt",
    stdout, stderr, ct
);

// Append to file
await _terminal.ExecuteAsync(
    "echo NewLine >> output.txt",
    stdout, stderr, ct
);
```

## What's Next?

- [Built-in Commands](commands/index.md) - Learn available commands
- [Custom Commands](custom-commands.md) - Create your own commands
- [Pipeline & Redirects](pipeline-redirects.md) - Advanced usage
- [UniTask Support](unitask-support.md) - High-performance async
