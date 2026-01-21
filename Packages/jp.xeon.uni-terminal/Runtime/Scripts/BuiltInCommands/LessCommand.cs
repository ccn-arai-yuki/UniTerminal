using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xeon.UniTerminal.BuiltInCommands.Less;

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
            var result = await ReadInputAsync(context, ct);

            if (result.HasError)
            {
                await context.Stderr.WriteLineAsync(result.Error, ct);
                return ExitCode.RuntimeError;
            }

            if (result.NoLines)
                return ExitCode.Success;

            var startLine = NormalizeFromLine(result.Lines.Count);
            return await DisplayAsync(context, startLine, result, ct);
        }

        /// <summary>
        /// 入力ソースから全行を読み取ります。
        /// </summary>
        private async Task<ReadResult> ReadInputAsync(CommandContext context, CancellationToken ct)
        {
            // 引数なしまたは"-"の場合は標準入力から読み取り
            if (context.PositionalArguments.Count == 0)
                return new (await ReadStdinAsync(context, ct), null);

            var filePath = context.PositionalArguments[0];

            if (filePath == "-")
                return new (await ReadStdinAsync(context, ct), null);

            return await ReadFileAsync(filePath, context, ct);
        }

        /// <summary>
        /// 開始行を検証して正規化します。
        /// </summary>
        private int NormalizeFromLine(int totalLines)
        {
            if (FromLine < 1)
                return 1;
            if (FromLine > totalLines)
                return totalLines;
            return FromLine;
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
        /// ファイルから全行を読み取ります。
        /// </summary>
        /// <returns>成功時は(lines, fileName, null)、エラー時は(null, null, errorMessage)を返します。</returns>
        private async Task<ReadResult> ReadFileAsync(string filePath, CommandContext context, CancellationToken ct)
        {
            var resolvedPath = PathUtility.ResolvePath(filePath, context.WorkingDirectory, context.HomeDirectory);

            if (Directory.Exists(resolvedPath))
                return new ($"less: {filePath}: Is a directory");

            if (!File.Exists(resolvedPath))
                return new ($"less: {filePath}: No such file or directory");

            try
            {
                var lines = new List<string>(await File.ReadAllLinesAsync(resolvedPath, ct));
                var fileName = Path.GetFileName(resolvedPath);
                return new (lines, fileName);
            }
            catch (UnauthorizedAccessException)
            {
                return new ($"less: {filePath}: Permission denied");
            }
            catch (Exception ex)
            {
                return new ($"less: {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// 内容を表示します。
        /// </summary>
        private async Task<ExitCode> DisplayAsync(CommandContext context, int startLine, ReadResult result, CancellationToken ct)
        {
            var totalLines = result.Lines.Count;
            var endLine = LinesPerPage > 0 ? Math.Min(startLine + LinesPerPage - 1, totalLines) : totalLines;

            // ヘッダー（LinesPerPageが指定されている場合のみ）
            if (LinesPerPage > 0)
            {
                var header = string.IsNullOrEmpty(result.FileName) 
                    ? $"(stdin) (lines {startLine}-{endLine} of {totalLines})"
                    : $"File: {result.FileName} (lines {startLine}-{endLine} of {totalLines})";

                await context.Stdout.WriteLineAsync(header, ct);
                await context.Stdout.WriteLineAsync(new string('-', 40), ct);
            }

            // 内容
            for (int i = startLine - 1; i < endLine; i++)
            {
                ct.ThrowIfCancellationRequested();
                string line = ProcessLine(result.Lines[i], i + 1);
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
