# UniTerminal

A string-based CLI execution framework for Unity with Linux-like behavior.

## Features

- **Linux-like Syntax** - Pipes (`|`), redirects (`>`, `>>`, `<`)
- **Built-in Commands** - File operations, text processing, Unity-specific commands
- **Extensible** - Easy custom command creation
- **Async Support** - async/await and UniTask integration
- **Tab Completion** - Context-aware command completion

## Quick Links

- [Getting Started](articles/getting-started.md)
- [Built-in Commands](articles/commands/index.md)
- [Custom Commands](articles/custom-commands.md)
- [API Reference](api/index.md)

## Installation

### Via Package Manager (Git URL)

```
https://github.com/AraiYuhki/UniTerminal.git?path=Packages/jp.xeon.uni-terminal
```

### Via Unity Asset Store

Search for "UniTerminal" in the Asset Store window.

## Basic Usage

```csharp
using Xeon.UniTerminal;

var terminal = new Terminal(
    workingDirectory: Application.dataPath,
    homeDirectory: Application.dataPath,
    registerBuiltInCommands: true
);

var stdout = new StringWriter();
var stderr = new StringWriter();

await terminal.ExecuteAsync("echo Hello, World!", stdout, stderr, ct);
```

## License

MIT License - see [LICENSE](https://github.com/AraiYuhki/UniTerminal/blob/main/Packages/jp.xeon.uni-terminal/LICENSE.md)
