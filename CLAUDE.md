# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Coding Guidelines (Unity / C# – for Claude)

This project is based on **Unity and C#**.
The following rules exist to prevent common issues related to **readability, performance, debugging, and long-term maintenance in Unity projects**.

When generating code, you **must follow these rules and respect their intent**, not just their literal wording.

### Naming Rules

* Do **not** use the `_` prefix for private methods.

**Reason**

* C# already clearly expresses visibility via access modifiers (`private`, `public`, etc.).
* In Unity projects, `_`-prefixed names are often confused with temporary variables, internal hacks, or auto-generated code.
* Avoids noise during refactoring and improves IDE autocomplete readability.

### Nesting Limits

* Excluding `namespace`, nesting is limited to **a maximum of 3 levels**.
* A single nested block (`{}`) must not exceed **100 lines**.

  * If it does, **split the logic into meaningful methods**.

**Reason**

* MonoBehaviour classes contain many lifecycle methods; deep nesting makes behavior hard to reason about.
* Deeply nested code complicates debugging, breakpoints, and stack tracing.
* Encourages code that communicates **intent**, not just execution flow.

### switch Statement Restrictions

* Inside a `switch` `case`, **do not use**:

  * `if`
  * `for`
  * `foreach`
  * `switch`

* Each `case` must be limited to **a maximum of 5 lines**, excluding `break`.

**Reason**

* `switch` statements should only represent **explicit state or value branching**.
* Embedding logic inside `case` blocks hides control flow and state transitions.
* Complex behavior should be delegated to **dedicated methods**, keeping each `case` simple and readable.

### Design Priority Order

When making implementation decisions, always prioritize in the following order:

1. **Readability**
2. **Performance**
3. **Robustness**
4. **Extensibility**

**Reason**

* Unity projects often require frequent iteration, tuning, and debugging.
* Performance matters early in Unity due to GC allocation, frame timing, and platform constraints.
* Optimized but unreadable code quickly becomes a maintenance risk.
* Extensibility is important, but not at the cost of clarity or runtime performance.

### Control Statement Style

* Even if an `if`, `for`, or `foreach` can be written on a single line, **always use two lines**.
* Omitting braces `{}` is allowed, but **single-line control statements are forbidden**.
* Braces are recommended for non-trivial logic.
* This rule applies to `switch` statements, not `switch` expressions.

#### Example

```csharp
if (flag)
    return;

foreach (var element in array)
    Process(element);
```

**Reason**

* Unity debugging frequently involves adding logs or breakpoints inside control blocks.
* Prevents future bugs caused by forgetting to add braces when extending logic.
* Produces cleaner diffs and improves code review clarity.

## Project Overview

UniTerminal is a Unity package providing a Linux-like CLI execution framework. It parses and executes string-based commands with shell features like pipes (`|`) and redirects (`>`, `>>`, `<`).

- **Unity Version**: 6000.0+ required (project uses 6000.3.2f1)
- **Target Framework**: .NET Standard 2.1, C# 9.0
- **Package Path**: `Packages/jp.xeon.uni-terminal/`
- **Optional**: UniTask support (auto-detected via `UNI_TERMINAL_UNI_TASK_SUPPORT` define)

## Build & Test Commands

This is a Unity project. Use Unity Editor or Unity CLI for building and testing.

### Running Tests

**Via Unity Editor:**
- Window > General > Test Runner
- Edit Mode tab for unit tests
- Play Mode tab for integration tests

**Via CLI:**
```bash
# Edit Mode tests
Unity -runTests -testPlatform editmode -projectPath .

# Play Mode tests
Unity -runTests -testPlatform playmode -projectPath .
```

### Test Structure
- `Packages/jp.xeon.uni-terminal/Tests/Editor/` - Unit tests (Parser, Tokenizer, Binder, Commands)
- `Packages/jp.xeon.uni-terminal/Tests/Runtime/` - Play Mode tests (GameObject operations)

## Architecture

### Command Execution Pipeline

```
Terminal.ExecuteAsync(input)
    → Parser.Parse()        # Tokenize and build ParsedPipeline
    → Binder.Bind()         # Resolve command names, validate options
    → PipelineExecutor      # Chain commands with pipes, handle redirects
    → Command.ExecuteAsync  # Individual command execution
    → ExitCode
```

### Key Directories in `Packages/jp.xeon.uni-terminal/Runtime/Scripts/`

| Directory | Purpose |
|-----------|---------|
| `Core/` | IAsyncTextReader/Writer interfaces, CommandContext, ExitCode, PathUtility |
| `Commands/` | ICommand interface, CommandRegistry, CommandAttribute, OptionAttribute |
| `BuiltInCommands/` | File operations (echo, cat, grep, ls, cd, pwd, find, less, diff) |
| `UnityCommands/` | Scene manipulation (hierarchy, go, transform, component, property) |
| `Parsing/` | Parser, Tokenizer, Token types, ParsedPipeline structures |
| `Binding/` | Binder, BoundCommand, CommandBindingContext |
| `Execution/` | PipelineExecutor, ExecutionResult |
| `UI/` | UniTerminal MonoBehaviour, OutputWriter |
| `FlyweightScrollView/` | Virtual scrolling with CircularBuffer (1000-line ring buffer) |
| `UniTask/` | UniTask async support (conditional compilation) |

### Creating Custom Commands

```csharp
[Command("mycommand", "Description")]
public class MyCommand : ICommand
{
    [Option("message", "m", Description = "Message")]
    public string Message;

    public string CommandName => "mycommand";
    public string Description => "Description";

    public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
    {
        await context.Stdout.WriteLineAsync(Message ?? "Hello!", ct);
        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context) => Enumerable.Empty<string>();
}

// Register: terminal.Registry.Register<MyCommand>();
```

### Subcommand Pattern

Commands like `go`, `component`, `property` use subcommands:
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

### Text I/O Abstraction

- `IAsyncTextReader`: FileTextReader, ListTextReader, EmptyTextReader
- `IAsyncTextWriter`: FileTextWriter, ListTextWriter, StringBuilderTextWriter, OutputWriter
- Pipeline supports: `< input.txt`, `> output.txt`, `>> append.txt`, `| command`

### UI Component

`UniTerminal.cs` (MonoBehaviour) handles:
- Keyboard input (Tab=completion, Up/Down=history, Enter=execute)
- FlyweightVerticalScrollView for virtual scrolling
- CircularBuffer for fixed-size log storage

## Assembly Definitions

- `jp.xeon.uni-terminal.runtime.asmdef` - Main library
- `jp.xeon.uni-terminal.editor.asmdef` - Editor-only code
- `jp.xeon.uni-terminal.tests.runtime.asmdef` - Runtime tests
- `jp.xeon.uni-terminal.tests.editor.asmdef` - Editor tests

## ExitCode Values

- `ExitCode.Success` (0) - Normal completion
- `ExitCode.UsageError` (1) - Usage/argument error
- `ExitCode.RuntimeError` (2) - Runtime error

## Language

The codebase uses Japanese for comments and documentation. README.md is in Japanese.
