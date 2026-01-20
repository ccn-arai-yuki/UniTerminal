using System.Collections.Generic;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// Normal形式のハンク。
    /// </summary>
    public class DiffHunk
    {
        public int OldStart { get; }
        public int NewStart { get; }
        public List<string> DeletedLines { get; } = new List<string>();
        public List<string> AddedLines { get; } = new List<string>();
        public int DeleteCount => DeletedLines.Count;
        public int AddCount => AddedLines.Count;

        public DiffHunk(int oldStart, int newStart)
        {
            OldStart = oldStart;
            NewStart = newStart;
        }
    }
}