using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// lsコマンドのソート方法
    /// </summary>
    public enum LsSortType
    {
        Name,
        Size,
        Time
    }

    /// <summary>
    /// ディレクトリの内容を一覧表示します
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
            var paths = GetTargetPaths(context);
            var param = CreateParams();
            var showHeader = paths.Count > 1 || Recursive;

            var hasError = false;
            for (int i = 0; i < paths.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var result = await ProcessPathAsync(context, paths[i], param, showHeader, i > 0, ct);
                if (result == PathCheckResult.NotFound)
                    hasError = true;
            }

            return hasError ? ExitCode.RuntimeError : ExitCode.Success;
        }

        private List<string> GetTargetPaths(CommandContext context)
        {
            if (context.PositionalArguments.Count > 0)
                return context.PositionalArguments.ToList();
            return new List<string> { "." };
        }

        private LsParams CreateParams()
        {
            return new LsParams(ShowAll, LongFormat, HumanReadable, Reverse, Recursive, SortBy);
        }

        /// <summary>
        /// 単一パスを処理します
        /// </summary>
        private async Task<PathCheckResult> ProcessPathAsync(
            CommandContext context,
            string path,
            LsParams param,
            bool showHeader,
            bool addBlankLine,
            CancellationToken ct)
        {
            var resolvedPath = PathUtility.ResolvePath(path, context.WorkingDirectory, context.HomeDirectory);
            var checkResult = await CheckPathAsync(context, resolvedPath, path, ct);

            if (checkResult != PathCheckResult.Directory)
                return checkResult;

            if (showHeader)
                await WriteHeaderAsync(context, path, addBlankLine, ct);

            try
            {
                await ListDirectoryAsync(context, resolvedPath, param, ct);

                if (param.Recursive)
                    await ListRecursiveAsync(context, resolvedPath, param, ct);
            }
            catch (UnauthorizedAccessException)
            {
                await context.Stderr.WriteLineAsync($"ls: {path}: Permission denied", ct);
                return PathCheckResult.NotFound;
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync($"ls: {path}: {ex.Message}", ct);
                return PathCheckResult.NotFound;
            }

            return PathCheckResult.Directory;
        }

        /// <summary>
        /// パスの存在とタイプを確認します
        /// </summary>
        private async Task<PathCheckResult> CheckPathAsync(
            CommandContext context,
            string resolvedPath,
            string displayPath,
            CancellationToken ct)
        {
            if (!Directory.Exists(resolvedPath) && !File.Exists(resolvedPath))
            {
                await context.Stderr.WriteLineAsync($"ls: {displayPath}: No such file or directory", ct);
                return PathCheckResult.NotFound;
            }

            if (File.Exists(resolvedPath))
            {
                var fileInfo = new FileInfo(resolvedPath);
                await PrintEntryAsync(context, fileInfo, ct);
                return PathCheckResult.FileProcessed;
            }

            return PathCheckResult.Directory;
        }

        private async Task WriteHeaderAsync(CommandContext context, string path, bool addBlankLine, CancellationToken ct)
        {
            if (addBlankLine)
                await context.Stdout.WriteLineAsync("", ct);
            await context.Stdout.WriteLineAsync($"{path}:", ct);
        }

        /// <summary>
        /// ディレクトリの内容を一覧表示します
        /// </summary>
        private async Task ListDirectoryAsync(CommandContext context, string directoryPath, LsParams param, CancellationToken ct)
        {
            var entries = param.GetSortedEntries(directoryPath);

            if (LongFormat)
            {
                foreach (var entry in entries)
                {
                    await PrintEntryAsync(context, entry, ct);
                }
                return;
            }

            var names = entries.Select(e => e is DirectoryInfo ? e.Name + "/" : e.Name).ToList();
            if (names.Count > 0)
                await context.Stdout.WriteLineAsync(string.Join("  ", names), ct);
        }

        /// <summary>
        /// 再帰的にサブディレクトリを表示します
        /// </summary>
        private async Task ListRecursiveAsync(CommandContext context, string directoryPath, LsParams param, CancellationToken ct)
        {
            var directories = param.GetSubDirectories(directoryPath);

            foreach (var subDir in directories)
            {
                ct.ThrowIfCancellationRequested();

                await context.Stdout.WriteLineAsync("", ct);
                await context.Stdout.WriteLineAsync($"{PathUtility.NormalizeToSlash(subDir)}:", ct);

                try
                {
                    await ListDirectoryAsync(context, subDir, param, ct);
                    await ListRecursiveAsync(context, subDir, param, ct);
                }
                catch (UnauthorizedAccessException)
                {
                    await context.Stderr.WriteLineAsync($"ls: {subDir}: Permission denied", ct);
                }
            }
        }

        /// <summary>
        /// エントリを詳細形式で出力します
        /// </summary>
        private async Task PrintEntryAsync(CommandContext context, FileSystemInfo entry, CancellationToken ct)
        {
            var name = GetDisplayName(entry);

            if (!LongFormat)
            {
                await context.Stdout.WriteLineAsync(name, ct);
                return;
            }

            var permissions = GetPermissionString(entry);
            var linkCount = entry is DirectoryInfo ? 2 : 1;
            var size = entry is FileInfo fileInfo ? fileInfo.Length : 0;
            var sizeStr = HumanReadable ? FormatSize(size) : size.ToString();
            var dateStr = entry.LastWriteTime.ToString("yyyy-MM-dd HH:mm");

            await context.Stdout.WriteLineAsync($"{permissions}  {linkCount}  {sizeStr,8}  {dateStr}  {name}", ct);
        }

        private string GetDisplayName(FileSystemInfo entry)
        {
            if (entry is DirectoryInfo)
                return entry.Name + "/";
            return entry.Name;
        }

        /// <summary>
        /// パーミッション文字列を取得します
        /// </summary>
        private string GetPermissionString(FileSystemInfo entry)
        {
            var isDir = entry is DirectoryInfo;
            var typeChar = isDir ? 'd' : '-';
            bool canWrite = !entry.Attributes.HasFlag(FileAttributes.ReadOnly);
            bool canExecute = isDir;

            var ownerPerms = $"r{(canWrite ? 'w' : '-')}{(canExecute ? 'x' : '-')}";

            return $"{typeChar}{ownerPerms}{ownerPerms}{ownerPerms}";
        }

        /// <summary>
        /// ファイルサイズを人間が読みやすい形式にフォーマットします
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

            if (unitIndex == 0)
                return $"{size:F0}{units[unitIndex]}";
            return $"{size:F1}{units[unitIndex]}";
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            yield break;
        }
    }
}
