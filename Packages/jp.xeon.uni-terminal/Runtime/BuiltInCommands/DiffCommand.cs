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
        [Option("unified", "u", Description = "Output in unified format with N lines of context")]
        public int UnifiedContext = -1;

        [Option("ignore-case", "i", Description = "Ignore case differences")]
        public bool IgnoreCase;

        [Option("ignore-space", "b", Description = "Ignore changes in whitespace amount")]
        public bool IgnoreSpaceChange;

        [Option("ignore-all-space", "w", Description = "Ignore all whitespace")]
        public bool IgnoreAllSpace;

        [Option("brief", "q", Description = "Report only when files differ")]
        public bool Brief;

        public string CommandName => "diff";
        public string Description => "Compare files line by line";

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
                await context.Stdout.WriteLineAsync($"Files {file1Path} and {file2Path} differ", ct);
                return (ExitCode)1; // 差分あり
            }

            // Unified形式
            if (UnifiedContext >= 0)
            {
                await OutputUnifiedAsync(context, file1Path, file2Path, lines1, lines2, diff, ct);
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
            int m = lines1.Length;
            int n = lines2.Length;

            // LCS（最長共通部分列）の長さを計算
            int[,] lcs = new int[m + 1, n + 1];

            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (LinesEqual(lines1[i - 1], lines2[j - 1]))
                    {
                        lcs[i, j] = lcs[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        lcs[i, j] = Math.Max(lcs[i - 1, j], lcs[i, j - 1]);
                    }
                }
            }

            // バックトラックして差分を生成
            var diffLines = new List<DiffLine>();
            int x = m, y = n;

            while (x > 0 || y > 0)
            {
                if (x > 0 && y > 0 && LinesEqual(lines1[x - 1], lines2[y - 1]))
                {
                    diffLines.Insert(0, new DiffLine(DiffType.Context, lines1[x - 1], x, y));
                    x--;
                    y--;
                }
                else if (y > 0 && (x == 0 || lcs[x, y - 1] >= lcs[x - 1, y]))
                {
                    diffLines.Insert(0, new DiffLine(DiffType.Add, lines2[y - 1], x, y));
                    y--;
                }
                else if (x > 0)
                {
                    diffLines.Insert(0, new DiffLine(DiffType.Delete, lines1[x - 1], x, y));
                    x--;
                }
            }

            return new DiffResult(diffLines);
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

                // ハンク情報を出力
                string rangeStr;
                if (hunk.DeleteCount > 0 && hunk.AddCount > 0)
                {
                    // 変更
                    rangeStr = $"{FormatRange(hunk.OldStart, hunk.DeleteCount)}c{FormatRange(hunk.NewStart, hunk.AddCount)}";
                }
                else if (hunk.DeleteCount > 0)
                {
                    // 削除
                    rangeStr = $"{FormatRange(hunk.OldStart, hunk.DeleteCount)}d{hunk.NewStart}";
                }
                else
                {
                    // 追加
                    rangeStr = $"{hunk.OldStart}a{FormatRange(hunk.NewStart, hunk.AddCount)}";
                }

                await context.Stdout.WriteLineAsync(rangeStr, ct);

                // 削除行
                foreach (var line in hunk.DeletedLines)
                {
                    await context.Stdout.WriteLineAsync($"< {line}", ct);
                }

                // 区切り
                if (hunk.DeleteCount > 0 && hunk.AddCount > 0)
                {
                    await context.Stdout.WriteLineAsync("---", ct);
                }

                // 追加行
                foreach (var line in hunk.AddedLines)
                {
                    await context.Stdout.WriteLineAsync($"> {line}", ct);
                }
            }
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

            // ヘッダー
            await context.Stdout.WriteLineAsync($"--- {file1Path}", ct);
            await context.Stdout.WriteLineAsync($"+++ {file2Path}", ct);

            var hunks = diff.GetUnifiedHunks(lines1, lines2, contextLines);

            foreach (var hunk in hunks)
            {
                ct.ThrowIfCancellationRequested();

                // ハンクヘッダー
                await context.Stdout.WriteLineAsync(
                    $"@@ -{hunk.OldStart},{hunk.OldCount} +{hunk.NewStart},{hunk.NewCount} @@", ct);

                // 行を出力
                foreach (var line in hunk.Lines)
                {
                    string prefix = line.Type switch
                    {
                        DiffType.Add => "+",
                        DiffType.Delete => "-",
                        _ => " "
                    };
                    await context.Stdout.WriteLineAsync($"{prefix}{line.Content}", ct);
                }
            }
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
                {
                    if (currentHunk != null)
                    {
                        hunks.Add(currentHunk);
                        currentHunk = null;
                    }
                    oldLine++;
                    newLine++;
                }
                else
                {
                    if (currentHunk == null)
                    {
                        currentHunk = new DiffHunk(oldLine, newLine);
                    }

                    if (line.Type == DiffType.Delete)
                    {
                        currentHunk.DeletedLines.Add(line.Content);
                        oldLine++;
                    }
                    else if (line.Type == DiffType.Add)
                    {
                        currentHunk.AddedLines.Add(line.Content);
                        newLine++;
                    }
                }
            }

            if (currentHunk != null)
            {
                hunks.Add(currentHunk);
            }

            return hunks;
        }

        /// <summary>
        /// Unified形式用のハンクを取得します。
        /// </summary>
        public List<UnifiedHunk> GetUnifiedHunks(string[] lines1, string[] lines2, int contextLines)
        {
            var hunks = new List<UnifiedHunk>();
            var diffIndexes = new List<int>();

            // 差分がある行のインデックスを収集
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].Type != DiffType.Context)
                {
                    diffIndexes.Add(i);
                }
            }

            if (diffIndexes.Count == 0)
                return hunks;

            // 差分をグループ化してハンクを作成
            int groupStart = 0;
            while (groupStart < diffIndexes.Count)
            {
                int groupEnd = groupStart;

                // 連続する差分をグループ化（コンテキスト行数以内なら同じハンクに）
                while (groupEnd + 1 < diffIndexes.Count &&
                       diffIndexes[groupEnd + 1] - diffIndexes[groupEnd] <= contextLines * 2 + 1)
                {
                    groupEnd++;
                }

                // ハンクの範囲を計算
                int startIdx = Math.Max(0, diffIndexes[groupStart] - contextLines);
                int endIdx = Math.Min(Lines.Count - 1, diffIndexes[groupEnd] + contextLines);

                var hunkLines = new List<DiffLine>();
                int oldStart = 1, newStart = 1;
                int oldCount = 0, newCount = 0;

                // 開始位置を計算
                for (int i = 0; i < startIdx; i++)
                {
                    if (Lines[i].Type == DiffType.Context || Lines[i].Type == DiffType.Delete)
                        oldStart++;
                    if (Lines[i].Type == DiffType.Context || Lines[i].Type == DiffType.Add)
                        newStart++;
                }

                // ハンクの行を収集
                for (int i = startIdx; i <= endIdx; i++)
                {
                    hunkLines.Add(Lines[i]);
                    if (Lines[i].Type == DiffType.Context || Lines[i].Type == DiffType.Delete)
                        oldCount++;
                    if (Lines[i].Type == DiffType.Context || Lines[i].Type == DiffType.Add)
                        newCount++;
                }

                hunks.Add(new UnifiedHunk(oldStart, oldCount, newStart, newCount, hunkLines));
                groupStart = groupEnd + 1;
            }

            return hunks;
        }
    }

    /// <summary>
    /// Normal形式のハンク。
    /// </summary>
    public class DiffHunk
    {
        public int OldStart { get; }
        public int NewStart { get; }
        public List<string> DeletedLines { get; } = new List<string>();
        public List<string> AddedLines { get; } = new List<string>();
        public int DeleteCount => DeletedLines.Count;
        public int AddCount => AddedLines.Count;

        public DiffHunk(int oldStart, int newStart)
        {
            OldStart = oldStart;
            NewStart = newStart;
        }
    }

    /// <summary>
    /// Unified形式のハンク。
    /// </summary>
    public class UnifiedHunk
    {
        public int OldStart { get; }
        public int OldCount { get; }
        public int NewStart { get; }
        public int NewCount { get; }
        public List<DiffLine> Lines { get; }

        public UnifiedHunk(int oldStart, int oldCount, int newStart, int newCount, List<DiffLine> lines)
        {
            OldStart = oldStart;
            OldCount = oldCount;
            NewStart = newStart;
            NewCount = newCount;
            Lines = lines;
        }
    }
}
