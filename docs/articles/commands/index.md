# Built-in Commands

UniTerminal provides a comprehensive set of built-in commands organized into categories.

## Command Categories

| Category | Commands | Description |
|----------|----------|-------------|
| [File Operations](file-operations.md) | `pwd`, `cd`, `ls`, `cat`, `find`, `less`, `diff` | Navigate and manipulate files |
| [Text Processing](text-processing.md) | `echo`, `grep` | Process and filter text |
| [Utilities](#utilities) | `help`, `history` | General utilities |
| [Unity Commands](unity-commands.md) | `hierarchy`, `go`, `transform`, `component`, `property` | Unity-specific operations |

## Utilities

### help

Display help information for commands.

```bash
# List all commands
help

# Get help for specific command
help ls
help hierarchy
```

### history

Manage command history.

```bash
# Show command history
history

# Show last N commands
history -n 10

# Clear history
history -c

# Delete specific entry
history -d 5

# Read history from file
history -r ~/.terminal_history
```

**Options:**

| Option | Description |
|--------|-------------|
| `-c` | Clear history |
| `-d <index>` | Delete entry at index |
| `-n <count>` | Show last N entries |
| `-r <file>` | Read history from file |

## Common Patterns

### Combining Commands

```bash
# Find GameObjects and filter
hierarchy -r | grep Enemy

# List files and filter
ls -la | grep .cs

# Export hierarchy to file
hierarchy -r > hierarchy_dump.txt
```

### Working with Unity Objects

```bash
# Find all Rigidbody objects
hierarchy -c Rigidbody

# Get transform info
transform /Player -p

# Modify component property
property set /Player Rigidbody mass 10
```
