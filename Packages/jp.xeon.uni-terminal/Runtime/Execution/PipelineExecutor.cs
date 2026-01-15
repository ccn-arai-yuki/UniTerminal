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
            if (pipeline == null || pipeline.Commands.Count == 0)
            {
                return ExecutionResult.Successful;
            }

            var disposables = new List<IDisposable>();

            try
            {
                IAsyncTextReader currentStdin = stdin ?? EmptyTextReader.Instance;
                ExitCode exitCode = ExitCode.Success;

                for (int i = 0; i < pipeline.Commands.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var boundCmd = pipeline.Commands[i];
                    bool isLast = i == pipeline.Commands.Count - 1;

                    // stdinリダイレクションを解決
                    IAsyncTextReader cmdStdin = currentStdin;
                    if (boundCmd.Redirections.StdinPath != null)
                    {
                        var stdinPath = PathUtility.ResolvePath(
                            boundCmd.Redirections.StdinPath,
                            workingDirectory,
                            homeDirectory);

                        if (!System.IO.File.Exists(stdinPath))
                        {
                            await stderr.WriteLineAsync($"File not found: {stdinPath}", ct);
                            return new ExecutionResult(ExitCode.RuntimeError);
                        }

                        cmdStdin = new FileTextReader(stdinPath);
                    }

                    // stdoutリダイレクションまたはパイプを解決
                    IAsyncTextWriter cmdStdout;
                    ListTextWriter pipeBuffer = null;

                    if (boundCmd.Redirections.StdoutMode != RedirectMode.None)
                    {
                        var stdoutPath = PathUtility.ResolvePath(
                            boundCmd.Redirections.StdoutPath,
                            workingDirectory,
                            homeDirectory);

                        var fileWriter = new FileTextWriter(
                            stdoutPath,
                            boundCmd.Redirections.StdoutMode == RedirectMode.Append);
                        disposables.Add(fileWriter);
                        cmdStdout = fileWriter;
                    }
                    else if (isLast)
                    {
                        cmdStdout = stdout;
                    }
                    else
                    {
                        // 次のコマンドへパイプ
                        pipeBuffer = new ListTextWriter();
                        cmdStdout = pipeBuffer;
                    }

                    // コンテキストを作成
                    var context = new CommandContext(
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

                    // コマンドを実行
                    try
                    {
                        exitCode = await boundCmd.Command.ExecuteAsync(context, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        await stderr.WriteLineAsync($"Error executing {boundCmd.Command.CommandName}: {ex.Message}", ct);
                        return new ExecutionResult(ExitCode.RuntimeError);
                    }

                    // コマンドが失敗した場合、パイプラインを停止
                    if (exitCode != ExitCode.Success)
                    {
                        return new ExecutionResult(exitCode);
                    }

                    // 次のコマンド用のstdinを準備
                    if (pipeBuffer != null)
                    {
                        pipeBuffer.Flush();
                        currentStdin = new ListTextReader(pipeBuffer.Lines);
                    }
                }

                return new ExecutionResult(exitCode);
            }
            finally
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
