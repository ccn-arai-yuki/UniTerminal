namespace Xeon.UniTerminal
{
    /// <summary>
    /// ターミナル出力のデータを表す構造体
    /// </summary>
    public readonly struct OutputData
    {
        /// <summary>
        /// 出力メッセージ
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// エラー出力かどうか
        /// </summary>
        public bool IsError { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="message">出力メッセージ</param>
        /// <param name="isError">エラー出力かどうか</param>
        public OutputData(string message, bool isError)
        {
            Message = message;
            IsError = isError;
        }
    }
}