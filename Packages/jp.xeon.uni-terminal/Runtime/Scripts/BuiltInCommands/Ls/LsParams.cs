using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// lsコマンドの表示パラメータを保持する構造体
    /// </summary>
    public readonly struct LsParams
    {
        /// <summary>
        /// 隠しファイルを表示するかどうか
        /// </summary>
        public bool ShowAll { get; }

        /// <summary>
        /// 詳細表示を行うかどうか
        /// </summary>
        public bool LongFormat { get; }

        /// <summary>
        /// サイズを可読形式で表示するかどうか
        /// </summary>
        public bool HumanReadable { get; }

        /// <summary>
        /// 逆順表示するかどうか
        /// </summary>
        public bool Reverse { get; }

        /// <summary>
        /// 再帰的に表示するかどうか
        /// </summary>
        public bool Recursive { get; }

        /// <summary>
        /// ソート方法
        /// </summary>
        public LsSortType SortBy { get; }

        /// <summary>
        /// lsの表示設定を初期化します
        /// </summary>
        /// <param name="showAll">隠しファイルを表示するかどうか</param>
        /// <param name="longFormat">詳細表示するかどうか</param>
        /// <param name="humanReadable">サイズを可読形式で表示するかどうか</param>
        /// <param name="reverse">逆順表示するかどうか</param>
        /// <param name="recursive">再帰的に表示するかどうか</param>
        /// <param name="sortBy">ソート方法</param>
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
        /// ソートされたエントリを取得します
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
        /// 隠しファイルを除外した子ディレクトリを取得します
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
