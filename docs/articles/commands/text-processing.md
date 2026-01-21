# Text Processing

Commands for processing and filtering text output.

## echo

Output text to stdout.

```bash
# Simple output
echo Hello, World!

# Output without newline
echo -n "No newline"

# With variables (from pipeline)
hierarchy | echo "Objects found:"
```

**Options:**

| Option | Description |
|--------|-------------|
| `-n` | Do not output trailing newline |

## grep

Search for patterns in input.

```bash
# Search for pattern
cat file.txt | grep --pattern="error"

# Case-insensitive search
hierarchy -r | grep -p "player" -i

# Invert match (show non-matching lines)
ls -la | grep -p ".meta" -v

# Count matches
hierarchy -r | grep -p "Enemy" -c
```

**Options:**

| Option | Description |
|--------|-------------|
| `-p`, `--pattern` | Pattern to search for |
| `-i` | Case-insensitive matching |
| `-v` | Invert match (show lines that don't match) |
| `-c` | Count matching lines |

## Pipeline Examples

### Filter Unity Hierarchy

```bash
# Find all objects with "Player" in name
hierarchy -r | grep -p "Player"

# Find objects NOT tagged as "Untagged"
hierarchy -r -l | grep -p "Untagged" -v

# Count enemies in scene
hierarchy -r | grep -p "Enemy" -c
```

### Filter File Lists

```bash
# Find C# files
ls -R | grep -p ".cs"

# Exclude meta files
ls -la | grep -p ".meta" -v

# Find specific patterns
cat log.txt | grep -p "Exception"
```

### Chain Multiple Filters

```bash
# Find active players
hierarchy -r -l | grep -p "Player" | grep -p "Active: True"

# Find large files (combine with ls -lh)
ls -lhS | grep -p "MB"
```
