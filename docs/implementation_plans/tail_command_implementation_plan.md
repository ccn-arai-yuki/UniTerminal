# Tail Command Implementation Plan

## 概要

Linuxの`tail`コマンドを再現します。ファイルまたは標準入力の末尾を出力するコマンドです。

**前提**: この実装は [Ctrl+Cキャンセル機能](ctrl_c_cancel_implementation_plan.md) が実装されていることを前提とします。

## Linuxのtailコマンド仕様

```
tail [OPTION]... [FILE]...
```

### 主要オプション

| オプション | 説明 |
|-----------|------|
| `-n, --lines=K` | 最後のK行を出力（デフォルト10） |
| `-c, --bytes=K` | 最後のKバイトを出力 |
| `-f, --follow` | ファイルの追記を監視し続ける |
| `-q, --quiet` | 複数ファイル時にファイル名ヘッダーを出力しない |
| `-v, --verbose` | 常にファイル名ヘッダーを出力 |

### K値の指定方法

- `K` - 最後からK行/バイト（例: `-n 10`）
- `+K` - 先頭からK行/バイト目以降（例: `-n +5`）

### 動作仕様

1. **ファイル指定なし**: 標準入力から読み取り
2. **ファイル指定あり**: 指定ファイルから読み取り
3. **複数ファイル**: 各ファイルの前にヘッダー `==> filename <==` を出力
4. **存在しないファイル**: エラーメッセージを出力し、次のファイルを処理
5. **デフォルト行数**: 10行
6. **-fオプション**: ファイルへの追記を監視し、新しい内容をリアルタイム出力

## 実装方針

### 設計方針

- Linuxのtailコマンドの動作を忠実に再現
- 既存のCatCommand、HeadCommandとの一貫性を保つ
- パイプライン対応（標準入力からの読み取り）
- `-f`オプションは`FileSystemWatcher`で実装

### -fオプションの終了方法

Linuxでは`Ctrl+C`で終了します。UniTerminalでも同様に`Ctrl+C`でキャンセルします。
- `Ctrl+C` → `CancellationToken`がキャンセル → `OperationCanceledException` → 正常終了

### 対象外とする機能

以下のオプションはこのバージョンでは実装しません：
- `-F` - ファイル名で追跡（ログローテーション対応）
- `--pid` - プロセス監視
- `-s, --sleep-interval` - ポーリング間隔
- 複数ファイルの同時`-f`監視

## 実装内容

### 新規作成ファイル

#### TailCommand.cs

**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/BuiltInCommands/TailCommand.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// ファイルの末尾を出力するコマンド
    /// </summary>
    [Command("tail", "Output the last part of files")]
    public class TailCommand : ICommand
    {
        [Option("lines", "n", Description = "Output the last K lines (default: 10)")]
        public string Lines;

        [Option("bytes", "c", Description = "Output the last K bytes")]
        public string Bytes;

        [Option("follow", "f", Description = "Output appended data as the file grows (Ctrl+C to stop)")]
        public bool Follow;

        [Option("quiet", "q", Description = "Never output headers giving file names")]
        public bool Quiet;

        [Option("verbose", "v", Description = "Always output headers giving file names")]
        public bool Verbose;

        public string CommandName => "tail";
        public string Description => "Output the last part of files";

        private const int DefaultLines = 10;

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            // -n と -c は排他
            if (!string.IsNullOrEmpty(Lines) && !string.IsNullOrEmpty(Bytes))
            {
                await context.Stderr.WriteLineAsync("tail: cannot specify both --lines and --bytes", ct);
                return ExitCode.UsageError;
            }

            var files = context.PositionalArguments;

            // -f オプションはファイル指定が必須
            if (Follow && files.Count == 0)
            {
                await context.Stderr.WriteLineAsync("tail: --follow requires a file argument", ct);
                return ExitCode.UsageError;
            }

            // -f オプションは単一ファイルのみ対応
            if (Follow && files.Count > 1)
            {
                await context.Stderr.WriteLineAsync("tail: --follow with multiple files is not supported", ct);
                return ExitCode.UsageError;
            }

            // ファイル指定なし → 標準入力
            if (files.Count == 0)
                return await ProcessStdinAsync(context, ct);

            // 単一ファイル → ヘッダーなし（-v指定時を除く）
            // 複数ファイル → ヘッダーあり（-q指定時を除く）
            var showHeaders = files.Count > 1 ? !Quiet : Verbose;
            var exitCode = ExitCode.Success;
            var isFirst = true;

            foreach (var file in files)
            {
                var result = await ProcessFileAsync(context, file, showHeaders, isFirst, ct);
                if (result != ExitCode.Success)
                    exitCode = result;

                isFirst = false;
            }

            return exitCode;
        }

        private async Task<ExitCode> ProcessStdinAsync(CommandContext context, CancellationToken ct)
        {
            var allLines = await ReadAllLinesFromReaderAsync(context.Stdin, ct);

            if (!string.IsNullOrEmpty(Bytes))
                return await OutputByBytesAsync(context, allLines, ct);

            return await OutputByLinesAsync(context, allLines, ct);
        }

        private async Task<ExitCode> ProcessFileAsync(
            CommandContext context,
            string filePath,
            bool showHeader,
            bool isFirst,
            CancellationToken ct)
        {
            var resolvedPath = PathUtility.ResolvePath(filePath, context.WorkingDirectory);

            if (!File.Exists(resolvedPath))
            {
                await context.Stderr.WriteLineAsync($"tail: cannot open '{filePath}' for reading: No such file or directory", ct);
                return ExitCode.RuntimeError;
            }

            if (showHeader)
            {
                if (!isFirst)
                    await context.Stdout.WriteLineAsync("", ct);

                await context.Stdout.WriteLineAsync($"==> {filePath} <==", ct);
            }

            if (!string.IsNullOrEmpty(Bytes))
                return await OutputFileBytesAsync(context, resolvedPath, ct);

            var result = await OutputFileLinesAsync(context, resolvedPath, ct);
            if (result != ExitCode.Success)
                return result;

            // -f オプション: ファイル監視
            if (Follow)
                return await FollowFileAsync(context, resolvedPath, ct);

            return ExitCode.Success;
        }

        private async Task<ExitCode> OutputFileLinesAsync(
            CommandContext context,
            string filePath,
            CancellationToken ct)
        {
            var (count, fromStart) = ParseCount(Lines, DefaultLines);
            var lines = await File.ReadAllLinesAsync(filePath, ct);

            var outputLines = fromStart
                ? lines.Skip(count - 1)
                : lines.TakeLast(count);

            foreach (var line in outputLines)
            {
                await context.Stdout.WriteLineAsync(line, ct);
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> OutputFileBytesAsync(
            CommandContext context,
            string filePath,
            CancellationToken ct)
        {
            var (count, fromStart) = ParseCount(Bytes, 0);
            var content = await File.ReadAllBytesAsync(filePath, ct);

            byte[] output;
            if (fromStart)
            {
                var startIndex = Math.Min(count - 1, content.Length);
                output = content.Skip(startIndex).ToArray();
            }
            else
            {
                var startIndex = Math.Max(0, content.Length - count);
                output = content.Skip(startIndex).ToArray();
            }

            var text = Encoding.UTF8.GetString(output);
            await context.Stdout.WriteAsync(text, ct);

            return ExitCode.Success;
        }

        private async Task<ExitCode> OutputByLinesAsync(
            CommandContext context,
            List<string> allLines,
            CancellationToken ct)
        {
            var (count, fromStart) = ParseCount(Lines, DefaultLines);

            var outputLines = fromStart
                ? allLines.Skip(count - 1)
                : allLines.TakeLast(count);

            foreach (var line in outputLines)
            {
                await context.Stdout.WriteLineAsync(line, ct);
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> OutputByBytesAsync(
            CommandContext context,
            List<string> allLines,
            CancellationToken ct)
        {
            var (count, fromStart) = ParseCount(Bytes, 0);
            var fullText = string.Join("\n", allLines);
            var bytes = Encoding.UTF8.GetBytes(fullText);

            byte[] output;
            if (fromStart)
            {
                var startIndex = Math.Min(count - 1, bytes.Length);
                output = bytes.Skip(startIndex).ToArray();
            }
            else
            {
                var startIndex = Math.Max(0, bytes.Length - count);
                output = bytes.Skip(startIndex).ToArray();
            }

            var text = Encoding.UTF8.GetString(output);
            await context.Stdout.WriteAsync(text, ct);

            return ExitCode.Success;
        }

        /// <summary>
        /// FileSystemWatcherを使用してファイルの追記を監視
        /// Ctrl+CでCancellationTokenがキャンセルされると終了
        /// </summary>
        private async Task<ExitCode> FollowFileAsync(
            CommandContext context,
            string filePath,
            CancellationToken ct)
        {
            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            var lastPosition = new FileInfo(filePath).Length;

            using var watcher = new FileSystemWatcher(directory, fileName);
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;

            watcher.Changed += async (sender, e) =>
            {
                try
                {
                    lastPosition = await OutputNewContentAsync(context, filePath, lastPosition, ct);
                }
                catch (OperationCanceledException)
                {
                    // キャンセル時は何もしない
                }
                catch (Exception ex)
                {
                    try
                    {
                        await context.Stderr.WriteLineAsync($"tail: error reading file: {ex.Message}", ct);
                    }
                    catch (OperationCanceledException)
                    {
                        // キャンセル時は何もしない
                    }
                }
            };

            watcher.EnableRaisingEvents = true;

            // Ctrl+C（CancellationToken）でキャンセルされるまで待機
            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                // Ctrl+Cによる正常終了
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// ファイルの新しい内容を出力し、新しい位置を返す
        /// </summary>
        private async Task<long> OutputNewContentAsync(
            CommandContext context,
            string filePath,
            long lastPosition,
            CancellationToken ct)
        {
            var fileInfo = new FileInfo(filePath);
            var currentLength = fileInfo.Length;

            // ファイルが切り詰められた場合は先頭から
            if (currentLength < lastPosition)
                lastPosition = 0;

            if (currentLength <= lastPosition)
                return lastPosition;

            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            stream.Seek(lastPosition, SeekOrigin.Begin);

            var bytesToRead = (int)(currentLength - lastPosition);
            var buffer = new byte[bytesToRead];
            var bytesRead = await stream.ReadAsync(buffer, 0, bytesToRead, ct);

            if (bytesRead > 0)
            {
                var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                await context.Stdout.WriteAsync(text, ct);
            }

            return currentLength;
        }

        private static async Task<List<string>> ReadAllLinesFromReaderAsync(
            IAsyncTextReader reader,
            CancellationToken ct)
        {
            var lines = new List<string>();
            while (true)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null)
                    break;

                lines.Add(line);
            }
            return lines;
        }

        /// <summary>
        /// カウント値をパース（+Kは先頭から、Kは末尾から）
        /// </summary>
        private static (int count, bool fromStart) ParseCount(string value, int defaultCount)
        {
            if (string.IsNullOrEmpty(value))
                return (defaultCount, false);

            if (value.StartsWith("+"))
            {
                if (int.TryParse(value.Substring(1), out var n))
                    return (n, true);
            }
            else
            {
                if (int.TryParse(value, out var n))
                    return (Math.Abs(n), false);
            }

            return (defaultCount, false);
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // ファイルパス補完はシステムに委譲
            return Enumerable.Empty<string>();
        }
    }
}
```

### 修正ファイル

#### Terminal.cs

**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Terminal.cs`

**変更内容**: `RegisterBuiltInCommands()`に`TailCommand`を追加

```csharp
private void RegisterBuiltInCommands()
{
    // 既存のコマンド...
    Register<TailCommand>();
}
```

## 技術詳細

### FileSystemWatcherによるファイル監視

```csharp
using var watcher = new FileSystemWatcher(directory, fileName);
watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
watcher.Changed += async (sender, e) => { /* 新しい内容を出力 */ };
watcher.EnableRaisingEvents = true;
```

**利点**:
- イベント駆動で効率的（ポーリング不要）
- .NET標準APIで安定

**考慮点**:
- `FileShare.ReadWrite`でファイルを開き、他プロセスとのロック競合を回避
- ファイルが切り詰められた場合（ログローテーション）は先頭から再読み込み

### Ctrl+Cによる終了フロー

```
ユーザーがCtrl+Cを押す
    ↓
UniTerminal.Update()でIsPressedCtrlC()を検出
    ↓
commandCancellationTokenSource.Cancel()
    ↓
Task.Delay(Timeout.Infinite, ct)がOperationCanceledExceptionをスロー
    ↓
FollowFileAsync()が正常終了（ExitCode.Success）
    ↓
UniTerminal.OnInputCommand()で"^C"を表示
```

### ParseCount メソッド

Linuxのtailでは、`-n +5`のように`+`を付けると「5行目から最後まで」という意味になります：

```
-n 10   → 最後の10行
-n +10  → 10行目から最後まで
```

### 複数ファイル時のヘッダー

```
$ tail file1.txt file2.txt
==> file1.txt <==
(file1の内容)

==> file2.txt <==
(file2の内容)
```

### エラー処理

存在しないファイルがあっても、他のファイルは処理を継続：

```
$ tail missing.txt exists.txt
tail: cannot open 'missing.txt' for reading: No such file or directory
==> exists.txt <==
(exists.txtの内容)
```

終了コードは、1つでもエラーがあれば`RuntimeError`を返します。

## 実装順序

**前提**: [Ctrl+Cキャンセル機能](ctrl_c_cancel_implementation_plan.md)を先に実装

1. `TailCommand.cs`を新規作成
2. `Terminal.cs`でコマンドを登録
3. 動作確認

## 検証方法

### 基本機能テスト

```bash
# デフォルト（最後の10行）
tail test.txt

# 行数指定
tail -n 5 test.txt

# 先頭からN行目以降
tail -n +5 test.txt

# バイト数指定
tail -c 100 test.txt

# 複数ファイル
tail file1.txt file2.txt

# 複数ファイル（ヘッダーなし）
tail -q file1.txt file2.txt

# 単一ファイル（ヘッダーあり）
tail -v test.txt

# パイプライン
cat test.txt | tail -n 5
```

### -fオプションテスト

```bash
# ファイル監視開始
tail -f test.log

# 別のターミナル/スクリプトでファイルに追記
echo "new line" >> test.log

# → "new line" がリアルタイムで表示される

# Ctrl+Cで終了
# → "^C" が表示されてコマンドが終了
```

### エッジケーステスト

1. **存在しないファイル**: エラーメッセージ出力
2. **空ファイル**: 何も出力せず正常終了
3. **行数がファイルより多い**: 全行出力
4. **-n と -c の同時指定**: エラー
5. **負の数**: 絶対値として扱う（`-n -5` → `-n 5`）
6. **-f でファイル指定なし**: エラー
7. **-f で複数ファイル**: エラー
8. **-f 中にファイルが切り詰められる**: 先頭から再読み込み
9. **-f 中にCtrl+C**: 正常終了、"^C"表示

## ファイル一覧

### 新規作成
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/BuiltInCommands/TailCommand.cs`

### 修正
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Terminal.cs`

## 依存関係

- [Ctrl+Cキャンセル機能](ctrl_c_cancel_implementation_plan.md) - 必須

## CLAUDE.md準拠

- **ネスト制限**: 最大3レベル（namespace除く）
- **switch制限**: 使用せず、条件分岐はif文で処理
- **可読性優先**: メソッドを分割し、意図が明確な命名
- **制御文スタイル**: 2行形式を使用
