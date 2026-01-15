using System.IO;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// パス解決用ユーティリティ。
    /// </summary>
    public static class PathUtility
    {
        /// <summary>
        /// 作業ディレクトリを基準にパスを解決します（~展開付き）。
        /// </summary>
        /// <param name="path">解決するパス。</param>
        /// <param name="workingDirectory">現在の作業ディレクトリ。</param>
        /// <param name="homeDirectory">~展開用のホームディレクトリ。</param>
        /// <returns>解決された絶対パス。</returns>
        public static string ResolvePath(string path, string workingDirectory, string homeDirectory)
        {
            if (string.IsNullOrEmpty(path))
                return workingDirectory;

            // ~展開の処理
            if (path.StartsWith("~"))
            {
                if (path.Length == 1)
                {
                    return homeDirectory;
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

            // パスを正規化（..と.を解決）
            return Path.GetFullPath(path);
        }
    }
}
