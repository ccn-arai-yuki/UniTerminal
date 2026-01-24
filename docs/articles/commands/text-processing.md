# Text Processing

Commands for processing and filtering text output.

## echo

Output text to standard output.

### Synopsis

```bash
echo [-n] [string...]
```

### Description

Outputs the specified strings to standard output, separated by spaces, followed by a newline (unless `-n` is specified).

### Options

| Option | Long | Description |
|--------|------|-------------|
| `-n` | `--newline` | Do not output the trailing newline |

### Arguments

| Argument | Description |
|----------|-------------|
| `string` | One or more strings to output. Multiple strings are joined with spaces. |

### Examples

```bash
# Simple output
echo Hello, World!
# Output: Hello, World!

# Multiple arguments
echo Hello World from UniTerminal
# Output: Hello World from UniTerminal

# Output without trailing newline
echo -n "No newline here"

# Create files with content
echo "Configuration data" > config.txt

# Append to file
echo "Additional line" >> log.txt

# Use in pipeline
echo "Hello" | grep -p "ell"
# Output: Hello
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Always succeeds |

---

## grep

Search for lines matching a pattern.

### Synopsis

```bash
grep -p <pattern> [-i] [-v] [-c]
```

### Description

Filters input lines, outputting only those that match (or don't match with `-v`) the specified regular expression pattern. Reads from standard input.

### Options

| Option | Long | Description |
|--------|------|-------------|
| `-p` | `--pattern` | **Required.** Regular expression pattern to search for |
| `-i` | `--ignorecase` | Ignore case distinctions in pattern matching |
| `-v` | `--invert` | Invert the match; select non-matching lines |
| `-c` | `--count` | Only print a count of matching lines |

### Pattern Syntax

grep uses .NET regular expressions. Common patterns:

| Pattern | Description |
|---------|-------------|
| `.` | Matches any single character |
| `*` | Matches zero or more of the preceding element |
| `+` | Matches one or more of the preceding element |
| `?` | Matches zero or one of the preceding element |
| `^` | Matches start of line |
| `$` | Matches end of line |
| `[abc]` | Matches any character in the set |
| `[^abc]` | Matches any character not in the set |
| `\d` | Matches any digit |
| `\w` | Matches any word character |
| `\s` | Matches any whitespace |
| `(a\|b)` | Matches a or b |

### Examples

```bash
# Basic pattern search
cat file.txt | grep -p "error"

# Case-insensitive search
cat log.txt | grep -p "warning" -i

# Invert match (exclude lines)
ls -la | grep -p ".meta" -v

# Count matches only
hierarchy -r | grep -p "Enemy" -c
# Output: 15

# Regular expression patterns
cat code.cs | grep -p "public class \w+"
cat log.txt | grep -p "^\[ERROR\]"
cat data.txt | grep -p "id: \d+"

# Find lines starting with specific text
cat file.txt | grep -p "^TODO"

# Find lines ending with specific text
cat file.txt | grep -p "\.cs$"

# Use OR pattern
cat log.txt | grep -p "(error|warning|critical)" -i
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | One or more lines matched |
| 2 | No lines matched |
| 1 | Invalid pattern or other error |

---

## Pipeline Examples

### Working with Text Files

```bash
# Search for errors in log file
cat application.log | grep -p "Exception"

# Find TODO comments in code
cat script.cs | grep -p "// TODO"

# Extract lines with numbers
cat data.txt | grep -p "\d+"

# Find empty lines
cat file.txt | grep -p "^$"

# Find non-empty lines
cat file.txt | grep -p "^$" -v
```

### Filtering Unity Hierarchy

```bash
# Find all objects with "Player" in name
hierarchy -r | grep -p "Player"

# Find objects by pattern
hierarchy -r | grep -p "Enemy_\d+"

# Find objects NOT tagged as "Untagged"
hierarchy -r -l | grep -p "Untagged" -v

# Count enemies in scene
hierarchy -r | grep -p "Enemy" -c
```

### Filtering File Lists

```bash
# Find C# files
ls -R | grep -p "\.cs$"

# Exclude meta files
ls -la | grep -p "\.meta$" -v

# Find files with specific prefix
ls | grep -p "^Player"

# Find files by size pattern
ls -lh | grep -p "MB"
```

### Chaining Multiple Filters

```bash
# Find active players
hierarchy -r -l | grep -p "Player" | grep -p "Active"

# Find error messages excluding warnings
cat log.txt | grep -p "error" -i | grep -p "warning" -v

# Complex filtering
ls -la | grep -p "\.cs$" | grep -p "Test" -v
```

### Combining with Other Commands

```bash
# Find files and search content
find -n "*.cs" | cat | grep -p "class"

# Count specific items
hierarchy -c Rigidbody | grep -p "/" -c

# Search and save results
hierarchy -r | grep -p "Enemy" > enemies.txt

# Multi-stage filtering
cat config.json | grep -p "\"enabled\"" | grep -p "true" -c
```

### Practical Use Cases

```bash
# Find all GameObjects with missing scripts
component list /Root -v | grep -p "Missing"

# Find objects with specific components
hierarchy -r -l | grep -p "Rigidbody"

# Debug output filtering
property list /Player Rigidbody | grep -p "mass\|drag\|velocity"

# Search for patterns in hierarchy paths
hierarchy -r | grep -p "UI/.*Button"
```
