namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// パス確認の結果を表す列挙型
    /// </summary>
    public enum PathCheckResult
    {
        /// <summary>
        /// パスは有効なディレクトリ。処理を継続する
        /// </summary>
        Directory,

        /// <summary>
        /// パスは有効なファイル。ファイル情報を出力済み
        /// </summary>
        FileProcessed,

        /// <summary>
        /// パスが存在しない。エラーを出力済み
        /// </summary>
        NotFound
    }
}
