namespace Xeon.UniTerminal.Execution
{
    /// <summary>
    /// パイプライン実行の結果
    /// </summary>
    public readonly struct ExecutionResult
    {
        /// <summary>
        /// パイプライン内の最後のコマンドからの終了コード
        /// </summary>
        public ExitCode ExitCode { get; }

        /// <summary>
        /// 実行が成功したかどうか（終了コード0）
        /// </summary>
        public bool Success => ExitCode == ExitCode.Success;

        /// <summary>
        /// 実行結果を初期化します
        /// </summary>
        /// <param name="exitCode">終了コード</param>
        public ExecutionResult(ExitCode exitCode)
        {
            ExitCode = exitCode;
        }

        /// <summary>
        /// 成功を表す実行結果
        /// </summary>
        public static readonly ExecutionResult Successful = new ExecutionResult(ExitCode.Success);
    }
}
