using System.Collections.Generic;

namespace Xeon.UniTerminal.Completion
{
    /// <summary>
    /// 補完結果。
    /// </summary>
    public class CompletionResult
    {
        /// <summary>
        /// 補完候補。
        /// </summary>
        public IReadOnlyList<CompletionCandidate> Candidates { get; }

        /// <summary>
        /// 補完中のトークンの開始位置。
        /// </summary>
        public int TokenStart { get; }

        /// <summary>
        /// 補完中のトークンの長さ。
        /// </summary>
        public int TokenLength { get; }

        public CompletionResult(List<CompletionCandidate> candidates, int tokenStart, int tokenLength)
        {
            Candidates = candidates;
            TokenStart = tokenStart;
            TokenLength = tokenLength;
        }

        public static CompletionResult Empty => new CompletionResult(new List<CompletionCandidate>(), 0, 0);
    }
}
