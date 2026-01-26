namespace Xeon.UniTerminal.Completion
{
    /// <summary>
    /// 補完コンテキストの分析結果
    /// </summary>
    internal class CompletionAnalysis
    {
        /// <summary>
        /// 補完対象の種別
        /// </summary>
        public CompletionTarget Target { get; set; }

        /// <summary>
        /// 対象コマンド名
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// 現在のトークンインデックス
        /// </summary>
        public int TokenIndex { get; set; }
    }
}
