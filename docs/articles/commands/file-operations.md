# File Operations

Commands for navigating and working with files and directories.

## pwd

Print the current working directory.

```bash
pwd
```

**Options:**

| Option | Description |
|--------|-------------|
| `-L` | Print logical path (with symlinks) |
| `-P` | Print physical path (resolve symlinks) |

## cd

Change the current working directory.

```bash
# Go to specific directory
cd /path/to/directory

# Go to home directory
cd ~

# Go to parent directory
cd ..

# Go to previous directory
cd -
```

**Options:**

| Option | Description |
|--------|-------------|
| `-L` | Follow symbolic links |
| `-P` | Use physical directory structure |

## ls

List directory contents.

```bash
# List current directory
ls

# List specific directory
ls /path/to/directory

# List with details
ls -l

# List all files (including hidden)
ls -a

# List with human-readable sizes
ls -lh

# List recursively
ls -R

# Sort by size
ls -S

# Reverse order
ls -r
```

**Options:**

| Option | Description |
|--------|-------------|
| `-a` | Show hidden files (starting with `.`) |
| `-l` | Long format with details |
| `-h` | Human-readable file sizes |
| `-r` | Reverse sort order |
| `-R` | List subdirectories recursively |
| `-S` | Sort by file size |

## cat

Display file contents.

```bash
# Display single file
cat file.txt

# Display multiple files
cat file1.txt file2.txt

# Use in pipeline
cat config.json | grep "setting"
```

## find

Search for files matching criteria.

```bash
# Find by name
find -n "*.cs"

# Find by name (case-insensitive)
find -n "readme*" -i

# Find in specific directory
find /Assets -n "*.prefab"

# Find by type (file or directory)
find -t f -n "*.cs"    # Files only
find -t d -n "Scripts" # Directories only

# Limit depth
find -d 2 -n "*.cs"
```

**Options:**

| Option | Description |
|--------|-------------|
| `-n <pattern>` | Name pattern (supports wildcards) |
| `-i` | Case-insensitive matching |
| `-t <type>` | Type: `f` (file) or `d` (directory) |
| `-d <depth>` | Maximum search depth |

## less

View files page by page (useful for large files).

```bash
# View file
less file.txt

# Show line numbers
less -n file.txt

# Show from specific line
less -f 100 file.txt

# Don't wrap long lines
less -S file.txt
```

**Options:**

| Option | Description |
|--------|-------------|
| `-n` | Show line numbers |
| `-f <line>` | Start from specific line |
| `-N <count>` | Show N lines per page |
| `-S` | Don't wrap long lines |

## diff

Compare two files.

```bash
# Basic comparison
diff file1.txt file2.txt

# Unified format
diff -u file1.txt file2.txt

# Ignore case
diff -i file1.txt file2.txt

# Ignore whitespace
diff -w file1.txt file2.txt

# Brief output (only show if different)
diff -q file1.txt file2.txt
```

**Options:**

| Option | Description |
|--------|-------------|
| `-u` | Unified format output |
| `-i` | Ignore case differences |
| `-b` | Ignore changes in whitespace amount |
| `-w` | Ignore all whitespace |
| `-q` | Report only if files differ |
