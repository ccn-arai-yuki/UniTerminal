using System.Collections.Generic;

namespace Xeon.UniTerminal.Parsing
{
    /// <summary>
    /// 標準出力のリダイレクトモード。
    /// </summary>
    public enum RedirectMode
    {
        None,
        Overwrite,
        Append
    }

    /// <summary>
    /// コマンドのパースされたリダイレクション。
    /// </summary>
    public class ParsedRedirections
    {
        /// <summary>
        /// 標準入力リダイレクションのパス (< file)。指定されていない場合はnull。
        /// </summary>
        public string StdinPath { get; set; }

        /// <summary>
        /// 標準出力リダイレクションのパス (> または >> file)。指定されていない場合はnull。
        /// </summary>
        public string StdoutPath { get; set; }

        /// <summary>
        /// 標準出力リダイレクションのモード。
        /// </summary>
        public RedirectMode StdoutMode { get; set; } = RedirectMode.None;
    }

    /// <summary>
    /// パースされたオプションの出現（まだ型にバインドされていない）。
    /// </summary>
    public class ParsedOptionOccurrence
    {
        /// <summary>
        /// オプション名（-または--なし）。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// ロングオプション(--)かどうか。
        /// </summary>
        public bool IsLong { get; }

        /// <summary>
        /// 生の値（値が指定されていない場合はnull、--opt=の場合は空文字列）。
        /// </summary>
        public string RawValue { get; }

        /// <summary>
        /// 値が明示的に指定されたかどうか（空でも）。
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// 値がクォートされていたかどうか。
        /// </summary>
        public bool WasQuoted { get; }

        public ParsedOptionOccurrence(string name, bool isLong, string rawValue = null, bool hasValue = false, bool wasQuoted = false)
        {
            Name = name;
            IsLong = isLong;
            RawValue = rawValue;
            HasValue = hasValue;
            WasQuoted = wasQuoted;
        }

        public override string ToString()
        {
            var prefix = IsLong ? "--" : "-";
            if (HasValue)
                return $"{prefix}{Name}={RawValue ?? ""}";
            return $"{prefix}{Name}";
        }
    }

    /// <summary>
    /// パースされたコマンド（パイプライン内）。
    /// </summary>
    public class ParsedCommand
    {
        /// <summary>
        /// コマンド名。
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// 位置引数（オプション以外の引数）。
        /// </summary>
        public List<string> PositionalArguments { get; } = new List<string>();

        /// <summary>
        /// パースされたオプションの出現。
        /// </summary>
        public List<ParsedOptionOccurrence> Options { get; } = new List<ParsedOptionOccurrence>();

        /// <summary>
        /// このコマンドのリダイレクション。
        /// </summary>
        public ParsedRedirections Redirections { get; } = new ParsedRedirections();
    }

    /// <summary>
    /// パースされたパイプライン（パイプで接続された1つ以上のコマンド）。
    /// </summary>
    public class ParsedPipeline
    {
        /// <summary>
        /// パイプライン内のコマンド（順序通り）。
        /// </summary>
        public List<ParsedCommand> Commands { get; } = new List<ParsedCommand>();
    }

    /// <summary>
    /// トップレベルのパースされた入力構造。
    /// </summary>
    public class ParsedInput
    {
        /// <summary>
        /// 実行するパイプライン。
        /// </summary>
        public ParsedPipeline Pipeline { get; set; }

        /// <summary>
        /// 入力が空かどうか。
        /// </summary>
        public bool IsEmpty => Pipeline == null || Pipeline.Commands.Count == 0;
    }
}
