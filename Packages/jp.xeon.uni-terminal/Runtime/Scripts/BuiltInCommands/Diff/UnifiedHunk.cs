using System.Collections.Generic;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// Unified形式のハンク。
    /// </summary>
    public class UnifiedHunk
    {
        /// <summary>
        /// 旧ファイル側の開始行。
        /// </summary>
        public int OldStart { get; }

        /// <summary>
        /// 旧ファイル側の行数。
        /// </summary>
        public int OldCount { get; }

        /// <summary>
        /// 新ファイル側の開始行。
        /// </summary>
        public int NewStart { get; }

        /// <summary>
        /// 新ファイル側の行数。
        /// </summary>
        public int NewCount { get; }

        /// <summary>
        /// ハンク内の差分行。
        /// </summary>
        public List<DiffLine> Lines { get; }

        /// <summary>
        /// Unifiedハンクを初期化します。
        /// </summary>
        /// <param name="oldStart">旧ファイル側の開始行。</param>
        /// <param name="oldCount">旧ファイル側の行数。</param>
        /// <param name="newStart">新ファイル側の開始行。</param>
        /// <param name="newCount">新ファイル側の行数。</param>
        /// <param name="lines">差分行。</param>
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
