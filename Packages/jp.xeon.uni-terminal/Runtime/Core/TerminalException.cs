using System;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// ターミナルエラーの基底例外クラス。
    /// </summary>
    public abstract class TerminalException : Exception
    {
        public ExitCode ExitCode { get; }

        protected TerminalException(string message, ExitCode exitCode) : base(message)
        {
            ExitCode = exitCode;
        }

        protected TerminalException(string message, ExitCode exitCode, Exception innerException)
            : base(message, innerException)
        {
            ExitCode = exitCode;
        }
    }

    /// <summary>
    /// パースエラー（構文エラー、閉じられていないクォート等）。
    /// 終了コード: 2
    /// </summary>
    public class ParseException : TerminalException
    {
        public ParseException(string message) : base(message, ExitCode.UsageError) { }
        public ParseException(string message, Exception innerException)
            : base(message, ExitCode.UsageError, innerException) { }
    }

    /// <summary>
    /// バインドエラー（不明なオプション、必須オプション不足、型変換失敗等）。
    /// 終了コード: 2
    /// </summary>
    public class BindException : TerminalException
    {
        /// <summary>
        /// エラーを発生させたコマンド名（ヘルプ表示用）。
        /// </summary>
        public string CommandName { get; }

        public BindException(string message, string commandName = null)
            : base(message, ExitCode.UsageError)
        {
            CommandName = commandName;
        }

        public BindException(string message, string commandName, Exception innerException)
            : base(message, ExitCode.UsageError, innerException)
        {
            CommandName = commandName;
        }
    }

    /// <summary>
    /// 実行時エラー（I/O障害、コマンド実行エラー等）。
    /// 終了コード: 1
    /// </summary>
    public class RuntimeException : TerminalException
    {
        public RuntimeException(string message) : base(message, ExitCode.RuntimeError) { }
        public RuntimeException(string message, Exception innerException)
            : base(message, ExitCode.RuntimeError, innerException) { }
    }
}
