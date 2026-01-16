using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// ファイルの内容をページ単位で表示するコマンド。
    /// Unity CLI環境の制約により、非対話モードで動作します。
    /// </summary>
    [Command("less", "View file contents page by page")]
    public class LessCommand : ICommand
    {
        [Option("lines", "n", Description = "Number of lines to display at once (0 = all)")]
        public int LinesPerPage = 0;

        [Option("from-line", "f", Description = "Start from specified line (1-based)")]
        public int FromLine = 1;

        [Option("line-numbers", "N", Description = "Show line numbers")]
        public bool ShowLineNumbers;

        [Option("chop-long-lines", "S", Description = "Chop long lines at 80 characters")]
        public bool ChopLongLines;

        public string CommandName => "less";
        public string Description => "View file contents page by page";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            List<string> allLines;
            string fileName = null;

            // ファイル指定がある場合
            if (context.PositionalArguments.Count > 0)
            {
                var filePath = context.PositionalArguments[0];

                // "-" は標準入力として扱う
                if (filePath == "-")
                {
                    allLines = await ReadStdinAsync(context, ct);
                }
                else
                {
                    var resolvedPath = PathUtility.ResolvePath(filePath, context.WorkingDirectory, context.HomeDirectory);

                    if (Directory.Exists(resolvedPath))
                    {
                        await context.Stderr.WriteLineAsync($"less: {filePath}: Is a directory", ct);
                        return ExitCode.RuntimeError;
                    }

                    if (!File.Exists(resolvedPath))
                    {
                        await context.Stderr.WriteLineAsync($"less: {filePath}: No such file or directory", ct);
                        return ExitCode.RuntimeError;
                    }

                    try
                    {
                        allLines = new List<string>(await File.ReadAllLinesAsync(resolvedPath, ct));
                        fileName = Path.GetFileName(resolvedPath);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        await context.Stderr.WriteLineAsync($"less: {filePath}: Permission denied", ct);
                        return ExitCode.RuntimeError;
                    }
                    catch (Exception ex)
                    {
                        await context.Stderr.WriteLineAsync($"less: {filePath}: {ex.Message}", ct);
                        return ExitCode.RuntimeError;
                    }
                }
            }
            else
            {
                // 標準入力から読み取り
                allLines = await ReadStdinAsync(context, ct);
            }

            if (allLines.Count == 0)
            {
                return ExitCode.Success;
            }

            // 開始行の検証
            if (FromLine < 1)
                FromLine = 1;
            if (FromLine > allLines.Count)
                FromLine = allLines.Count;

            // 表示
            return await DisplayAsync(context, allLines, FromLine, fileName, ct);
        }

        /// <summary>
        /// 標準入力から全行を読み取ります。
        /// </summary>
        private async Task<List<string>> ReadStdinAsync(CommandContext context, CancellationToken ct)
        {
            var lines = new List<string>();

            if (context.Stdin == null)
                return lines;

            await foreach (var line in context.Stdin.ReadLinesAsync(ct))
            {
                lines.Add(line);
            }

            return lines;
        }

        /// <summary>
        /// 内容を表示します。
        /// </summary>
        private async Task<ExitCode> DisplayAsync(
            CommandContext context,
            List<string> allLines,
            int startLine,
            string fileName,
            CancellationToken ct)
        {
            int totalLines = allLines.Count;
            int endLine = LinesPerPage > 0
                ? Math.Min(startLine + LinesPerPage - 1, totalLines)
                : totalLines;

            // ヘッダー（LinesPerPageが指定されている場合のみ）
            if (LinesPerPage > 0)
            {
                var header = string.IsNullOrEmpty(fileName)
                    ? $"(stdin) (lines {startLine}-{endLine} of {totalLines})"
                    : $"File: {fileName} (lines {startLine}-{endLine} of {totalLines})";

                await context.Stdout.WriteLineAsync(header, ct);
                await context.Stdout.WriteLineAsync(new string('-', 40), ct);
            }

            // 内容
            for (int i = startLine - 1; i < endLine; i++)
            {
                ct.ThrowIfCancellationRequested();
                string line = ProcessLine(allLines[i], i + 1);
                await context.Stdout.WriteLineAsync(line, ct);
            }

            // フッター（LinesPerPageが指定されていて、まだ残りがある場合）
            if (LinesPerPage > 0 && endLine < totalLines)
            {
                await context.Stdout.WriteLineAsync(new string('-', 40), ct);
                await context.Stdout.WriteLineAsync($"({totalLines - endLine} more lines)", ct);
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// 行を処理します（行番号追加、長い行の切り詰め）。
        /// </summary>
        private string ProcessLine(string line, int lineNumber)
        {
            // 長い行の切り詰め
            if (ChopLongLines && line.Length > 80)
            {
                line = line.Substring(0, 77) + "...";
            }

            // 行番号
            if (ShowLineNumbers)
            {
                return $"{lineNumber,6}\t{line}";
            }

            return line;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // パス補完はCompletionEngineで処理
            yield break;
        }
    }
}
