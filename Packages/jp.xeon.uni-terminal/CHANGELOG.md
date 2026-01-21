# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2025-01-15

### Added

- **Core Framework**
  - String-based CLI execution framework with Linux-like behavior
  - Pipeline support with pipe (`|`) and redirect (`>`, `>>`, `<`) operators
  - Async/await command execution
  - Tab completion for commands and paths

- **File Operation Commands**
  - `pwd` - Print working directory
  - `cd` - Change directory
  - `ls` - List directory contents
  - `cat` - Display file contents
  - `find` - Search for files
  - `less` - View files page by page
  - `diff` - Compare files

- **Text Processing Commands**
  - `echo` - Output text
  - `grep` - Pattern matching search

- **Utility Commands**
  - `help` - Display help information
  - `history` - Command history management

- **Unity-Specific Commands**
  - `hierarchy` - Display scene hierarchy with filtering options
  - `go` - GameObject operations (create, delete, find, clone, etc.)
  - `transform` - Transform manipulation (position, rotation, scale)
  - `component` - Component management (add, remove, enable/disable)
  - `property` - Property value operations via reflection

- **UniTask Support** (Optional)
  - `IUniTaskCommand` interface for UniTask-based async commands
  - `UniTaskCommandContext` for UniTask command execution
  - Automatic detection when UniTask is installed

- **FlyweightScrollView**
  - Virtual scrolling for efficient large data display
  - Vertical and horizontal scroll support
  - `CircularBuffer` for fixed-size log buffering
  - `ObservableCollection` integration

### Technical Details

- Minimum Unity version: 6000.0
- Supports custom command registration via `ICommand` interface
- Assembly definition files for proper code separation
- Comprehensive unit tests and PlayMode tests

[0.1.0]: https://github.com/AraiYuhki/UniTerminal/releases/tag/v0.1.0
