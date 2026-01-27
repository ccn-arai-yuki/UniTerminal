# Head Command Implementation Plan

## 概要

Linuxの`head`コマンドを再現します。ファイルまたは標準入力の先頭を出力するコマンドです。

## Linuxのheadコマンド仕様

```
head [OPTION]... [FILE]...
```

### 主要オプション

| オプション | 説明 |
|-----------|------|
| `-n, --lines=K` | 先頭のK行を出力（デフォルト10） |
| `-c, --bytes=K` | 先頭のKバイトを出力 |
| `-q, --quiet` | 複数ファイル時にファイル名ヘッダーを出力しない |
| `-v, --verbose` | 常にファイル名ヘッダーを出力 |

### K値の指定方法

- `K` - 先頭からK行/バイト（例: `-n 10`）
- `-K` - 最後のK行/バイトを除いた全て（例: `-n -5`）

### 動作仕様

1. **ファイル指定なし**: 標準入力から読み取り
2. **ファイル指定あり**: 指定ファイルから読み取り
3. **複数ファイル**: 各ファイルの前にヘッダー `==> filename <==` を出力
4. **存在しないファイル**: エラーメッセージを出力し、次のファイルを処理
5. **デフォルト行数**: 10行

## 実装方針

### 設計方針

- Linuxのheadコマンドの動作を忠実に再現
- 既存のCatCommand、tailコマンドとの一貫性を保つ
- パイプライン対応（標準入力からの読み取り）

## 実装内容

### 新規作成ファイル

#### HeadCommand.cs

**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/BuiltInCommands/HeadCommand.cs`

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
    /// ファイルの先頭を出力するコマンド
    /// </summary>
    [Command("head", "Output the first part of files")]
    public class HeadCommand : ICommand
    {
        [Option("lines", "n", Description = "Output the first K lines (default: 10)")]
        public string Lines;

        [Option("bytes", "c", Description = "Output the first K bytes")]
        public string Bytes;

        [Option("quiet", "q", Description = "Never output headers giving file names")]
        public bool Quiet;

        [Option("verbose", "v", Description = "Always output headers giving file names")]
        public bool Verbose;

        public string CommandName => "head";
        public string Description => "Output the first part of files";

        private const int DefaultLines = 10;

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            // -n と -c は排他
            if (!string.IsNullOrEmpty(Lines) && !string.IsNullOrEmpty(Bytes))
            {
                await context.Stderr.WriteLineAsync("head: cannot specify both --lines and --bytes", ct);
                return ExitCode.UsageError;
            }

            var files = context.PositionalArguments;

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
                await context.Stderr.WriteLineAsync($"head: cannot open '{filePath}' for reading: No such file or directory", ct);
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

            return await OutputFileLinesAsync(context, resolvedPath, ct);
        }

        private async Task<ExitCode> OutputFileLinesAsync(
            CommandContext context,
            string filePath,
            CancellationToken ct)
        {
            var (count, excludeLast) = ParseCount(Lines, DefaultLines);
            var lines = await File.ReadAllLinesAsync(filePath, ct);

            var outputLines = excludeLast
                ? lines.SkipLast(count)
                : lines.Take(count);

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
            var (count, excludeLast) = ParseCount(Bytes, 0);
            var content = await File.ReadAllBytesAsync(filePath, ct);

            byte[] output;
            if (excludeLast)
            {
                var takeCount = Math.Max(0, content.Length - count);
                output = content.Take(takeCount).ToArray();
            }
            else
            {
                output = content.Take(count).ToArray();
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
            var (count, excludeLast) = ParseCount(Lines, DefaultLines);

            var outputLines = excludeLast
                ? allLines.SkipLast(count)
                : allLines.Take(count);

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
            var (count, excludeLast) = ParseCount(Bytes, 0);
            var fullText = string.Join("\n", allLines);
            var bytes = Encoding.UTF8.GetBytes(fullText);

            byte[] output;
            if (excludeLast)
            {
                var takeCount = Math.Max(0, bytes.Length - count);
                output = bytes.Take(takeCount).ToArray();
            }
            else
            {
                output = bytes.Take(count).ToArray();
            }

            var text = Encoding.UTF8.GetString(output);
            await context.Stdout.WriteAsync(text, ct);

            return ExitCode.Success;
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
        /// カウント値をパース（-Kは末尾を除外、Kは先頭から）
        /// </summary>
        private static (int count, bool excludeLast) ParseCount(string value, int defaultCount)
        {
            if (string.IsNullOrEmpty(value))
                return (defaultCount, false);

            if (value.StartsWith("-"))
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

**変更内容**: `RegisterBuiltInCommands()`に`HeadCommand`を追加

```csharp
private void RegisterBuiltInCommands()
{
    // 既存のコマンド...
    Register<HeadCommand>();
}
```

## 技術詳細

### ParseCount メソッド

Linuxのheadでは、`-n -5`のように`-`を付けると「最後の5行を除いた全て」という意味になります：

```
-n 10   → 先頭の10行
-n -10  → 最後の10行を除いた全て
```

これはtailの`+K`記法と対称的な動作です：
- head `-K` → 末尾のK行/バイトを除外
- tail `+K` → 先頭からK行/バイト目以降

### 複数ファイル時のヘッダー

```
$ head file1.txt file2.txt
==> file1.txt <==
(file1の内容)

==> file2.txt <==
(file2の内容)
```

### エラー処理

存在しないファイルがあっても、他のファイルは処理を継続：

```
$ head missing.txt exists.txt
head: cannot open 'missing.txt' for reading: No such file or directory
==> exists.txt <==
(exists.txtの内容)
```

終了コードは、1つでもエラーがあれば`RuntimeError`を返します。

## 実装順序

1. `HeadCommand.cs`を新規作成
2. `Terminal.cs`でコマンドを登録
3. 動作確認

## 検証方法

### 基本機能テスト

```bash
# デフォルト（先頭の10行）
head test.txt

# 行数指定
head -n 5 test.txt

# 末尾を除外
head -n -5 test.txt

# バイト数指定
head -c 100 test.txt

# 複数ファイル
head file1.txt file2.txt

# 複数ファイル（ヘッダーなし）
head -q file1.txt file2.txt

# 単一ファイル（ヘッダーあり）
head -v test.txt

# パイプライン
cat test.txt | head -n 5
```

### エッジケーステスト

1. **存在しないファイル**: エラーメッセージ出力
2. **空ファイル**: 何も出力せず正常終了
3. **行数がファイルより多い**: 全行出力
4. **-n と -c の同時指定**: エラー
5. **-n -K でKがファイル行数より大きい**: 何も出力しない

## ファイル一覧

### 新規作成
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/BuiltInCommands/HeadCommand.cs`

### 修正
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Terminal.cs`

## headとtailの比較

| 項目 | head | tail |
|------|------|------|
| デフォルト出力 | 先頭10行 | 末尾10行 |
| `-K`記法 | 末尾K行/バイトを除外 | - |
| `+K`記法 | - | 先頭からK行/バイト目以降 |
| `-f`オプション | なし | ファイル監視 |

## CLAUDE.md準拠

- **ネスト制限**: 最大3レベル（namespace除く）
- **switch制限**: 使用せず、条件分岐はif文で処理
- **可読性優先**: メソッドを分割し、意図が明確な命名
- **制御文スタイル**: 2行形式を使用
