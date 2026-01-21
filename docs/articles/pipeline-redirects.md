# Pipeline & Redirects

UniTerminal supports Linux-like pipelines and redirects for powerful command chaining.

## Pipelines

Connect commands using the pipe operator (`|`). The output of one command becomes the input of the next.

### Basic Syntax

```bash
command1 | command2 | command3
```

### Examples

```bash
# Find objects and filter
hierarchy -r | grep -p "Player"

# Chain multiple filters
hierarchy -r -l | grep -p "Enemy" | grep -p "Active: True"

# Count results
hierarchy -r | grep -p "Collider" | count

# Process file content
cat config.json | grep -p "setting"
```

### How It Works

```
┌─────────┐    stdout    ┌─────────┐    stdout    ┌─────────┐
│ Command1 │────────────▶│ Command2 │────────────▶│ Command3 │
└─────────┘              └─────────┘              └─────────┘
                              ▲                        │
                           stdin                    stdout
                                                       ▼
                                                   [output]
```

1. Command1 writes to stdout
2. Command2 reads from stdin (Command1's output)
3. Command3 reads from stdin (Command2's output)
4. Final output goes to the terminal

## Redirects

### Output Redirect (`>`)

Write command output to a file, **overwriting** existing content:

```bash
# Save hierarchy to file
hierarchy -r > hierarchy.txt

# Save filtered results
hierarchy -r | grep -p "Player" > players.txt

# Export configuration
property list /Settings GameConfig > config_dump.txt
```

### Append Redirect (`>>`)

Append command output to a file, **preserving** existing content:

```bash
# Add to log file
echo "Session started" >> session.log

# Append hierarchy snapshot
hierarchy -r >> snapshots.txt

# Build up a report
echo "=== Players ===" >> report.txt
hierarchy -n "Player*" >> report.txt
echo "=== Enemies ===" >> report.txt
hierarchy -n "Enemy*" >> report.txt
```

### Input Redirect (`<`)

Read file content as command input:

```bash
# Search in file
grep -p "error" < log.txt

# Process file content
count < data.txt

# Count words in file
count -w < document.txt
```

## Combined Usage

### Pipeline + Output Redirect

```bash
# Filter and save
hierarchy -r | grep -p "UI" > ui_objects.txt

# Process and save
cat data.txt | grep -p "important" | count > result.txt
```

### Input Redirect + Pipeline

```bash
# Read file and filter
grep -p "error" < log.txt | count

# Process file through pipeline
count -w < document.txt
```

### Complex Chains

```bash
# Full analysis pipeline
hierarchy -r -l | grep -p "Enemy" | grep -v "Disabled" > active_enemies.txt

# Multi-step processing
cat input.txt | grep -p "data" | count > analysis.txt
```

## Practical Examples

### Scene Analysis

```bash
# Export full hierarchy
hierarchy -r -l > scene_dump.txt

# Count objects by type
echo "Rigidbody count:" > physics_report.txt
hierarchy -c Rigidbody | count >> physics_report.txt
echo "Collider count:" >> physics_report.txt
hierarchy -c Collider | count >> physics_report.txt
```

### Debug Logging

```bash
# Create debug snapshot
echo "=== Debug Snapshot $(date) ===" >> debug.log
hierarchy -r -l >> debug.log
echo "" >> debug.log
```

### Configuration Export

```bash
# Export all settings
property list /GameManager Settings > settings.txt
property list /AudioManager AudioSettings >> settings.txt
property list /GraphicsManager GraphicsSettings >> settings.txt
```

### Batch Processing

```bash
# Find and document all UI elements
hierarchy -c "UnityEngine.UI.Image" > ui_images.txt
hierarchy -c "UnityEngine.UI.Text" > ui_texts.txt
hierarchy -c "UnityEngine.UI.Button" > ui_buttons.txt
```

## Error Handling

### Stderr vs Stdout

- **Stdout**: Normal output (goes through pipeline)
- **Stderr**: Error messages (displayed directly)

```bash
# Errors don't go through pipe
nonexistent_command | grep -p "test"
# Error: Command 'nonexistent_command' not found
# (grep receives nothing)
```

### Exit Codes in Pipelines

The exit code of a pipeline is the exit code of the last command:

```bash
# If grep finds nothing, exit code is still Success
# (grep outputs nothing, but doesn't error)
hierarchy | grep -p "NonExistent"
```

## Tips

1. **Use pipelines for filtering** - Avoid loading everything into memory
2. **Redirect large outputs** - Save to file instead of displaying
3. **Chain incrementally** - Build up complex pipelines step by step
4. **Check intermediate results** - Remove final redirect to debug

### Debugging Pipelines

```bash
# Full pipeline
hierarchy -r | grep -p "Player" | grep -p "Active" > result.txt

# Debug step 1
hierarchy -r

# Debug step 2
hierarchy -r | grep -p "Player"

# Debug step 3
hierarchy -r | grep -p "Player" | grep -p "Active"

# Final with redirect
hierarchy -r | grep -p "Player" | grep -p "Active" > result.txt
```
