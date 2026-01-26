using System;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// ターミナルエラーの基底例外クラス
    /// </summary>
    public abstract class TerminalException : Exception
    {
        /// <summary>
        /// 対応する終了コード
        /// </summary>
        public ExitCode ExitCode { get; }

        /// <summary>
        /// ターミナル例外を初期化します
        /// </summary>
        /// <param name="message">例外メッセージ</param>
        /// <param name="exitCode">終了コード</param>
        protected TerminalException(string message, ExitCode exitCode) : base(message)
        {
            ExitCode = exitCode;
        }

        /// <summary>
        /// 内部例外を伴うターミナル例外を初期化します
        /// </summary>
        /// <param name="message">例外メッセージ</param>
        /// <param name="exitCode">終了コード</param>
        /// <param name="innerException">内部例外</param>
        protected TerminalException(string message, ExitCode exitCode, Exception innerException)
            : base(message, innerException)
        {
            ExitCode = exitCode;
        }
    }

    /// <summary>
    /// パースエラー（構文エラー、閉じられていないクォート等）
    /// 終了コード: 2
    /// </summary>
    public class ParseException : TerminalException
    {
        /// <summary>
        /// パースエラーを生成します
        /// </summary>
        /// <param name="message">例外メッセージ</param>
        public ParseException(string message) : base(message, ExitCode.UsageError) { }

        /// <summary>
        /// 内部例外を伴うパースエラーを生成します
        /// </summary>
        /// <param name="message">例外メッセージ</param>
        /// <param name="innerException">内部例外</param>
        public ParseException(string message, Exception innerException)
            : base(message, ExitCode.UsageError, innerException) { }
    }

    /// <summary>
    /// バインドエラー（不明なオプション、必須オプション不足、型変換失敗等）
    /// 終了コード: 2
    /// </summary>
    public class BindException : TerminalException
    {
        /// <summary>
        /// エラーを発生させたコマンド名（ヘルプ表示用）
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// バインドエラーを生成します
        /// </summary>
        /// <param name="message">例外メッセージ</param>
        /// <param name="commandName">エラー元のコマンド名</param>
        public BindException(string message, string commandName = null)
            : base(message, ExitCode.UsageError)
        {
            CommandName = commandName;
        }

        /// <summary>
        /// 内部例外を伴うバインドエラーを生成します
        /// </summary>
        /// <param name="message">例外メッセージ</param>
        /// <param name="commandName">エラー元のコマンド名</param>
        /// <param name="innerException">内部例外</param>
        public BindException(string message, string commandName, Exception innerException)
            : base(message, ExitCode.UsageError, innerException)
        {
            CommandName = commandName;
        }
    }

    /// <summary>
    /// 実行時エラー（I/O障害、コマンド実行エラー等）
    /// 終了コード: 1
    /// </summary>
    public class RuntimeException : TerminalException
    {
        /// <summary>
        /// 実行時エラーを生成します
        /// </summary>
        /// <param name="message">例外メッセージ</param>
        public RuntimeException(string message) : base(message, ExitCode.RuntimeError) { }

        /// <summary>
        /// 内部例外を伴う実行時エラーを生成します
        /// </summary>
        /// <param name="message">例外メッセージ</param>
        /// <param name="innerException">内部例外</param>
        public RuntimeException(string message, Exception innerException)
            : base(message, ExitCode.RuntimeError, innerException) { }
    }
}
