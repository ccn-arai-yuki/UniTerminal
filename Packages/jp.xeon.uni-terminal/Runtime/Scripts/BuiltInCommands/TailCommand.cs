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

        /// <summary>
        /// 標準入力から読み取って出力する。
        /// </summary>
        private async Task<ExitCode> ProcessStdinAsync(CommandContext context, CancellationToken ct)
        {
            var allLines = await ReadAllLinesFromReaderAsync(context.Stdin, ct);

            if (!string.IsNullOrEmpty(Bytes))
                return await OutputByBytesAsync(context, allLines, ct);

            return await OutputByLinesAsync(context, allLines, ct);
        }

        /// <summary>
        /// 指定ファイルの末尾を出力する。
        /// </summary>
        private async Task<ExitCode> ProcessFileAsync(
            CommandContext context,
            string filePath,
            bool showHeader,
            bool isFirst,
            CancellationToken ct)
        {
            var resolvedPath = PathUtility.ResolvePath(filePath, context.WorkingDirectory, context.HomeDirectory);

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

        /// <summary>
        /// ファイルの末尾を行数で出力する。
        /// </summary>
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

        /// <summary>
        /// ファイルの末尾をバイト数で出力する。
        /// </summary>
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

        /// <summary>
        /// 行リストの末尾を行数で出力する。
        /// </summary>
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

        /// <summary>
        /// 行リストの末尾をバイト数で出力する。
        /// </summary>
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

        /// <summary>
        /// リーダーから全行を読み取る。
        /// </summary>
        private static async Task<List<string>> ReadAllLinesFromReaderAsync(
            IAsyncTextReader reader,
            CancellationToken ct)
        {
            var lines = new List<string>();
            await foreach (var line in reader.ReadLinesAsync(ct))
            {
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
