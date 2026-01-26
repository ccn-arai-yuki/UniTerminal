using System.Collections.Generic;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// Normal形式のハンク。
    /// </summary>
    public class DiffHunk
    {
        /// <summary>
        /// 旧ファイル側の開始行。
        /// </summary>
        public int OldStart { get; }

        /// <summary>
        /// 新ファイル側の開始行。
        /// </summary>
        public int NewStart { get; }

        /// <summary>
        /// 削除された行の一覧。
        /// </summary>
        public List<string> DeletedLines { get; } = new List<string>();

        /// <summary>
        /// 追加された行の一覧。
        /// </summary>
        public List<string> AddedLines { get; } = new List<string>();

        /// <summary>
        /// 削除行数。
        /// </summary>
        public int DeleteCount => DeletedLines.Count;

        /// <summary>
        /// 追加行数。
        /// </summary>
        public int AddCount => AddedLines.Count;

        /// <summary>
        /// Normalハンクを初期化します。
        /// </summary>
        /// <param name="oldStart">旧ファイル側の開始行。</param>
        /// <param name="newStart">新ファイル側の開始行。</param>
        public DiffHunk(int oldStart, int newStart)
        {
            OldStart = oldStart;
            NewStart = newStart;
        }
    }
}
