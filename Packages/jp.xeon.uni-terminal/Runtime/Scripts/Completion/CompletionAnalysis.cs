namespace Xeon.UniTerminal.Completion
{
    /// <summary>
    /// 補完コンテキストの分析結果。
    /// </summary>
    internal class CompletionAnalysis
    {
        public CompletionTarget Target { get; set; }
        public string CommandName { get; set; }
        public int TokenIndex { get; set; }
    }
}
