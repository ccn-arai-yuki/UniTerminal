namespace Xeon.UniTerminal.Parsing
{
    /// <summary>
    /// CLIトークナイザーのトークン種別。
    /// </summary>
    public enum TokenKind
    {
        /// <summary>
        /// ワードトークン（コマンド、引数、オプション等）。
        /// </summary>
        Word,

        /// <summary>
        /// パイプ演算子: |
        /// </summary>
        Pipe,

        /// <summary>
        /// 標準入力リダイレクト: <
        /// </summary>
        RedirectIn,

        /// <summary>
        /// 標準出力上書き: >
        /// </summary>
        RedirectOut,

        /// <summary>
        /// 標準出力追記: >>
        /// </summary>
        RedirectAppend,

        /// <summary>
        /// オプション終端マーカー: --
        /// </summary>
        EndOfOptions
    }

    /// <summary>
    /// ソース入力内の位置範囲を表します。
    /// </summary>
    public readonly struct SourceSpan
    {
        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;

        public SourceSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public override string ToString() => $"[{Start}..{End})";
    }

    /// <summary>
    /// 入力からのトークンを表します。
    /// </summary>
    public readonly struct Token
    {
        /// <summary>
        /// トークンの種別。
        /// </summary>
        public TokenKind Kind { get; }

        /// <summary>
        /// トークンの値（Wordトークンの場合は処理済みテキスト）。
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// 元の入力内でのトークンのソース範囲。
        /// </summary>
        public SourceSpan Span { get; }

        /// <summary>
        /// このトークンがクォートされていたかどうか。
        /// </summary>
        public bool WasQuoted { get; }

        public Token(TokenKind kind, string value, SourceSpan span, bool wasQuoted = false)
        {
            Kind = kind;
            Value = value;
            Span = span;
            WasQuoted = wasQuoted;
        }

        public override string ToString() => $"{Kind}: \"{Value}\" {Span}";
    }
}
