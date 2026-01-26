using System;
using System.Collections.Generic;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// コマンド実行時に提供されるコンテキスト
    /// stdin/stdout/stderrストリーム、作業ディレクトリ、位置引数を含みます
    /// </summary>
    public class CommandContext
    {
        /// <summary>
        /// 標準入力リーダー（行単位）
        /// </summary>
        public IAsyncTextReader Stdin { get; }

        /// <summary>
        /// 標準出力ライター
        /// </summary>
        public IAsyncTextWriter Stdout { get; }

        /// <summary>
        /// 標準エラー出力ライター
        /// </summary>
        public IAsyncTextWriter Stderr { get; }

        /// <summary>
        /// 現在の作業ディレクトリ
        /// </summary>
        public string WorkingDirectory { get; }

        /// <summary>
        /// 前の作業ディレクトリ（cd - で使用）
        /// </summary>
        public string PreviousWorkingDirectory { get; }

        /// <summary>
        /// ホームディレクトリ（Application.persistentDataPath）
        /// </summary>
        public string HomeDirectory { get; }

        /// <summary>
        /// 作業ディレクトリを変更するコールバック（cdコマンド専用）
        /// </summary>
        public Action<string> ChangeWorkingDirectory { get; }

        /// <summary>
        /// コマンドに渡された位置引数
        /// </summary>
        public IReadOnlyList<string> PositionalArguments { get; }

        /// <summary>
        /// コマンドレジストリ（helpなど他のコマンドにアクセスする必要があるコマンド用）
        /// </summary>
        public CommandRegistry Registry { get; }

        /// <summary>
        /// コマンド履歴への参照
        /// </summary>
        public IReadOnlyList<string> CommandHistory { get; }

        /// <summary>
        /// 履歴をクリアするコールバック
        /// </summary>
        public Action ClearHistory { get; }

        /// <summary>
        /// 指定番号の履歴を削除するコールバック
        /// </summary>
        public Action<int> DeleteHistoryEntry { get; }

        /// <summary>
        /// コマンド実行に必要なコンテキスト情報を初期化します
        /// </summary>
        /// <param name="stdin">標準入力リーダー</param>
        /// <param name="stdout">標準出力ライター</param>
        /// <param name="stderr">標準エラー出力ライター</param>
        /// <param name="workingDirectory">作業ディレクトリ</param>
        /// <param name="homeDirectory">ホームディレクトリ</param>
        /// <param name="positionalArguments">位置引数</param>
        /// <param name="registry">コマンドレジストリ</param>
        /// <param name="previousWorkingDirectory">前の作業ディレクトリ</param>
        /// <param name="changeWorkingDirectory">作業ディレクトリ変更コールバック</param>
        /// <param name="commandHistory">コマンド履歴</param>
        /// <param name="clearHistory">履歴クリアコールバック</param>
        /// <param name="deleteHistoryEntry">履歴削除コールバック</param>
        public CommandContext(
            IAsyncTextReader stdin,
            IAsyncTextWriter stdout,
            IAsyncTextWriter stderr,
            string workingDirectory,
            string homeDirectory,
            IReadOnlyList<string> positionalArguments,
            CommandRegistry registry = null,
            string previousWorkingDirectory = null,
            Action<string> changeWorkingDirectory = null,
            IReadOnlyList<string> commandHistory = null,
            Action clearHistory = null,
            Action<int> deleteHistoryEntry = null)
        {
            Stdin = stdin;
            Stdout = stdout;
            Stderr = stderr;
            WorkingDirectory = workingDirectory;
            HomeDirectory = homeDirectory;
            PositionalArguments = positionalArguments ?? new List<string>();
            Registry = registry;
            PreviousWorkingDirectory = previousWorkingDirectory;
            ChangeWorkingDirectory = changeWorkingDirectory;
            CommandHistory = commandHistory ?? new List<string>();
            ClearHistory = clearHistory;
            DeleteHistoryEntry = deleteHistoryEntry;
        }
    }
}
