using System.Collections.Generic;

namespace Xeon.UniTerminal.Parsing
{
    /// <summary>
    /// 標準出力のリダイレクトモード
    /// </summary>
    public enum RedirectMode
    {
        None,
        Overwrite,
        Append
    }

    /// <summary>
    /// コマンドのパースされたリダイレクション
    /// </summary>
    public readonly struct ParsedRedirections
    {
        /// <summary>
        /// 標準入力リダイレクションのパス (< file)。指定されていない場合はnull
        /// </summary>
        public string StdinPath { get; }

        /// <summary>
        /// 標準出力リダイレクションのパス (> または >> file)。指定されていない場合はnull
        /// </summary>
        public string StdoutPath { get; }

        /// <summary>
        /// 標準出力リダイレクションのモード
        /// </summary>
        public RedirectMode StdoutMode { get; }

        /// <summary>
        /// リダイレクション情報を初期化します
        /// </summary>
        /// <param name="stdinPath">標準入力リダイレクトのパス</param>
        /// <param name="stdoutPath">標準出力リダイレクトのパス</param>
        /// <param name="stdoutMode">標準出力のリダイレクトモード</param>
        public ParsedRedirections(string stdinPath, string stdoutPath, RedirectMode stdoutMode)
        {
            StdinPath = stdinPath;
            StdoutPath = stdoutPath;
            StdoutMode = stdoutMode;
        }
    }

    /// <summary>
    /// パースされたオプションの出現（まだ型にバインドされていない）
    /// </summary>
    public readonly struct ParsedOptionOccurrence
    {
        /// <summary>
        /// オプション名（-または--なし）
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// ロングオプション(--)かどうか
        /// </summary>
        public bool IsLong { get; }

        /// <summary>
        /// 生の値（値が指定されていない場合はnull、--opt=の場合は空文字列）
        /// </summary>
        public string RawValue { get; }

        /// <summary>
        /// 値が明示的に指定されたかどうか（空でも）
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// 値がクォートされていたかどうか
        /// </summary>
        public bool WasQuoted { get; }

        /// <summary>
        /// 値がスペース区切りで解析されたかどうか（=構文ではなく）
        /// </summary>
        public bool IsValueSpaceSeparated { get; }

        /// <summary>
        /// パース済みオプション情報を初期化します
        /// </summary>
        /// <param name="name">オプション名</param>
        /// <param name="isLong">ロングオプションかどうか</param>
        /// <param name="rawValue">生の値</param>
        /// <param name="hasValue">値が指定されたかどうか</param>
        /// <param name="wasQuoted">値がクォートされていたかどうか</param>
        /// <param name="isValueSpaceSeparated">値がスペース区切りで解析されたかどうか</param>
        public ParsedOptionOccurrence(string name, bool isLong, string rawValue = null, bool hasValue = false, bool wasQuoted = false, bool isValueSpaceSeparated = false)
        {
            Name = name;
            IsLong = isLong;
            RawValue = rawValue;
            HasValue = hasValue;
            WasQuoted = wasQuoted;
            IsValueSpaceSeparated = isValueSpaceSeparated;
        }

        /// <summary>
        /// 表示用の文字列を生成します
        /// </summary>
        public override string ToString()
        {
            var prefix = IsLong ? "--" : "-";
            if (HasValue)
                return $"{prefix}{Name}={RawValue ?? ""}";
            return $"{prefix}{Name}";
        }
    }

    /// <summary>
    /// パースされたコマンド（パイプライン内）
    /// </summary>
    public class ParsedCommand
    {
        /// <summary>
        /// コマンド名
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// 位置引数（オプション以外の引数）
        /// </summary>
        public List<string> PositionalArguments { get; } = new List<string>();

        /// <summary>
        /// パースされたオプションの出現
        /// </summary>
        public List<ParsedOptionOccurrence> Options { get; } = new List<ParsedOptionOccurrence>();

        /// <summary>
        /// このコマンドのリダイレクション
        /// </summary>
        public ParsedRedirections Redirections { get; set; }
    }

    /// <summary>
    /// パースされたパイプライン（パイプで接続された1つ以上のコマンド）
    /// </summary>
    public class ParsedPipeline
    {
        /// <summary>
        /// パイプライン内のコマンド（順序通り）
        /// </summary>
        public List<ParsedCommand> Commands { get; } = new List<ParsedCommand>();
    }

    /// <summary>
    /// トップレベルのパースされた入力構造
    /// </summary>
    public class ParsedInput
    {
        /// <summary>
        /// 実行するパイプライン
        /// </summary>
        public ParsedPipeline Pipeline { get; set; }

        /// <summary>
        /// 入力が空かどうか
        /// </summary>
        public bool IsEmpty => Pipeline == null || Pipeline.Commands.Count == 0;
    }
}
