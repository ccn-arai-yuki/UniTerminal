using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// ファイルタイプ（findコマンド用）
    /// </summary>
    public enum FindFileType
    {
        All,
        File,
        Directory
    }

    /// <summary>
    /// ディレクトリ階層内でファイルを検索するコマンド
    /// </summary>
    [Command("find", "Search for files in a directory hierarchy")]
    public class FindCommand : ICommand
    {
        /// <summary>
        /// ファイル名の検索パターン
        /// </summary>
        [Option("name", "n", Description = "File name pattern (supports wildcards)")]
        public string NamePattern;

        /// <summary>
        /// 大文字小文字を無視する検索パターン
        /// </summary>
        [Option("iname", "i", Description = "Case-insensitive file name pattern")]
        public string INamePattern;

        /// <summary>
        /// 検索対象のファイル種別
        /// </summary>
        [Option("type", "t", Description = "File type: f (file), d (directory)")]
        public string FileType;

        /// <summary>
        /// 探索の最大深さ
        /// </summary>
        [Option("maxdepth", "d", Description = "Maximum search depth (-1 = unlimited)")]
        public int MaxDepth = -1;

        /// <summary>
        /// 探索の最小深さ
        /// </summary>
        [Option("mindepth", "", Description = "Minimum search depth")]
        public int MinDepth = 0;

        /// <summary>
        /// コマンド名
        /// </summary>
        public string CommandName => "find";

        /// <summary>
        /// コマンドの説明
        /// </summary>
        public string Description => "Search for files in a directory hierarchy";

        /// <summary>
        /// コマンドを実行します
        /// </summary>
        /// <param name="context">実行コンテキスト</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>終了コード</returns>
        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            try
            {
                // 検索開始パスを取得
                var searchPaths = new List<string>();
                if (context.PositionalArguments.Count == 0)
                {
                    searchPaths.Add(context.WorkingDirectory);
                }
                else
                {
                    foreach (var arg in context.PositionalArguments)
                    {
                        var resolvedPath = PathUtility.ResolvePath(arg, context.WorkingDirectory, context.HomeDirectory);
                        searchPaths.Add(resolvedPath);
                    }
                }
                
                // ファイルタイプを解析
                var fileType = ParseFileType(FileType);

                // パターンを準備
                string pattern = NamePattern ?? INamePattern;
                bool ignoreCase = INamePattern != null;

                var param = new FindParams(pattern, ignoreCase, fileType);

                // 各パスを検索
                bool hasError = false;
                foreach (var searchPath in searchPaths)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!Directory.Exists(searchPath) && !File.Exists(searchPath))
                    {
                        await context.Stderr.WriteLineAsync($"find: {searchPath}: No such file or directory", ct);
                        hasError = true;
                        continue;
                    }

                    // 検索を実行
                    await foreach (var result in SearchAsync(searchPath, searchPath, 0, param, ct))
                    {
                        await context.Stdout.WriteLineAsync(result, ct);
                    }
                }

                return hasError && searchPaths.Count == 1 ? ExitCode.RuntimeError : ExitCode.Success;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync($"find: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
        }

        /// <summary>
        /// ファイルタイプを解析します
        /// </summary>
        private FindFileType ParseFileType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return FindFileType.All;

            switch (type.ToLowerInvariant())
            {
                case "f":
                case "file":
                    return FindFileType.File;
                case "d":
                case "dir":
                case "directory":
                    return FindFileType.Directory;
                default:
                    return FindFileType.All;
            }
        }

        /// <summary>
        /// ディレクトリを再帰的に検索します
        /// </summary>
        private async IAsyncEnumerable<string> SearchAsync(
            string basePath,
            string currentPath,
            int currentDepth,
            FindParams param,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            // 最大深度チェック
            if (MaxDepth >= 0 && currentDepth > MaxDepth)
                yield break;

            ct.ThrowIfCancellationRequested();

            bool isDirectory = Directory.Exists(currentPath);
            bool isFile = !isDirectory && File.Exists(currentPath);

            // 現在のパスがマッチするか確認
            if (currentDepth >= MinDepth)
            {
                bool typeMatches = param.MatchType(isFile, isDirectory);

                if (typeMatches)
                {
                    string name = Path.GetFileName(currentPath);
                    bool nameMatches = string.IsNullOrEmpty(param.pattern) || MatchWildcard(name, param);

                    if (nameMatches)
                    {
                        yield return GetRelativePath(basePath, currentPath);
                    }
                }
            }

            // ディレクトリの場合、サブエントリを検索
            if (!isDirectory)
                yield break;
            
            IEnumerable<string> entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(currentPath);
            }
            catch (UnauthorizedAccessException)
            {
                // アクセス権限がない場合はスキップ（エラーは出さない）
                yield break;
            }
            catch (DirectoryNotFoundException)
            {
                yield break;
            }

            foreach (var entry in entries)
            {
                await foreach (var result in SearchAsync(basePath, entry, currentDepth + 1, param, ct))
                {
                    yield return result;
                }
            }
        }

        /// <summary>
        /// ワイルドカードパターンとマッチするか確認します
        /// </summary>
        private bool MatchWildcard(string name, FindParams param)
        {
            // パターンを正規表現に変換
            var regexPattern = "^" + Regex.Escape(param.pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".")
                .Replace("\\[", "[")
                .Replace("\\]", "]") + "$";
            
            return Regex.IsMatch(name, regexPattern, param.RegexOption);
        }

        /// <summary>
        /// ベースパスからの相対パスを取得します
        /// </summary>
        private string GetRelativePath(string basePath, string fullPath)
        {
            // ベースパスがファイルの場合、そのファイル名を返す
            if (File.Exists(basePath))
            {
                return Path.GetFileName(fullPath);
            }

            // パスをスラッシュで正規化
            var normalizedBase = PathUtility.NormalizeToSlash(basePath);
            var normalizedFull = PathUtility.NormalizeToSlash(fullPath);

            // ベースパスと同じ場合
            if (normalizedBase == normalizedFull)
            {
                return ".";
            }

            // 相対パスを計算
            if (normalizedFull.StartsWith(normalizedBase, StringComparison.Ordinal))
            {
                var relative = normalizedFull.Substring(normalizedBase.Length).TrimStart('/');
                return "./" + relative;
            }

            return normalizedFull;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // パス補完はCompletionEngineで処理
            yield break;
        }
    }
}
