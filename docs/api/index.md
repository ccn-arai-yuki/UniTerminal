# API Reference

This section contains the API documentation generated from source code comments.

## Interfaces

| Interface | Description |
|-----------|-------------|
| [ICommand](Xeon.UniTerminal.ICommand.html) | Command implementation interface |
| [IAsyncTextReader](Xeon.UniTerminal.IAsyncTextReader.html) | Async text input stream |
| [IAsyncTextWriter](Xeon.UniTerminal.IAsyncTextWriter.html) | Async text output stream |

## Attributes

| Attribute | Description |
|-----------|-------------|
| [CommandAttribute](Xeon.UniTerminal.CommandAttribute.html) | Marks a class as a command |
| [OptionAttribute](Xeon.UniTerminal.OptionAttribute.html) | Marks a field as a command option |

## Enums

| Enum | Description |
|------|-------------|
| [ExitCode](Xeon.UniTerminal.ExitCode.html) | Command exit codes |

> **Note:** Some classes that depend on Unity-specific assemblies (UnityEngine, TMPro) are not included in the API documentation. These include `Terminal`, `CommandRegistry`, `CommandContext`, `UniTask` support classes, and `FlyweightScrollView` components. Please refer to the source code for details on these classes.
