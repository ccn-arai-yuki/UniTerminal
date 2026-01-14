using System;
using System.Threading;
using System.Threading.Tasks;
using Xeon.UniTerminal.Binding;
using Xeon.UniTerminal.BuiltInCommands;
using Xeon.UniTerminal.Completion;
using Xeon.UniTerminal.Execution;
using Xeon.UniTerminal.Parsing;
using UnityEngine;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// ターミナルCLIのメインエントリポイント。
    /// </summary>
    public class Terminal
    {
        private readonly CommandRegistry _registry;
        private readonly Parser _parser;
        private readonly Binder _binder;
        private string _workingDirectory;
        private readonly string _homeDirectory;

        /// <summary>
        /// コマンドレジストリ。
        /// </summary>
        public CommandRegistry Registry => _registry;

        /// <summary>
        /// 現在の作業ディレクトリ。
        /// </summary>
        public string WorkingDirectory
        {
            get => _workingDirectory;
            set => _workingDirectory = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// ホームディレクトリ。
        /// </summary>
        public string HomeDirectory => _homeDirectory;

        /// <summary>
        /// 新しいTerminalインスタンスを作成します。
        /// </summary>
        /// <param name="homeDirectory">ホームディレクトリ（デフォルトはApplication.persistentDataPath）。</param>
        /// <param name="workingDirectory">初期作業ディレクトリ（デフォルトはホームディレクトリ）。</param>
        /// <param name="registerBuiltInCommands">組み込みコマンドを登録するかどうか。</param>
        public Terminal(string homeDirectory = null, string workingDirectory = null, bool registerBuiltInCommands = true)
        {
            _homeDirectory = homeDirectory ?? Application.persistentDataPath;
            _workingDirectory = workingDirectory ?? _homeDirectory;
            _registry = new CommandRegistry();
            _parser = new Parser();
            _binder = new Binder(_registry);

            if (registerBuiltInCommands)
            {
                RegisterBuiltInCommands();
            }
        }

        /// <summary>
        /// 組み込みコマンドを登録します。
        /// </summary>
        public void RegisterBuiltInCommands()
        {
            _registry.RegisterCommand<EchoCommand>();
            _registry.RegisterCommand<CatCommand>();
            _registry.RegisterCommand<GrepCommand>();
            _registry.RegisterCommand<HelpCommand>();
        }

        /// <summary>
        /// 指定された入力に対する補完候補を取得します。
        /// </summary>
        /// <param name="input">現在の入力行。</param>
        /// <returns>候補を含む補完結果。</returns>
        public CompletionResult GetCompletions(string input)
        {
            var engine = new CompletionEngine(_registry, _workingDirectory, _homeDirectory);
            return engine.GetCompletions(input);
        }

        /// <summary>
        /// コマンドラインを実行します。
        /// </summary>
        /// <param name="input">コマンドライン入力。</param>
        /// <param name="stdout">標準出力ライター。</param>
        /// <param name="stderr">標準エラー出力ライター。</param>
        /// <param name="stdin">標準入力リーダー（オプション）。</param>
        /// <param name="ct">キャンセルトークン。</param>
        /// <returns>終了コード（成功の場合はSuccess）。</returns>
        public async Task<ExitCode> ExecuteAsync(
            string input,
            IAsyncTextWriter stdout,
            IAsyncTextWriter stderr,
            IAsyncTextReader stdin = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return ExitCode.Success;
            }

            try
            {
                // パース
                var parsed = _parser.Parse(input);
                if (parsed.IsEmpty)
                {
                    return ExitCode.Success;
                }

                // バインド
                var bound = _binder.Bind(parsed.Pipeline);

                // 実行
                var executor = new PipelineExecutor(_workingDirectory, _homeDirectory, _registry);
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
    }
}
