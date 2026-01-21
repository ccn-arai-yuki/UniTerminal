# Utilities

General utility commands for help and history management.

## help

Display help information for commands.

### Synopsis

```bash
help [command]
```

### Description

Without arguments, displays a list of all available commands with brief descriptions. When a command name is provided, displays detailed help for that specific command including usage, options, and examples.

### Arguments

| Argument | Description |
|----------|-------------|
| `command` | Optional. Name of the command to get help for. |

### Examples

```bash
# List all available commands
help
```

**Output:**
```
Available commands:
  cat        Concatenate and display file contents
  cd         Change working directory
  component  Manage GameObject components
  diff       Compare files line by line
  echo       Echo arguments to stdout
  find       Search for files in a directory hierarchy
  go         GameObject operations
  grep       Filter lines matching a pattern
  help       Display help for commands
  hierarchy  Display scene hierarchy
  history    Display or manage command history
  less       View file contents page by page
  ls         List directory contents
  property   Get or set component property values
  pwd        Print current working directory
  transform  Manipulate GameObject transforms

Use 'help <command>' for detailed information.
```

```bash
# Get help for specific command
help ls
```

**Output:**
```
ls - List directory contents

Usage: ls [options] [path...]

Options:
  -a, --all             Do not ignore entries starting with .
  -l, --long            Use a long listing format
  -h, --human-readable  Print sizes in human readable format
  -r, --reverse         Reverse order while sorting
  -R, --recursive       List subdirectories recursively
  -S, --sort            Sort by: name, size, time
```

```bash
# Get help for Unity commands
help hierarchy
help component
help property
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | Unknown command specified |
| 2 | Registry not configured |

---

## history

Display or manage command history.

### Synopsis

```bash
history [-c] [-d position] [-n count] [-r]
```

### Description

Displays the command history list with line numbers. Can also be used to clear history or delete specific entries.

### Options

| Option | Long | Description |
|--------|------|-------------|
| `-c` | `--clear` | Clear all history entries |
| `-d` | `--delete` | Delete the history entry at the specified position (1-based) |
| `-n` | `--number` | Display only the last N entries |
| `-r` | `--reverse` | Display history in reverse order (newest first) |

### Examples

```bash
# Display full history
history
```

**Output:**
```
    1  ls -la
    2  cd ~/Projects
    3  hierarchy -r
    4  go create Player
    5  component add /Player Rigidbody
    6  property set /Player Rigidbody mass 10
```

```bash
# Show last 5 commands
history -n 5
```

**Output:**
```
    2  cd ~/Projects
    3  hierarchy -r
    4  go create Player
    5  component add /Player Rigidbody
    6  property set /Player Rigidbody mass 10
```

```bash
# Show history in reverse order
history -r
```

**Output:**
```
    6  property set /Player Rigidbody mass 10
    5  component add /Player Rigidbody
    4  go create Player
    3  hierarchy -r
    2  cd ~/Projects
    1  ls -la
```

```bash
# Delete specific entry
history -d 3

# Clear all history
history -c
```

### History Format

Each history entry is displayed with:
- **Line number** (1-based, right-aligned in 5 characters)
- **Command** (the exact command as entered)

### Notes

- History is maintained per Terminal instance
- History clearing and deletion require the Terminal to be configured with history callbacks
- The current command being executed is typically not included in history until after execution

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 2 | History operation not supported or position out of range |

---

## Practical Examples

### Reviewing Recent Work

```bash
# See what commands you ran recently
history -n 10

# Find specific commands in history (combine with grep)
history | grep -p "component"
```

### Cleaning Up History

```bash
# Remove a mistaken command
history -d 5

# Start fresh
history -c
```

### Getting Command Help

```bash
# Explore available commands
help

# Learn about a specific command
help transform

# Check Unity-specific commands
help hierarchy
help go
help component
help property
```

### Quick Reference

```bash
# Common help queries
help ls          # File listing options
help find        # File search options
help grep        # Pattern matching syntax
help diff        # File comparison options
help hierarchy   # Scene hierarchy display
help go          # GameObject operations
help transform   # Transform manipulation
help component   # Component management
help property    # Property access via reflection
```
