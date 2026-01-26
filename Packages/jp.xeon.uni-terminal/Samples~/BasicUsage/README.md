# UniTerminal Basic Usage Sample

This sample demonstrates the basic usage of UniTerminal.

## Contents

### Scripts

- **TerminalExample.cs** - Basic terminal initialization and command execution
- **CustomCommandExample.cs** - How to create custom commands

## Usage

1. Import this sample via Package Manager
2. Add the `TerminalExample` component to any GameObject
3. Enter Play Mode
4. Use the Context Menu (right-click on component) to run example commands

## Custom Commands

To register custom commands:

```csharp
// Get or create a Terminal instance
var terminal = new Terminal(
    workingDirectory: Application.dataPath,
    homeDirectory: Application.dataPath,
    registerBuiltInCommands: true
);

// Output writers
var stdout = new StringBuilderTextWriter();
var stderr = new StringBuilderTextWriter();
var ct = CancellationToken.None;

// Register custom commands
terminal.Registry.Register<GreetCommand>();
terminal.Registry.Register<CountCommand>();

// Now you can use them
await terminal.ExecuteAsync("greet --name=Unity --times=3", stdout, stderr, ct);
await terminal.ExecuteAsync("echo Hello World | count --words", stdout, stderr, ct);
```

## Example Commands

```bash
# Basic greeting
greet --name=Unity

# Greet multiple times in uppercase
greet -n Unity -t 3 -u

# Count words in pipeline
echo "Hello World from UniTerminal" | count --words

# Count lines
hierarchy -r | count
```
