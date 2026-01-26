using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xeon.UniTerminal.Binding;
using Xeon.UniTerminal.Parsing;

namespace Xeon.UniTerminal.Execution
{
    /// <summary>
    /// バインドされたパイプラインを実行します。
    /// </summary>
    public class PipelineExecutor
    {
        private string workingDirectory;
        private readonly string homeDirectory;
        private readonly CommandRegistry registry;
        private string previousWorkingDirectory;
        private readonly Action<string> changeWorkingDirectoryCallback;
        private readonly IReadOnlyList<string> commandHistory;
        private readonly Action clearHistoryCallback;
        private readonly Action<int> deleteHistoryEntryCallback;

        /// <summary>
        /// パイプライン実行に必要な環境情報を初期化します。
        /// </summary>
        /// <param name="workingDirectory">作業ディレクトリ。</param>
        /// <param name="homeDirectory">ホームディレクトリ。</param>
        /// <param name="registry">コマンドレジストリ。</param>
        /// <param name="previousWorkingDirectory">前の作業ディレクトリ。</param>
        /// <param name="changeWorkingDirectoryCallback">作業ディレクトリ変更コールバック。</param>
        /// <param name="commandHistory">コマンド履歴。</param>
        /// <param name="clearHistoryCallback">履歴クリアコールバック。</param>
        /// <param name="deleteHistoryEntryCallback">履歴削除コールバック。</param>
        public PipelineExecutor(
            string workingDirectory,
            string homeDirectory,
            CommandRegistry registry = null,
            string previousWorkingDirectory = null,
            Action<string> changeWorkingDirectoryCallback = null,
            IReadOnlyList<string> commandHistory = null,
            Action clearHistoryCallback = null,
            Action<int> deleteHistoryEntryCallback = null)
        {
            this.workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
            this.homeDirectory = homeDirectory ?? throw new ArgumentNullException(nameof(homeDirectory));
            this.registry = registry;
            this.previousWorkingDirectory = previousWorkingDirectory;
            this.changeWorkingDirectoryCallback = changeWorkingDirectoryCallback;
            this.commandHistory = commandHistory;
            this.clearHistoryCallback = clearHistoryCallback;
            this.deleteHistoryEntryCallback = deleteHistoryEntryCallback;
        }

        /// <summary>
        /// 作業ディレクトリを変更します（cdコマンドから呼び出される）。
        /// </summary>
        private void ChangeWorkingDirectory(string newPath)
        {
            previousWorkingDirectory = workingDirectory;
            workingDirectory = newPath;
            changeWorkingDirectoryCallback?.Invoke(newPath);
        }

        /// <summary>
        /// バインドされたパイプラインを実行します。
        /// </summary>
        /// <param name="pipeline">実行するバインドされたパイプライン。</param>
        /// <param name="stdin">初期stdin（空の場合はnull）。</param>
        /// <param name="stdout">最終stdout。</param>
        /// <param name="stderr">すべてのコマンド用のstderr。</param>
        /// <param name="ct">キャンセルトークン。</param>
        /// <returns>終了コードを含む実行結果。</returns>
        public async Task<ExecutionResult> ExecuteAsync(
            BoundPipeline pipeline,
            IAsyncTextReader stdin,
            IAsyncTextWriter stdout,
            IAsyncTextWriter stderr,
            CancellationToken ct = default)
        {
            if (pipeline.Commands == null || pipeline.Commands.Count == 0)
                return ExecutionResult.Successful;

            var disposables = new List<IDisposable>();

            try
            {
                return await ExecutePipelineAsync(pipeline, stdin, stdout, stderr, disposables, ct);
            }
            finally
            {
                DisposeAll(disposables);
            }
        }

        private async Task<ExecutionResult> ExecutePipelineAsync(
            BoundPipeline pipeline,
            IAsyncTextReader stdin,
            IAsyncTextWriter stdout,
            IAsyncTextWriter stderr,
            List<IDisposable> disposables,
            CancellationToken ct)
        {
            IAsyncTextReader currentStdin = stdin ?? EmptyTextReader.Instance;
            ExitCode exitCode = ExitCode.Success;

            for (int i = 0; i < pipeline.Commands.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var boundCmd = pipeline.Commands[i];
                bool isLast = i == pipeline.Commands.Count - 1;

                // stdinリダイレクションを解決
                var stdinResult = ResolveStdin(boundCmd, currentStdin);
                if (!stdinResult.Success)
                {
                    await stderr.WriteLineAsync(stdinResult.ErrorMessage, ct);
                    return new ExecutionResult(ExitCode.RuntimeError);
                }

                // stdoutリダイレクションまたはパイプを解決
                var stdoutResult = ResolveStdout(boundCmd, stdout, isLast, disposables);

                // コマンドを実行
                var execResult = await ExecuteCommandAsync(
                    boundCmd, stdinResult.Reader, stdoutResult.Writer, stderr, ct);

                if (!execResult.Success)
                {
                    if (execResult.ErrorMessage != null)
                        await stderr.WriteLineAsync(execResult.ErrorMessage, ct);
                    return new ExecutionResult(execResult.ExitCode);
                }

                exitCode = execResult.ExitCode;

                // コマンドが失敗した場合、パイプラインを停止
                if (exitCode != ExitCode.Success)
                    return new ExecutionResult(exitCode);

                // 次のコマンド用のstdinを準備
                currentStdin = PrepareNextStdin(stdoutResult.PipeBuffer, currentStdin);
            }

            return new ExecutionResult(exitCode);
        }

        private StdinResolutionResult ResolveStdin(BoundCommand boundCmd, IAsyncTextReader currentStdin)
        {
            if (boundCmd.Redirections.StdinPath == null)
                return StdinResolutionResult.FromReader(currentStdin);

            var stdinPath = PathUtility.ResolvePath(
                boundCmd.Redirections.StdinPath,
                workingDirectory,
                homeDirectory);

            if (!System.IO.File.Exists(stdinPath))
                return StdinResolutionResult.FromError($"File not found: {stdinPath}");

            return StdinResolutionResult.FromReader(new FileTextReader(stdinPath));
        }

        private StdoutResolutionResult ResolveStdout(
            BoundCommand boundCmd,
            IAsyncTextWriter stdout,
            bool isLast,
            List<IDisposable> disposables)
        {
            if (boundCmd.Redirections.StdoutMode != RedirectMode.None)
                return ResolveStdoutToFile(boundCmd, disposables);

            if (isLast)
                return StdoutResolutionResult.FromWriter(stdout);

            // 次のコマンドへパイプ
            var pipeBuffer = new ListTextWriter();
            return StdoutResolutionResult.FromPipe(pipeBuffer);
        }

        private StdoutResolutionResult ResolveStdoutToFile(BoundCommand boundCmd, List<IDisposable> disposables)
        {
            var stdoutPath = PathUtility.ResolvePath(
                boundCmd.Redirections.StdoutPath,
                workingDirectory,
                homeDirectory);

            var fileWriter = new FileTextWriter(
                stdoutPath,
                boundCmd.Redirections.StdoutMode == RedirectMode.Append);
            disposables.Add(fileWriter);

            return StdoutResolutionResult.FromWriter(fileWriter);
        }

        private async Task<CommandExecutionResult> ExecuteCommandAsync(
            BoundCommand boundCmd,
            IAsyncTextReader cmdStdin,
            IAsyncTextWriter cmdStdout,
            IAsyncTextWriter stderr,
            CancellationToken ct)
        {
            var context = CreateCommandContext(boundCmd, cmdStdin, cmdStdout, stderr);

            try
            {
                var exitCode = await boundCmd.Command.ExecuteAsync(context, ct);
                return CommandExecutionResult.FromSuccess(exitCode);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return CommandExecutionResult.FromError(
                    $"Error executing {boundCmd.Command.CommandName}: {ex.Message}");
            }
        }

        private CommandContext CreateCommandContext(
            BoundCommand boundCmd,
            IAsyncTextReader cmdStdin,
            IAsyncTextWriter cmdStdout,
            IAsyncTextWriter stderr)
        {
            return new CommandContext(
                cmdStdin,
                cmdStdout,
                stderr,
                workingDirectory,
                homeDirectory,
                boundCmd.PositionalArguments,
                registry,
                previousWorkingDirectory,
                ChangeWorkingDirectory,
                commandHistory,
                clearHistoryCallback,
                deleteHistoryEntryCallback);
        }

        private static IAsyncTextReader PrepareNextStdin(ListTextWriter pipeBuffer, IAsyncTextReader currentStdin)
        {
            if (pipeBuffer == null)
                return currentStdin;

            pipeBuffer.Flush();
            return new ListTextReader(pipeBuffer.Lines);
        }

        private static void DisposeAll(List<IDisposable> disposables)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        #region Result Types

        private readonly struct StdinResolutionResult
        {
            /// <summary>
            /// 解決が成功したかどうか。
            /// </summary>
            public bool Success { get; }

            /// <summary>
            /// 解決された入力リーダー。
            /// </summary>
            public IAsyncTextReader Reader { get; }

            /// <summary>
            /// エラーメッセージ。
            /// </summary>
            public string ErrorMessage { get; }

            private StdinResolutionResult(bool success, IAsyncTextReader reader, string errorMessage)
            {
                Success = success;
                Reader = reader;
                ErrorMessage = errorMessage;
            }

            /// <summary>
            /// 成功結果を生成します。
            /// </summary>
            /// <param name="reader">入力リーダー。</param>
            public static StdinResolutionResult FromReader(IAsyncTextReader reader)
                => new StdinResolutionResult(true, reader, null);

            /// <summary>
            /// 失敗結果を生成します。
            /// </summary>
            /// <param name="message">エラーメッセージ。</param>
            public static StdinResolutionResult FromError(string message)
                => new StdinResolutionResult(false, null, message);
        }

        private readonly struct StdoutResolutionResult
        {
            /// <summary>
            /// 解決された出力ライター。
            /// </summary>
            public IAsyncTextWriter Writer { get; }

            /// <summary>
            /// パイプ用のバッファ。
            /// </summary>
            public ListTextWriter PipeBuffer { get; }

            private StdoutResolutionResult(IAsyncTextWriter writer, ListTextWriter pipeBuffer)
            {
                Writer = writer;
                PipeBuffer = pipeBuffer;
            }

            /// <summary>
            /// 出力ライターの結果を生成します。
            /// </summary>
            /// <param name="writer">出力ライター。</param>
            public static StdoutResolutionResult FromWriter(IAsyncTextWriter writer)
                => new StdoutResolutionResult(writer, null);

            /// <summary>
            /// パイプ用の結果を生成します。
            /// </summary>
            /// <param name="pipeBuffer">パイプバッファ。</param>
            public static StdoutResolutionResult FromPipe(ListTextWriter pipeBuffer)
                => new StdoutResolutionResult(pipeBuffer, pipeBuffer);
        }

        private readonly struct CommandExecutionResult
        {
            /// <summary>
            /// 実行が成功したかどうか。
            /// </summary>
            public bool Success { get; }

            /// <summary>
            /// 終了コード。
            /// </summary>
            public ExitCode ExitCode { get; }

            /// <summary>
            /// エラーメッセージ。
            /// </summary>
            public string ErrorMessage { get; }

            private CommandExecutionResult(bool success, ExitCode exitCode, string errorMessage)
            {
                Success = success;
                ExitCode = exitCode;
                ErrorMessage = errorMessage;
            }

            /// <summary>
            /// 成功結果を生成します。
            /// </summary>
            /// <param name="exitCode">終了コード。</param>
            public static CommandExecutionResult FromSuccess(ExitCode exitCode)
                => new CommandExecutionResult(true, exitCode, null);

            /// <summary>
            /// 失敗結果を生成します。
            /// </summary>
            /// <param name="message">エラーメッセージ。</param>
            public static CommandExecutionResult FromError(string message)
                => new CommandExecutionResult(false, ExitCode.RuntimeError, message);
        }

        #endregion
    }
}
