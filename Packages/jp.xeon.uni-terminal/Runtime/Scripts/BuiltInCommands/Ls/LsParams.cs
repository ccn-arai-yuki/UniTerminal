using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// lsコマンドの表示パラメータを保持する構造体。
    /// </summary>
    public readonly struct LsParams
    {
        public bool ShowAll { get; }
        public bool LongFormat { get; }
        public bool HumanReadable { get; }
        public bool Reverse { get; }
        public bool Recursive { get; }
        public LsSortType SortBy { get; }

        public LsParams(bool showAll, bool longFormat, bool humanReadable, bool reverse, bool recursive, LsSortType sortBy)
        {
            ShowAll = showAll;
            LongFormat = longFormat;
            HumanReadable = humanReadable;
            Reverse = reverse;
            Recursive = recursive;
            SortBy = sortBy;
        }

        /// <summary>
        /// ソートされたエントリを取得します。
        /// </summary>
        public List<FileSystemInfo> GetSortedEntries(string directoryPath)
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            var showAll = ShowAll;
            var entries = dirInfo.GetFileSystemInfos()
                .Where(e => showAll || !e.Name.StartsWith("."));

            IOrderedEnumerable<FileSystemInfo> sorted = SortBy switch
            {
                LsSortType.Size => entries.OrderByDescending(e => GetSize(e)),
                LsSortType.Time => entries.OrderByDescending(e => e.LastWriteTime),
                _ => entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            };

            var result = sorted.ToList();

            if (Reverse)
                result.Reverse();

            return result;
        }

        /// <summary>
        /// 隠しファイルを除外した子ディレクトリを取得します。
        /// </summary>
        public IEnumerable<string> GetSubDirectories(string directoryPath)
        {
            var showAll = ShowAll;
            return Directory.GetDirectories(directoryPath)
                .Where(d => showAll || !Path.GetFileName(d).StartsWith("."))
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase);
        }

        private static long GetSize(FileSystemInfo entry)
        {
            if (entry is FileInfo fileInfo)
                return fileInfo.Length;
            return 0;
        }
    }
}
