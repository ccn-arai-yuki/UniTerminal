#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System;
using System.Threading;
using Xeon.UniTerminal.Binding;
using Xeon.UniTerminal.Parsing;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// Terminal用のUniTask拡張メソッド。
    /// </summary>
    public static class TerminalUniTaskExtensions
    {
        /// <summary>
        /// UniTaskを使用してコマンドを実行します。
        /// </summary>
        /// <param name="terminal">ターミナルインスタンス。</param>
        /// <param name="input">コマンドライン入力。</param>
        /// <param name="stdout">標準出力ライター。</param>
        /// <param name="stderr">標準エラー出力ライター。</param>
        /// <param name="stdin">標準入力リーダー（オプション）。</param>
        /// <param name="ct">キャンセルトークン。</param>
        /// <returns>終了コード。</returns>
        public static async UniTask<ExitCode> ExecuteUniTaskAsync(
            this Terminal terminal,
            string input,
            IUniTaskTextWriter stdout,
            IUniTaskTextWriter stderr,
            IUniTaskTextReader stdin = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return ExitCode.Success;
            }

            // 履歴に追加
            terminal.AddHistory(input);

            try
            {
                // パーサーとバインダーはTerminalの内部にあるため、
                // 直接アクセスできないので新しいインスタンスを作成
                var parser = new Parser();
                var binder = new Binder(terminal.Registry);

                // パース
                var parsed = parser.Parse(input);
                if (parsed.IsEmpty)
                {
                    return ExitCode.Success;
                }

                // バインド
                var bound = binder.Bind(parsed.Pipeline);

                // UniTask版エグゼキュータで実行
                var executor = new UniTaskPipelineExecutor(
                    terminal.WorkingDirectory,
                    terminal.HomeDirectory,
                    terminal.Registry,
                    terminal.PreviousWorkingDirectory,
                    path => terminal.WorkingDirectory = path,
                    terminal.CommandHistory,
                    () => terminal.ClearHistory(),
                    terminal.DeleteHistoryEntry);

                var result = await executor.ExecuteAsync(bound, stdin, stdout, stderr, ct);

                return result.ExitCode;
            }
            catch (ParseException ex)
            {
                await stderr.WriteLineAsync($"Parse error: {ex.Message}", ct);
                return ExitCode.UsageError;
            }
            catch (BindException ex)
            {
                await stderr.WriteLineAsync(ex.Message, ct);
                return ExitCode.UsageError;
            }
            catch (RuntimeException ex)
            {
                await stderr.WriteLineAsync($"Runtime error: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
            catch (OperationCanceledException)
            {
                return ExitCode.RuntimeError;
            }
            catch (Exception ex)
            {
                await stderr.WriteLineAsync($"Unexpected error: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
        }

        /// <summary>
        /// Task版のライター/リーダーをUniTask版に変換して実行します。
        /// </summary>
        public static UniTask<ExitCode> ExecuteUniTaskAsync(
            this Terminal terminal,
            string input,
            IAsyncTextWriter stdout,
            IAsyncTextWriter stderr,
            IAsyncTextReader stdin = null,
            CancellationToken ct = default)
        {
            var uniTaskStdout = new TaskTextWriterAdapter(stdout);
            var uniTaskStderr = new TaskTextWriterAdapter(stderr);
            var uniTaskStdin = stdin != null ? new TaskTextReaderAdapter(stdin) : null;

            return terminal.ExecuteUniTaskAsync(input, uniTaskStdout, uniTaskStderr, uniTaskStdin, ct);
        }
    }
}
#endif
