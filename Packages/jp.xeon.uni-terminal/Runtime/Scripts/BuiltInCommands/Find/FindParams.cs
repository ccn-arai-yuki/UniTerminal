using System.Text.RegularExpressions;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// findコマンドの検索条件
    /// </summary>
    public readonly struct FindParams
    {
        /// <summary>
        /// 検索パターン
        /// </summary>
        public string pattern { get; }
        private readonly bool ignoreCase;
        private readonly FindFileType fileType;

        /// <summary>
        /// 検索条件を初期化します
        /// </summary>
        /// <param name="pattern">検索パターン</param>
        /// <param name="ignoreCase">大文字小文字を無視するかどうか</param>
        /// <param name="fileType">検索対象のファイル種別</param>
        public FindParams(string pattern, bool ignoreCase, FindFileType fileType)
        {
            this.pattern = pattern;
            this.ignoreCase = ignoreCase;
            this.fileType = fileType;
        }

        /// <summary>
        /// ファイル種別が条件に合致するか判定します
        /// </summary>
        /// <param name="isFile">ファイルかどうか</param>
        /// <param name="isDirectory">ディレクトリかどうか</param>
        /// <returns>合致する場合はtrue</returns>
        public bool MatchType(bool isFile, bool isDirectory)
        {
            if (fileType is FindFileType.All)
                return true;
            if (fileType is FindFileType.File && isFile)
                return true;
            if (fileType is FindFileType.Directory && isDirectory)
                return true;
            return false;
        }

        /// <summary>
        /// 正規表現オプション
        /// </summary>
        public RegexOptions RegexOption => ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
    }
}
