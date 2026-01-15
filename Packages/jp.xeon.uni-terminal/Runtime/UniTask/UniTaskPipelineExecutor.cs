#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System;
using System.Collections.Generic;
using System.Threading;
using Xeon.UniTerminal.Binding;
using Xeon.UniTerminal.Execution;
using Xeon.UniTerminal.Parsing;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// UniTask版のパイプライン実行エンジン。
    /// </summary>
    public class UniTaskPipelineExecutor
    {
        private string workingDirectory;
        private readonly string homeDirectory;
        private readonly CommandRegistry registry;
        private string previousWorkingDirectory;
        private readonly Action<string> changeWorkingDirectoryCallback;
        private readonly IReadOnlyList<string> commandHistory;
        private readonly Action clearHistoryCallback;
        private readonly Action<int> deleteHistoryEntryCallback;

        public UniTaskPipelineExecutor(
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

        private void ChangeWorkingDirectory(string newPath)
        {
            previousWorkingDirectory = workingDirectory;
            workingDirectory = newPath;
            changeWorkingDirectoryCallback?.Invoke(newPath);
        }

        /// <summary>
        /// バインドされたパイプラインをUniTaskを使用して実行します。
        /// </summary>
        public async UniTask<ExecutionResult> ExecuteAsync(
            BoundPipeline pipeline,
            IUniTaskTextReader stdin,
            IUniTaskTextWriter stdout,
            IUniTaskTextWriter stderr,
            CancellationToken ct = default)
        {
            if (pipeline == null || pipeline.Commands.Count == 0)
            {
                return ExecutionResult.Successful;
            }

            var disposables = new List<IDisposable>();

            try
            {
                IUniTaskTextReader currentStdin = stdin ?? UniTaskEmptyTextReader.Instance;
                ExitCode exitCode = ExitCode.Success;

                for (int i = 0; i < pipeline.Commands.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var boundCmd = pipeline.Commands[i];
                    bool isLast = i == pipeline.Commands.Count - 1;

                    // stdinリダイレクションを解決
                    IUniTaskTextReader cmdStdin = currentStdin;
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

                        // Task版のFileTextReaderをアダプター経由で使用
                        var fileReader = new FileTextReader(stdinPath);
                        cmdStdin = new TaskTextReaderAdapter(fileReader);
                    }

                    // stdoutリダイレクションまたはパイプを解決
                    IUniTaskTextWriter cmdStdout;
                    UniTaskListTextWriter pipeBuffer = null;

                    if (boundCmd.Redirections.StdoutMode != RedirectMode.None)
                    {
                        var stdoutPath = PathUtility.ResolvePath(
                            boundCmd.Redirections.StdoutPath,
                            workingDirectory,
                            homeDirectory);

                        // Task版のFileTextWriterをアダプター経由で使用
                        var fileWriter = new FileTextWriter(
                            stdoutPath,
                            boundCmd.Redirections.StdoutMode == RedirectMode.Append);
                        disposables.Add(fileWriter);
                        cmdStdout = new TaskTextWriterAdapter(fileWriter);
                    }
                    else if (isLast)
                    {
                        cmdStdout = stdout;
                    }
                    else
                    {
                        pipeBuffer = new UniTaskListTextWriter();
                        cmdStdout = pipeBuffer;
                    }

                    // コンテキストを作成（Task版のコマンドを使用するため、アダプター経由）
                    var taskStdin = new UniTaskTextReaderAdapter(cmdStdin);
                    var taskStdout = new UniTaskTextWriterAdapter(cmdStdout);
                    var taskStderr = new UniTaskTextWriterAdapter(stderr);

                    var context = new CommandContext(
                        taskStdin,
                        taskStdout,
                        taskStderr,
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
                        currentStdin = new UniTaskListTextReader(pipeBuffer.Lines);
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
#endif
