using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// lsコマンドのソート方法。
    /// </summary>
    public enum LsSortType
    {
        Name,
        Size,
        Time
    }

    /// <summary>
    /// ディレクトリの内容を一覧表示します。
    /// </summary>
    [Command("ls", "List directory contents")]
    public class LsCommand : ICommand
    {
        [Option("all", "a", Description = "Do not ignore entries starting with .")]
        public bool ShowAll;

        [Option("long", "l", Description = "Use a long listing format")]
        public bool LongFormat;

        [Option("human-readable", "h", Description = "Print sizes in human readable format")]
        public bool HumanReadable;

        [Option("reverse", "r", Description = "Reverse order while sorting")]
        public bool Reverse;

        [Option("recursive", "R", Description = "List subdirectories recursively")]
        public bool Recursive;

        [Option("sort", "S", Description = "Sort by: name, size, time")]
        public LsSortType SortBy;

        public string CommandName => "ls";
        public string Description => "List directory contents";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            var paths = context.PositionalArguments.Count > 0
                ? context.PositionalArguments.ToList()
                : new List<string> { "." };

            bool hasError = false;
            bool multiplePaths = paths.Count > 1 || Recursive;

            for (int i = 0; i < paths.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var path = paths[i];
                var resolvedPath = PathUtility.ResolvePath(path, context.WorkingDirectory, context.HomeDirectory);

                // パスの存在確認
                if (!Directory.Exists(resolvedPath) && !File.Exists(resolvedPath))
                {
                    await context.Stderr.WriteLineAsync($"ls: {path}: No such file or directory", ct);
                    hasError = true;
                    continue;
                }

                // ファイルの場合、そのファイルの情報を表示
                if (File.Exists(resolvedPath))
                {
                    var fileInfo = new FileInfo(resolvedPath);
                    await PrintEntryAsync(context, fileInfo, ct);
                    continue;
                }

                // 複数パスまたは再帰の場合、ディレクトリ名を表示
                if (multiplePaths)
                {
                    if (i > 0)
                    {
                        await context.Stdout.WriteLineAsync("", ct);
                    }
                    await context.Stdout.WriteLineAsync($"{path}:", ct);
                }

                try
                {
                    await ListDirectoryAsync(context, resolvedPath, ct);

                    // 再帰モードの場合、サブディレクトリも処理
                    if (Recursive)
                    {
                        await ListRecursiveAsync(context, resolvedPath, ct);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    await context.Stderr.WriteLineAsync($"ls: {path}: Permission denied", ct);
                    hasError = true;
                }
                catch (Exception ex)
                {
                    await context.Stderr.WriteLineAsync($"ls: {path}: {ex.Message}", ct);
                    hasError = true;
                }
            }

            return hasError ? ExitCode.RuntimeError : ExitCode.Success;
        }

        /// <summary>
        /// ディレクトリの内容を一覧表示します。
        /// </summary>
        private async Task ListDirectoryAsync(CommandContext context, string directoryPath, CancellationToken ct)
        {
            var entries = GetSortedEntries(directoryPath);

            if (LongFormat)
            {
                foreach (var entry in entries)
                {
                    await PrintEntryAsync(context, entry, ct);
                }
            }
            else
            {
                // 通常形式: ファイル名をスペース区切りで表示
                var names = entries.Select(e => e.Name).ToList();
                if (names.Count > 0)
                {
                    await context.Stdout.WriteLineAsync(string.Join("  ", names), ct);
                }
            }
        }

        /// <summary>
        /// 再帰的にサブディレクトリを表示します。
        /// </summary>
        private async Task ListRecursiveAsync(CommandContext context, string directoryPath, CancellationToken ct)
        {
            var directories = Directory.GetDirectories(directoryPath)
                .Where(d => ShowAll || !Path.GetFileName(d).StartsWith("."))
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase);

            foreach (var subDir in directories)
            {
                ct.ThrowIfCancellationRequested();

                await context.Stdout.WriteLineAsync("", ct);
                await context.Stdout.WriteLineAsync($"{subDir}:", ct);

                try
                {
                    await ListDirectoryAsync(context, subDir, ct);
                    await ListRecursiveAsync(context, subDir, ct);
                }
                catch (UnauthorizedAccessException)
                {
                    await context.Stderr.WriteLineAsync($"ls: {subDir}: Permission denied", ct);
                }
            }
        }

        /// <summary>
        /// ソートされたエントリを取得します。
        /// </summary>
        private IEnumerable<FileSystemInfo> GetSortedEntries(string directoryPath)
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            var entries = dirInfo.GetFileSystemInfos()
                .Where(e => ShowAll || !e.Name.StartsWith("."));

            // ソート
            IOrderedEnumerable<FileSystemInfo> sorted;
            switch (SortBy)
            {
                case LsSortType.Size:
                    sorted = entries.OrderByDescending(e => GetSize(e));
                    break;
                case LsSortType.Time:
                    sorted = entries.OrderByDescending(e => e.LastWriteTime);
                    break;
                case LsSortType.Name:
                default:
                    sorted = entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase);
                    break;
            }

            var result = sorted.ToList();

            if (Reverse)
            {
                result.Reverse();
            }

            return result;
        }

        /// <summary>
        /// ファイルサイズを取得します（ディレクトリの場合は0）。
        /// </summary>
        private long GetSize(FileSystemInfo entry)
        {
            if (entry is FileInfo fileInfo)
            {
                return fileInfo.Length;
            }
            return 0;
        }

        /// <summary>
        /// エントリを詳細形式で出力します。
        /// </summary>
        private async Task PrintEntryAsync(CommandContext context, FileSystemInfo entry, CancellationToken ct)
        {
            if (LongFormat)
            {
                var permissions = GetPermissionString(entry);
                var linkCount = entry is DirectoryInfo ? 2 : 1;
                var size = GetSize(entry);
                var sizeStr = HumanReadable ? FormatSize(size) : size.ToString();
                var dateStr = entry.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
                var name = entry.Name;

                // ディレクトリの場合、末尾にスラッシュを付ける
                if (entry is DirectoryInfo)
                {
                    name += "/";
                }

                // 右揃えでサイズを表示
                await context.Stdout.WriteLineAsync(
                    $"{permissions}  {linkCount}  {sizeStr,8}  {dateStr}  {name}", ct);
            }
            else
            {
                var name = entry.Name;
                if (entry is DirectoryInfo)
                {
                    name += "/";
                }
                await context.Stdout.WriteLineAsync(name, ct);
            }
        }

        /// <summary>
        /// パーミッション文字列を取得します。
        /// </summary>
        private string GetPermissionString(FileSystemInfo entry)
        {
            var isDir = entry is DirectoryInfo;
            var typeChar = isDir ? 'd' : '-';

            // 実際のパーミッションを取得（簡略化版）
            bool canRead = true;
            bool canWrite = !entry.Attributes.HasFlag(FileAttributes.ReadOnly);
            bool canExecute = isDir; // ディレクトリは実行可能とみなす

            // Unix形式のパーミッション文字列を構築
            var ownerPerms = $"{(canRead ? 'r' : '-')}{(canWrite ? 'w' : '-')}{(canExecute ? 'x' : '-')}";

            // 簡略化のため、owner/group/otherは同じ値を使用
            return $"{typeChar}{ownerPerms}{ownerPerms}{ownerPerms}";
        }

        /// <summary>
        /// ファイルサイズを人間が読みやすい形式にフォーマットします。
        /// </summary>
        private string FormatSize(long bytes)
        {
            string[] units = { "B", "K", "M", "G", "T" };
            int unitIndex = 0;
            double size = bytes;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return unitIndex == 0
                ? $"{size:F0}{units[unitIndex]}"
                : $"{size:F1}{units[unitIndex]}";
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // パス補完はCompletionEngineで処理
            yield break;
        }
    }
}
