using System.IO;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// パス解決用ユーティリティ。
    /// </summary>
    public static class PathUtility
    {
        /// <summary>
        /// パスの区切り文字をスラッシュに正規化します。
        /// </summary>
        /// <param name="path">正規化するパス。</param>
        /// <returns>スラッシュで統一されたパス。</returns>
        public static string NormalizeToSlash(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            return path.Replace('\\', '/');
        }

        /// <summary>
        /// パスを結合し、スラッシュで正規化します。
        /// </summary>
        /// <param name="path1">最初のパス。</param>
        /// <param name="path2">結合するパス。</param>
        /// <returns>スラッシュで統一された結合パス。</returns>
        public static string Combine(string path1, string path2)
        {
            return NormalizeToSlash(Path.Combine(path1, path2));
        }

        /// <summary>
        /// パスの親ディレクトリを取得し、スラッシュで正規化します。
        /// </summary>
        /// <param name="path">パス。</param>
        /// <returns>スラッシュで統一された親ディレクトリパス。</returns>
        public static string GetDirectoryName(string path)
        {
            var dir = Path.GetDirectoryName(path);
            return dir != null ? NormalizeToSlash(dir) : null;
        }

        /// <summary>
        /// 作業ディレクトリを基準にパスを解決します（~展開付き）。
        /// </summary>
        /// <param name="path">解決するパス。</param>
        /// <param name="workingDirectory">現在の作業ディレクトリ。</param>
        /// <param name="homeDirectory">~展開用のホームディレクトリ。</param>
        /// <returns>解決された絶対パス（スラッシュで統一）。</returns>
        public static string ResolvePath(string path, string workingDirectory, string homeDirectory)
        {
            if (string.IsNullOrEmpty(path))
                return NormalizeToSlash(workingDirectory);

            // ~展開の処理
            if (path.StartsWith("~"))
            {
                if (path.Length == 1)
                {
                    return NormalizeToSlash(homeDirectory);
                }
                if (path[1] == '/' || path[1] == Path.DirectorySeparatorChar)
                {
                    path = Path.Combine(homeDirectory, path.Substring(2));
                }
                else
                {
                    // ~usernameはサポートされていないため、相対パスとして扱う
                    path = Path.Combine(workingDirectory, path);
                }
            }
            // 絶対パスの処理
            else if (Path.IsPathRooted(path))
            {
                // 既に絶対パス
            }
            // 相対パスの処理
            else
            {
                path = Path.Combine(workingDirectory, path);
            }

            // パスを正規化（..と.を解決し、スラッシュで統一）
            return NormalizeToSlash(Path.GetFullPath(path));
        }
    }
}
