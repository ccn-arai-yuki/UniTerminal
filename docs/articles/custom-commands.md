# Custom Commands

Learn how to create and register your own commands in UniTerminal.

## Basic Command Structure

Every command must implement the `ICommand` interface:

```csharp
using Xeon.UniTerminal;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[Command("mycommand", "Description of my command")]
public class MyCommand : ICommand
{
    public string CommandName => "mycommand";
    public string Description => "Description of my command";

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

## Adding Options

Use the `[Option]` attribute to define command-line options:

```csharp
[Command("greet", "Greet a user")]
public class GreetCommand : ICommand
{
    [Option("name", "n", Description = "Name to greet")]
    public string Name;

    [Option("times", "t", Description = "Number of times to greet")]
    public int Times = 1;

    [Option("uppercase", "u", Description = "Output in uppercase")]
    public bool Uppercase;

    public string CommandName => "greet";
    public string Description => "Greet a user";

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

**Usage:**

```bash
greet                          # Hello, World!
greet -n Alice                 # Hello, Alice!
greet --name=Bob --times=3     # Hello, Bob! (3 times)
greet -n Unity -t 2 -u         # HELLO, UNITY! (2 times)
```

## Option Types

### Supported Types

| Type | Example | Usage |
|------|---------|-------|
| `string` | `"hello"` | `--option=value` |
| `int` | `42` | `--count=42` |
| `float` | `3.14` | `--value=3.14` |
| `bool` | `true/false` | `--flag` (presence = true) |
| `Vector2` | `1,2` | `--pos=1,2` |
| `Vector3` | `1,2,3` | `--pos=1,2,3` |
| `Color` | `1,0,0,1` | `--color=1,0,0,1` |

### Positional Arguments

Arguments without option flags are passed as positional arguments in `context.Arguments`:

```csharp
[Command("move", "Move to position")]
public class MoveCommand : ICommand
{
    public string CommandName => "move";
    public string Description => "Move to position";

    public async Task<ExitCode> ExecuteAsync(
        CommandContext context,
        CancellationToken ct)
    {
        // move /Player 1,2,3
        // context.Arguments[0] = "/Player"
        // context.Arguments[1] = "1,2,3"

        if (context.Arguments.Count < 2)
        {
            await context.Stderr.WriteLineAsync(
                "Usage: move <object> <position>", ct);
            return ExitCode.UsageError;
        }

        var objectPath = context.Arguments[0];
        var position = context.Arguments[1];

        // ... implementation

        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context)
    {
        yield break;
    }
}
```

## Reading from Stdin

Process input from previous commands in a pipeline:

```csharp
[Command("count", "Count lines or words")]
public class CountCommand : ICommand
{
    [Option("words", "w", Description = "Count words instead of lines")]
    public bool CountWords;

    public string CommandName => "count";
    public string Description => "Count lines or words";

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

**Usage:**

```bash
hierarchy -r | count           # Count objects
echo "Hello World" | count -w  # Count words (2)
cat file.txt | count           # Count lines in file
```

## Tab Completion

Implement `GetCompletions` to provide context-aware suggestions:

```csharp
[Command("load", "Load a scene")]
public class LoadSceneCommand : ICommand
{
    [Option("scene", "s", Description = "Scene name")]
    public string SceneName;

    public string CommandName => "load";
    public string Description => "Load a scene";

    public async Task<ExitCode> ExecuteAsync(
        CommandContext context,
        CancellationToken ct)
    {
        // ... implementation
        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context)
    {
        // Check if completing the --scene option
        if (context.CurrentOption == "scene" ||
            context.CurrentOption == "s")
        {
            // Return available scene names
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

## Registering Commands

### Manual Registration

```csharp
var terminal = new Terminal(
    Application.dataPath,
    Application.dataPath,
    registerBuiltInCommands: true
);

// Register individual commands
terminal.Registry.Register<GreetCommand>();
terminal.Registry.Register<CountCommand>();
terminal.Registry.Register<LoadSceneCommand>();
```

### Assembly Registration

Register all commands from an assembly:

```csharp
// Register all commands in current assembly
terminal.Registry.RegisterFromAssembly(
    typeof(MyCommand).Assembly
);

// Register from multiple assemblies
terminal.Registry.RegisterFromAssembly(
    typeof(GameCommands).Assembly
);
terminal.Registry.RegisterFromAssembly(
    typeof(DebugCommands).Assembly
);
```

## Exit Codes

Return appropriate exit codes:

| Code | Constant | When to Use |
|------|----------|-------------|
| 0 | `ExitCode.Success` | Command completed successfully |
| 1 | `ExitCode.UsageError` | Invalid arguments or usage |
| 2 | `ExitCode.RuntimeError` | Runtime error occurred |

```csharp
public async Task<ExitCode> ExecuteAsync(
    CommandContext context,
    CancellationToken ct)
{
    if (context.Arguments.Count == 0)
    {
        await context.Stderr.WriteLineAsync(
            "Error: Missing required argument", ct);
        return ExitCode.UsageError;
    }

    try
    {
        // ... do work
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

## CommandContext Properties

| Property | Type | Description |
|----------|------|-------------|
| `Stdin` | `IAsyncTextReader` | Input stream |
| `Stdout` | `IAsyncTextWriter` | Output stream |
| `Stderr` | `IAsyncTextWriter` | Error stream |
| `Arguments` | `IReadOnlyList<string>` | Positional arguments |
| `WorkingDirectory` | `string` | Current working directory |
| `HomeDirectory` | `string` | Home directory |
| `Terminal` | `Terminal` | Terminal instance |

## Best Practices

1. **Use async/await properly** - Don't block the main thread
2. **Support cancellation** - Check `ct.IsCancellationRequested`
3. **Write errors to Stderr** - Keep Stdout for data output
4. **Return correct exit codes** - For proper pipeline handling
5. **Implement completions** - Better user experience
6. **Keep commands focused** - One command, one purpose
