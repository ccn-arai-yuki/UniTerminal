using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// コマンド履歴を表示・管理します。
    /// </summary>
    [Command("history", "Display or manage command history")]
    public class HistoryCommand : ICommand
    {
        [Option("clear", "c", Description = "Clear all history")]
        public bool Clear;

        [Option("delete", "d", Description = "Delete entry at specified position")]
        public int DeletePosition = -1;

        [Option("number", "n", Description = "Display only last N entries")]
        public int Number = -1;

        [Option("reverse", "r", Description = "Display history in reverse order")]
        public bool Reverse;

        public string CommandName => "history";
        public string Description => "Display or manage command history";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            try
            {
                // 履歴クリア
                if (Clear)
                {
                    if (context.ClearHistory == null)
                    {
                        await context.Stderr.WriteLineAsync("history: history clearing not supported", ct);
                        return ExitCode.RuntimeError;
                    }
                    context.ClearHistory();
                    return ExitCode.Success;
                }

                // 指定番号の履歴を削除
                if (DeletePosition > 0)
                {
                    if (context.DeleteHistoryEntry == null)
                    {
                        await context.Stderr.WriteLineAsync("history: history deletion not supported", ct);
                        return ExitCode.RuntimeError;
                    }
                    if (DeletePosition > context.CommandHistory.Count)
                    {
                        await context.Stderr.WriteLineAsync($"history: position {DeletePosition} out of range", ct);
                        return ExitCode.RuntimeError;
                    }
                    context.DeleteHistoryEntry(DeletePosition);
                    return ExitCode.Success;
                }

                // 履歴がない場合
                if (context.CommandHistory == null || context.CommandHistory.Count == 0)
                {
                    return ExitCode.Success;
                }

                // 表示する履歴を取得
                var history = context.CommandHistory;
                int startIndex = 0;
                int count = history.Count;

                // -n オプションで表示数を制限
                if (Number > 0 && Number < count)
                {
                    startIndex = count - Number;
                    count = Number;
                }

                // 履歴を表示
                if (Reverse)
                {
                    // 逆順で表示
                    for (int i = history.Count - 1; i >= startIndex; i--)
                    {
                        ct.ThrowIfCancellationRequested();
                        await context.Stdout.WriteLineAsync($"{i + 1,5}  {history[i]}", ct);
                    }
                }
                else
                {
                    // 正順で表示
                    for (int i = startIndex; i < history.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        await context.Stdout.WriteLineAsync($"{i + 1,5}  {history[i]}", ct);
                    }
                }

                return ExitCode.Success;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync($"history: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // historyは引数を取らないため補完なし
            yield break;
        }
    }
}
