namespace Xeon.UniTerminal
{
    /// <summary>
    /// タブ補完時に提供されるコンテキスト。
    /// </summary>
    public readonly struct CompletionContext
    {
        /// <summary>
        /// 入力行全体。
        /// </summary>
        public string InputLine { get; }

        /// <summary>
        /// 補完中の現在のトークン。
        /// </summary>
        public string CurrentToken { get; }

        /// <summary>
        /// トークンリスト内の現在のトークンのインデックス。
        /// </summary>
        public int TokenIndex { get; }

        /// <summary>
        /// 現在の作業ディレクトリ。
        /// </summary>
        public string WorkingDirectory { get; }

        /// <summary>
        /// ホームディレクトリ。
        /// </summary>
        public string HomeDirectory { get; }

        public CompletionContext(
            string inputLine,
            string currentToken,
            int tokenIndex,
            string workingDirectory,
            string homeDirectory)
        {
            InputLine = inputLine;
            CurrentToken = currentToken;
            TokenIndex = tokenIndex;
            WorkingDirectory = workingDirectory;
            HomeDirectory = homeDirectory;
        }
    }
}
