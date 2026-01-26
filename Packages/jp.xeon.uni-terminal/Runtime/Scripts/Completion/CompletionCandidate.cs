namespace Xeon.UniTerminal.Completion
{
    /// <summary>
    /// 補完候補。
    /// </summary>
    public class CompletionCandidate
    {
        /// <summary>
        /// 挿入する補完テキスト。
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// 表示テキスト（説明を含む場合あり）。
        /// </summary>
        public string DisplayText { get; }

        /// <summary>
        /// 補完の種別。
        /// </summary>
        public CompletionTarget Target { get; }

        /// <summary>
        /// 補完候補を初期化します。
        /// </summary>
        /// <param name="text">挿入する補完テキスト。</param>
        /// <param name="displayText">表示テキスト。</param>
        /// <param name="target">補完の種別。</param>
        public CompletionCandidate(string text, string displayText = null, CompletionTarget target = CompletionTarget.Argument)
        {
            Text = text;
            DisplayText = displayText ?? text;
            Target = target;
        }
    }
}
