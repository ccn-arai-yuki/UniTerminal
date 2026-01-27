namespace Xeon.UniTerminal
{
    /// <summary>
    /// 標準終了コード
    /// </summary>
    public enum ExitCode
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success = 0,

        /// <summary>
        /// 実行時エラー
        /// </summary>
        RuntimeError = 1,

        /// <summary>
        /// 使用法エラー（パースエラー、バインドエラー、不明なオプション等）
        /// </summary>
        UsageError = 2
    }
}
