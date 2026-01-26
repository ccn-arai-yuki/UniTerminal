using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// 2つのファイルの差分を比較・表示するコマンド。
    /// </summary>
    [Command("diff", "Compare files line by line")]
    public class DiffCommand : ICommand
    {
        /// <summary>
        /// Unified形式のコンテキスト行数。
        /// </summary>
        [Option("unified", "u", Description = "Output in unified format with N lines of context")]
        public int UnifiedContext = -1;

        /// <summary>
        /// 大文字小文字を無視するかどうか。
        /// </summary>
        [Option("ignore-case", "i", Description = "Ignore case differences")]
        public bool IgnoreCase;

        /// <summary>
        /// 空白の増減を無視するかどうか。
        /// </summary>
        [Option("ignore-space", "b", Description = "Ignore changes in whitespace amount")]
        public bool IgnoreSpaceChange;

        /// <summary>
        /// すべての空白を無視するかどうか。
        /// </summary>
        [Option("ignore-all-space", "w", Description = "Ignore all whitespace")]
        public bool IgnoreAllSpace;

        /// <summary>
        /// 差分があるかどうかのみを出力するかどうか。
        /// </summary>
        [Option("brief", "q", Description = "Report only when files differ")]
        public bool Brief;

        /// <summary>
        /// コマンド名。
        /// </summary>
        public string CommandName => "diff";

        /// <summary>
        /// コマンドの説明。
        /// </summary>
        public string Description => "Compare files line by line";

        /// <summary>
        /// コマンドを実行します。
        /// </summary>
        /// <param name="context">実行コンテキスト。</param>
        /// <param name="ct">キャンセルトークン。</param>
        /// <returns>終了コード。</returns>
        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count < 2)
            {
                await context.Stderr.WriteLineAsync("diff: missing operand", ct);
                await context.Stderr.WriteLineAsync("Usage: diff [options] <file1> <file2>", ct);
                return ExitCode.UsageError;
            }

            var file1Path = context.PositionalArguments[0];
            var file2Path = context.PositionalArguments[1];

            // ファイルの読み込み
            string[] lines1, lines2;

            try
            {
                lines1 = await ReadFileAsync(file1Path, context, ct);
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync($"diff: {file1Path}: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }

            try
            {
                lines2 = await ReadFileAsync(file2Path, context, ct);
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync($"diff: {file2Path}: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }

            // 差分計算
            var diff = ComputeDiff(lines1, lines2);

            // 差分がない場合
            if (!diff.HasDifferences)
            {
                return ExitCode.Success;
            }

            // Brief出力
            if (Brief)
            {
                await context.Stdout.WriteLineAsync($"Files {PathUtility.NormalizeToSlash(file1Path)} and {PathUtility.NormalizeToSlash(file2Path)} differ", ct);
                return (ExitCode)1; // 差分あり
            }

            // Unified形式
            if (UnifiedContext >= 0)
            {
                await OutputUnifiedAsync(context, PathUtility.NormalizeToSlash(file1Path), PathUtility.NormalizeToSlash(file2Path), lines1, lines2, diff, ct);
            }
            else
            {
                // Normal形式
                await OutputNormalAsync(context, diff, ct);
            }

            return (ExitCode)1; // 差分あり
        }

        /// <summary>
        /// ファイルを読み込みます。
        /// </summary>
        private async Task<string[]> ReadFileAsync(string path, CommandContext context, CancellationToken ct)
        {
            // 標準入力
            if (path == "-")
            {
                var lines = new List<string>();
                if (context.Stdin != null)
                {
                    await foreach (var line in context.Stdin.ReadLinesAsync(ct))
                    {
                        lines.Add(line);
                    }
                }
                return lines.ToArray();
            }

            var resolvedPath = PathUtility.ResolvePath(path, context.WorkingDirectory, context.HomeDirectory);

            if (Directory.Exists(resolvedPath))
            {
                throw new IOException("Is a directory");
            }

            if (!File.Exists(resolvedPath))
            {
                throw new FileNotFoundException("No such file or directory");
            }

            return await File.ReadAllLinesAsync(resolvedPath, ct);
        }

        /// <summary>
        /// 差分を計算します（LCSアルゴリズム）。
        /// </summary>
        private DiffResult ComputeDiff(string[] lines1, string[] lines2)
        {
            var lcs = ComputeLcsTable(lines1, lines2);
            var diffLines = BacktrackDiff(lines1, lines2, lcs);
            return new DiffResult(diffLines);
        }

        /// <summary>
        /// LCS（最長共通部分列）テーブルを計算します。
        /// </summary>
        private int[,] ComputeLcsTable(string[] lines1, string[] lines2)
        {
            int m = lines1.Length;
            int n = lines2.Length;
            int[,] lcs = new int[m + 1, n + 1];

            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (LinesEqual(lines1[i - 1], lines2[j - 1]))
                        lcs[i, j] = lcs[i - 1, j - 1] + 1;
                    else
                        lcs[i, j] = Math.Max(lcs[i - 1, j], lcs[i, j - 1]);
                }
            }

            return lcs;
        }

        /// <summary>
        /// LCSテーブルをバックトラックして差分行を生成します。
        /// </summary>
        private List<DiffLine> BacktrackDiff(string[] lines1, string[] lines2, int[,] lcs)
        {
            var diffLines = new List<DiffLine>();
            int x = lines1.Length;
            int y = lines2.Length;

            while (x > 0 || y > 0)
            {
                var diffLine = GetNextDiffLine(lines1, lines2, lcs, ref x, ref y);
                diffLines.Insert(0, diffLine);
            }

            return diffLines;
        }

        /// <summary>
        /// バックトラック中の次の差分行を取得します。
        /// </summary>
        private DiffLine GetNextDiffLine(string[] lines1, string[] lines2, int[,] lcs, ref int x, ref int y)
        {
            if (x > 0 && y > 0 && LinesEqual(lines1[x - 1], lines2[y - 1]))
            {
                var line = new DiffLine(DiffType.Context, lines1[x - 1], x, y);
                x--;
                y--;
                return line;
            }

            if (y > 0 && (x == 0 || lcs[x, y - 1] >= lcs[x - 1, y]))
            {
                var line = new DiffLine(DiffType.Add, lines2[y - 1], x, y);
                y--;
                return line;
            }

            var deleteLine = new DiffLine(DiffType.Delete, lines1[x - 1], x, y);
            x--;
            return deleteLine;
        }

        /// <summary>
        /// 行が等しいかどうかを比較します。
        /// </summary>
        private bool LinesEqual(string line1, string line2)
        {
            var a = NormalizeLine(line1);
            var b = NormalizeLine(line2);

            return IgnoreCase
                ? string.Equals(a, b, StringComparison.OrdinalIgnoreCase)
                : string.Equals(a, b);
        }

        /// <summary>
        /// 行を正規化します。
        /// </summary>
        private string NormalizeLine(string line)
        {
            if (IgnoreAllSpace)
                return Regex.Replace(line, @"\s+", "");
            if (IgnoreSpaceChange)
                return Regex.Replace(line, @"\s+", " ").Trim();
            return line;
        }

        /// <summary>
        /// Normal形式で出力します。
        /// </summary>
        private async Task OutputNormalAsync(CommandContext context, DiffResult diff, CancellationToken ct)
        {
            var hunks = diff.GetHunks();

            foreach (var hunk in hunks)
            {
                ct.ThrowIfCancellationRequested();
                await OutputHunkAsync(context, hunk, ct);
            }
        }

        /// <summary>
        /// 単一のハンクをNormal形式で出力します。
        /// </summary>
        private async Task OutputHunkAsync(CommandContext context, DiffHunk hunk, CancellationToken ct)
        {
            await context.Stdout.WriteLineAsync(FormatHunkRange(hunk), ct);

            foreach (var line in hunk.DeletedLines)
                await context.Stdout.WriteLineAsync($"< {line}", ct);

            if (hunk.DeleteCount > 0 && hunk.AddCount > 0)
                await context.Stdout.WriteLineAsync("---", ct);

            foreach (var line in hunk.AddedLines)
                await context.Stdout.WriteLineAsync($"> {line}", ct);
        }

        /// <summary>
        /// ハンクの範囲文字列を生成します。
        /// </summary>
        private string FormatHunkRange(DiffHunk hunk)
        {
            if (hunk.DeleteCount > 0 && hunk.AddCount > 0)
                return $"{FormatRange(hunk.OldStart, hunk.DeleteCount)}c{FormatRange(hunk.NewStart, hunk.AddCount)}";

            if (hunk.DeleteCount > 0)
                return $"{FormatRange(hunk.OldStart, hunk.DeleteCount)}d{hunk.NewStart}";

            return $"{hunk.OldStart}a{FormatRange(hunk.NewStart, hunk.AddCount)}";
        }

        /// <summary>
        /// Unified形式で出力します。
        /// </summary>
        private async Task OutputUnifiedAsync(
            CommandContext context,
            string file1Path,
            string file2Path,
            string[] lines1,
            string[] lines2,
            DiffResult diff,
            CancellationToken ct)
        {
            int contextLines = UnifiedContext >= 0 ? UnifiedContext : 3;

            await context.Stdout.WriteLineAsync($"--- {file1Path}", ct);
            await context.Stdout.WriteLineAsync($"+++ {file2Path}", ct);

            var hunks = diff.GetUnifiedHunks(lines1, lines2, contextLines);

            foreach (var hunk in hunks)
            {
                ct.ThrowIfCancellationRequested();
                await OutputUnifiedHunkAsync(context, hunk, ct);
            }
        }

        /// <summary>
        /// 単一のハンクをUnified形式で出力します。
        /// </summary>
        private async Task OutputUnifiedHunkAsync(CommandContext context, UnifiedHunk hunk, CancellationToken ct)
        {
            await context.Stdout.WriteLineAsync(
                $"@@ -{hunk.OldStart},{hunk.OldCount} +{hunk.NewStart},{hunk.NewCount} @@", ct);

            foreach (var line in hunk.Lines)
                await context.Stdout.WriteLineAsync($"{GetDiffLinePrefix(line.Type)}{line.Content}", ct);
        }

        /// <summary>
        /// 差分タイプに応じたプレフィックスを取得します。
        /// </summary>
        private string GetDiffLinePrefix(DiffType type)
        {
            return type switch
            {
                DiffType.Add => "+",
                DiffType.Delete => "-",
                _ => " "
            };
        }

        /// <summary>
        /// 範囲を文字列にフォーマットします。
        /// </summary>
        private string FormatRange(int start, int count)
        {
            if (count == 1)
                return start.ToString();
            return $"{start},{start + count - 1}";
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            yield break;
        }
    }

    /// <summary>
    /// 差分の種類。
    /// </summary>
    public enum DiffType
    {
        Context,
        Add,
        Delete
    }

    /// <summary>
    /// 差分行。
    /// </summary>
    public class DiffLine
    {
        public DiffType Type { get; }
        public string Content { get; }
        public int OldLineNumber { get; }
        public int NewLineNumber { get; }

        public DiffLine(DiffType type, string content, int oldLineNumber, int newLineNumber)
        {
            Type = type;
            Content = content;
            OldLineNumber = oldLineNumber;
            NewLineNumber = newLineNumber;
        }
    }

    /// <summary>
    /// 差分結果。
    /// </summary>
    public class DiffResult
    {
        public List<DiffLine> Lines { get; }
        public bool HasDifferences => Lines.Exists(l => l.Type != DiffType.Context);

        public DiffResult(List<DiffLine> lines)
        {
            Lines = lines;
        }

        /// <summary>
        /// Normal形式用のハンクを取得します。
        /// </summary>
        public List<DiffHunk> GetHunks()
        {
            var hunks = new List<DiffHunk>();
            DiffHunk currentHunk = null;
            int oldLine = 1;
            int newLine = 1;

            foreach (var line in Lines)
            {
                if (line.Type == DiffType.Context)
                    currentHunk = ProcessContextLine(hunks, currentHunk, ref oldLine, ref newLine);
                else
                    currentHunk = ProcessDiffLine(currentHunk, line, oldLine, newLine, ref oldLine, ref newLine);
            }

            if (currentHunk != null)
                hunks.Add(currentHunk);

            return hunks;
        }

        private DiffHunk ProcessContextLine(List<DiffHunk> hunks, DiffHunk currentHunk, ref int oldLine, ref int newLine)
        {
            if (currentHunk != null)
                hunks.Add(currentHunk);

            oldLine++;
            newLine++;
            return null;
        }

        private DiffHunk ProcessDiffLine(DiffHunk currentHunk, DiffLine line, int oldLine, int newLine, ref int oldLineRef, ref int newLineRef)
        {
            var hunk = currentHunk ?? new DiffHunk(oldLine, newLine);

            if (line.Type == DiffType.Delete)
            {
                hunk.DeletedLines.Add(line.Content);
                oldLineRef++;
            }
            else if (line.Type == DiffType.Add)
            {
                hunk.AddedLines.Add(line.Content);
                newLineRef++;
            }

            return hunk;
        }

        /// <summary>
        /// Unified形式用のハンクを取得します。
        /// </summary>
        public List<UnifiedHunk> GetUnifiedHunks(string[] lines1, string[] lines2, int contextLines)
        {
            var hunks = new List<UnifiedHunk>();
            var diffIndexes = CollectDiffIndexes();

            if (diffIndexes.Count == 0)
                return hunks;

            int groupStart = 0;
            while (groupStart < diffIndexes.Count)
            {
                int groupEnd = FindGroupEndIndex(diffIndexes, groupStart, contextLines);
                var hunk = CreateUnifiedHunkFromRange(diffIndexes, groupStart, groupEnd, contextLines);
                hunks.Add(hunk);
                groupStart = groupEnd + 1;
            }

            return hunks;
        }

        /// <summary>
        /// 差分がある行のインデックスを収集します。
        /// </summary>
        private List<int> CollectDiffIndexes()
        {
            var indexes = new List<int>();

            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].Type != DiffType.Context)
                    indexes.Add(i);
            }

            return indexes;
        }

        /// <summary>
        /// 連続する差分グループの終端インデックスを検索します。
        /// </summary>
        private int FindGroupEndIndex(List<int> diffIndexes, int groupStart, int contextLines)
        {
            int groupEnd = groupStart;
            int mergeThreshold = contextLines * 2 + 1;

            while (groupEnd + 1 < diffIndexes.Count &&
                   diffIndexes[groupEnd + 1] - diffIndexes[groupEnd] <= mergeThreshold)
            {
                groupEnd++;
            }

            return groupEnd;
        }

        /// <summary>
        /// 指定範囲からUnifiedハンクを作成します。
        /// </summary>
        private UnifiedHunk CreateUnifiedHunkFromRange(List<int> diffIndexes, int groupStart, int groupEnd, int contextLines)
        {
            int startIdx = Math.Max(0, diffIndexes[groupStart] - contextLines);
            int endIdx = Math.Min(Lines.Count - 1, diffIndexes[groupEnd] + contextLines);

            var (oldStart, newStart) = CalculateHunkStartPositions(startIdx);
            var (hunkLines, oldCount, newCount) = CollectHunkLinesWithCounts(startIdx, endIdx);

            return new UnifiedHunk(oldStart, oldCount, newStart, newCount, hunkLines);
        }

        /// <summary>
        /// ハンクの開始位置（oldStart, newStart）を計算します。
        /// </summary>
        private (int oldStart, int newStart) CalculateHunkStartPositions(int startIdx)
        {
            int oldStart = 1;
            int newStart = 1;

            for (int i = 0; i < startIdx; i++)
            {
                if (Lines[i].Type == DiffType.Context || Lines[i].Type == DiffType.Delete)
                    oldStart++;
                if (Lines[i].Type == DiffType.Context || Lines[i].Type == DiffType.Add)
                    newStart++;
            }

            return (oldStart, newStart);
        }

        /// <summary>
        /// ハンクの行を収集し、行数をカウントします。
        /// </summary>
        private (List<DiffLine> lines, int oldCount, int newCount) CollectHunkLinesWithCounts(int startIdx, int endIdx)
        {
            var hunkLines = new List<DiffLine>();
            int oldCount = 0;
            int newCount = 0;

            for (int i = startIdx; i <= endIdx; i++)
            {
                hunkLines.Add(Lines[i]);
                if (Lines[i].Type == DiffType.Context || Lines[i].Type == DiffType.Delete)
                    oldCount++;
                if (Lines[i].Type == DiffType.Context || Lines[i].Type == DiffType.Add)
                    newCount++;
            }

            return (hunkLines, oldCount, newCount);
        }
    }
}
