using System.Collections.Generic;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// Unified形式のハンク。
    /// </summary>
    public class UnifiedHunk
    {
        public int OldStart { get; }
        public int OldCount { get; }
        public int NewStart { get; }
        public int NewCount { get; }
        public List<DiffLine> Lines { get; }

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