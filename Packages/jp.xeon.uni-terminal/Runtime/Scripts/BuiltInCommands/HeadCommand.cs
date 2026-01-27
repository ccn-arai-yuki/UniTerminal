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
        /// 指定ファイルの先頭を出力する。
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

        /// <summary>
        /// ファイルの先頭を行数で出力する。
        /// </summary>
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

        /// <summary>
        /// ファイルの先頭をバイト数で出力する。
        /// </summary>
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

        /// <summary>
        /// 行リストの先頭を行数で出力する。
        /// </summary>
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

        /// <summary>
        /// 行リストの先頭をバイト数で出力する。
        /// </summary>
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
