namespace Xeon.UniTerminal.Execution
{
    /// <summary>
    /// パイプライン実行の結果。
    /// </summary>
    public class ExecutionResult
    {
        /// <summary>
        /// パイプライン内の最後のコマンドからの終了コード。
        /// </summary>
        public ExitCode ExitCode { get; }

        /// <summary>
        /// 実行が成功したかどうか（終了コード0）。
        /// </summary>
        public bool Success => ExitCode == ExitCode.Success;

        public ExecutionResult(ExitCode exitCode)
        {
            ExitCode = exitCode;
        }

        public static ExecutionResult Successful => new ExecutionResult(ExitCode.Success);
    }
}
