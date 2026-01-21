# File Operations

Commands for navigating and manipulating files and directories.

## pwd

Print the current working directory.

### Synopsis

```bash
pwd [-L|-P]
```

### Description

Displays the absolute path of the current working directory.

### Options

| Option | Long | Description |
|--------|------|-------------|
| `-L` | `--logical` | Print logical path (default). Uses the path as set, including any symbolic link names. |
| `-P` | `--physical` | Print physical path with symbolic links resolved. |

### Examples

```bash
# Print current directory
pwd
# Output: /Users/player/Projects/MyGame

# Print physical path (resolve symlinks)
pwd -P
# Output: /Users/player/Projects/MyGame
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 2 | Current directory does not exist or permission denied |

---

## cd

Change the working directory.

### Synopsis

```bash
cd [-L|-P] [directory]
cd -
```

### Description

Changes the current working directory to the specified path. Without arguments, changes to the home directory.

### Options

| Option | Long | Description |
|--------|------|-------------|
| `-L` | `--logical` | Follow symbolic links (default) |
| `-P` | `--physical` | Use physical directory structure, resolving symbolic links |

### Arguments

| Argument | Description |
|----------|-------------|
| `directory` | Target directory path. Supports `~` for home directory. |
| `-` | Change to previous directory (equivalent to `cd $OLDPWD`) |

### Examples

```bash
# Change to home directory
cd

# Change to specific directory
cd /Users/player/Projects

# Change to parent directory
cd ..

# Change using home shorthand
cd ~/Documents

# Change to previous directory
cd -
# Output: /previous/path

# Use physical path
cd -P /symlinked/path
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | Too many arguments |
| 2 | Directory not found, not a directory, or permission denied |

---

## ls

List directory contents.

### Synopsis

```bash
ls [-a] [-l] [-h] [-r] [-R] [-S <sort>] [path...]
```

### Description

Lists information about files and directories. By default, lists the current directory contents.

### Options

| Option | Long | Description |
|--------|------|-------------|
| `-a` | `--all` | Do not ignore entries starting with `.` (hidden files) |
| `-l` | `--long` | Use long listing format with details |
| `-h` | `--human-readable` | Print sizes in human readable format (e.g., 1K, 234M) |
| `-r` | `--reverse` | Reverse order while sorting |
| `-R` | `--recursive` | List subdirectories recursively |
| `-S` | `--sort` | Sort by: `name` (default), `size`, `time` |

### Arguments

| Argument | Description |
|----------|-------------|
| `path` | File or directory to list. Multiple paths can be specified. |

### Output Format

**Normal format:**
```
file1.txt  file2.txt  folder/
```

**Long format (`-l`):**
```
-rw-rw-rw-  1      1234  2025-01-15 10:30  file.txt
drwxrwxrwx  2         0  2025-01-15 09:00  folder/
```

Fields: permissions, link count, size, date, name

### Examples

```bash
# List current directory
ls

# List with details
ls -l

# List all files including hidden
ls -a

# List with human-readable sizes
ls -lh

# Sort by size (largest first)
ls -l -S size

# Sort by modification time
ls -l -S time

# Reverse sort order
ls -lr

# List recursively
ls -R

# List specific directory
ls /path/to/directory

# List multiple paths
ls file1.txt folder/
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 2 | File or directory not found, or permission denied |

---

## cat

Concatenate and display file contents.

### Synopsis

```bash
cat [file...]
```

### Description

Reads files sequentially and writes them to standard output. If no file is specified, reads from standard input.

### Arguments

| Argument | Description |
|----------|-------------|
| `file` | One or more files to display. If omitted, reads from stdin. |

### Examples

```bash
# Display file contents
cat file.txt

# Display multiple files
cat file1.txt file2.txt

# Use in pipeline (pass through)
echo "Hello" | cat

# Redirect to file
cat source.txt > destination.txt

# Append files
cat header.txt body.txt footer.txt > complete.txt
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 2 | File not found or read error |

---

## find

Search for files in a directory hierarchy.

### Synopsis

```bash
find [path...] [-n pattern] [-i pattern] [-t type] [-d depth] [--mindepth N]
```

### Description

Searches for files matching specified criteria within directory trees.

### Options

| Option | Long | Description |
|--------|------|-------------|
| `-n` | `--name` | File name pattern (supports `*` and `?` wildcards) |
| `-i` | `--iname` | Case-insensitive file name pattern |
| `-t` | `--type` | File type: `f` (file), `d` (directory) |
| `-d` | `--maxdepth` | Maximum search depth (-1 = unlimited, default) |
| | `--mindepth` | Minimum search depth (default: 0) |

### Arguments

| Argument | Description |
|----------|-------------|
| `path` | Starting directory for search. Defaults to current directory. |

### Wildcards

| Pattern | Description |
|---------|-------------|
| `*` | Matches any sequence of characters |
| `?` | Matches any single character |

### Examples

```bash
# Find all files in current directory tree
find

# Find by name pattern
find -n "*.cs"
find -n "Player*"

# Case-insensitive search
find -i "readme*"

# Find only files
find -t f

# Find only directories
find -t d

# Limit search depth
find -d 2

# Combine options
find /path/to/search -n "*.txt" -t f -d 3

# Find in specific directory
find Assets/Scripts -n "*.cs"
```

### Output

Outputs matching paths relative to the search directory:
```
./file.txt
./subfolder/another.txt
./subfolder/deep/file.txt
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success (even if no matches found) |
| 2 | Starting path not found |

---

## less

View file contents page by page.

### Synopsis

```bash
less [-n lines] [-f line] [-N] [-S] [file]
```

### Description

Displays file contents with pagination support. In UniTerminal's non-interactive environment, outputs a specified number of lines.

### Options

| Option | Long | Description |
|--------|------|-------------|
| `-n` | `--lines` | Number of lines to display (0 = all, default) |
| `-f` | `--from-line` | Start from specified line number (1-based, default: 1) |
| `-N` | `--line-numbers` | Show line numbers |
| `-S` | `--chop-long-lines` | Truncate long lines at 80 characters |

### Arguments

| Argument | Description |
|----------|-------------|
| `file` | File to view. Use `-` for stdin. If omitted, reads from stdin. |

### Examples

```bash
# View entire file
less file.txt

# View first 20 lines
less -n 20 file.txt

# View lines 50-70
less -f 50 -n 20 file.txt

# Show with line numbers
less -N file.txt

# Truncate long lines
less -S logfile.txt

# View from stdin
cat file.txt | less -n 10

# Combine options
less -N -n 50 -f 100 largefile.txt
```

### Output with `-n` option

```
File: example.txt (lines 1-20 of 150)
----------------------------------------
Line 1 content
Line 2 content
...
----------------------------------------
(130 more lines)
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 2 | File not found, is a directory, or permission denied |

---

## diff

Compare files line by line.

### Synopsis

```bash
diff [-u N] [-i] [-b] [-w] [-q] <file1> <file2>
```

### Description

Compares two files and displays the differences. Uses the LCS (Longest Common Subsequence) algorithm.

### Options

| Option | Long | Description |
|--------|------|-------------|
| `-u` | `--unified` | Output in unified format with N lines of context |
| `-i` | `--ignore-case` | Ignore case differences in file contents |
| `-b` | `--ignore-space` | Ignore changes in the amount of whitespace |
| `-w` | `--ignore-all-space` | Ignore all whitespace |
| `-q` | `--brief` | Report only whether files differ |

### Arguments

| Argument | Description |
|----------|-------------|
| `file1` | First file to compare. Use `-` for stdin. |
| `file2` | Second file to compare. Use `-` for stdin. |

### Output Formats

**Normal format (default):**
```
2c2
< old line
---
> new line
5a6
> added line
8d7
< deleted line
```

**Unified format (`-u`):**
```
--- file1.txt
+++ file2.txt
@@ -1,5 +1,6 @@
 context line
-removed line
+added line
 context line
```

### Examples

```bash
# Compare two files
diff file1.txt file2.txt

# Unified format with 3 lines context
diff -u 3 file1.txt file2.txt

# Ignore case
diff -i file1.txt file2.txt

# Ignore whitespace changes
diff -w file1.txt file2.txt

# Brief output
diff -q file1.txt file2.txt
# Output: Files file1.txt and file2.txt differ

# Compare with stdin
cat modified.txt | diff original.txt -
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Files are identical |
| 1 | Files differ |
| 2 | Error (file not found, etc.) |
