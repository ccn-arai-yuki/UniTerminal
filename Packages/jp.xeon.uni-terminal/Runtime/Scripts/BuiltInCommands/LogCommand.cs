using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// Unityログを表示するコマンド
    /// </summary>
    [Command("log", "Display Unity log messages")]
    public class LogCommand : ICommand
    {
        [Option("info", "i", Description = "Show only Info level logs")]
        public bool Info;

        [Option("warn", "w", Description = "Show only Warning level logs")]
        public bool Warn;

        [Option("error", "e", Description = "Show only Error level logs")]
        public bool Error;

        [Option("follow", "f", Description = "Output appended logs in real-time (Ctrl+C to stop)")]
        public bool Follow;

        [Option("tail", "t", Description = "Output the last N log entries")]
        public int? Tail;

        [Option("head", "h", Description = "Output the first N log entries")]
        public int? Head;

        [Option("stack-trace", "s", Description = "Show stack traces")]
        public bool StackTrace;

        public string CommandName => "log";
        public string Description => "Display Unity log messages";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            // LogBufferが設定されていない場合
            if (context.LogBuffer == null)
            {
                await context.Stderr.WriteLineAsync("log: LogBuffer is not available", ct);
                return ExitCode.RuntimeError;
            }

            // --head と --tail は排他
            if (Head.HasValue && Tail.HasValue)
            {
                await context.Stderr.WriteLineAsync("log: cannot specify both --head and --tail", ct);
                return ExitCode.UsageError;
            }

            // フィルタ条件を取得
            var filter = GetLogTypeFilter();

            // 履歴ログを出力
            var entries = context.LogBuffer.GetEntries();
            var filteredEntries = FilterEntries(entries, filter);
            var outputEntries = ApplyHeadTail(filteredEntries);

            foreach (var entry in outputEntries)
            {
                await OutputLogEntryAsync(context, entry, ct);
            }

            // -f オプション: リアルタイム監視
            if (Follow)
                return await FollowLogsAsync(context, filter, ct);

            return ExitCode.Success;
        }

        /// <summary>
        /// オプションに応じたログタイプフィルタを取得する。
        /// </summary>
        private HashSet<LogType> GetLogTypeFilter()
        {
            var filter = new HashSet<LogType>();

            // フィルタ指定がない場合は全て表示
            if (!Info && !Warn && !Error)
            {
                filter.Add(LogType.Log);
                filter.Add(LogType.Warning);
                filter.Add(LogType.Error);
                filter.Add(LogType.Exception);
                filter.Add(LogType.Assert);
                return filter;
            }

            if (Info)
                filter.Add(LogType.Log);

            if (Warn)
                filter.Add(LogType.Warning);

            if (Error)
            {
                filter.Add(LogType.Error);
                filter.Add(LogType.Exception);
                filter.Add(LogType.Assert);
            }

            return filter;
        }

        /// <summary>
        /// フィルタに一致するエントリのみを抽出する。
        /// </summary>
        private IEnumerable<LogEntry> FilterEntries(List<LogEntry> entries, HashSet<LogType> filter)
        {
            return entries.Where(e => filter.Contains(e.Type));
        }

        /// <summary>
        /// headまたはtailオプションに応じてエントリを絞り込む。
        /// </summary>
        private IEnumerable<LogEntry> ApplyHeadTail(IEnumerable<LogEntry> entries)
        {
            if (Head.HasValue)
                return entries.Take(Head.Value);

            if (Tail.HasValue)
                return entries.TakeLast(Tail.Value);

            return entries;
        }

        /// <summary>
        /// ログエントリを出力する。
        /// </summary>
        private async Task OutputLogEntryAsync(
            CommandContext context,
            LogEntry entry,
            CancellationToken ct)
        {
            var formattedMessage = FormatLogEntry(entry);
            await context.Stdout.WriteLineAsync(formattedMessage, ct);

            if (StackTrace && !string.IsNullOrEmpty(entry.StackTrace))
            {
                var stackTraceLines = entry.StackTrace.TrimEnd().Split('\n');
                foreach (var line in stackTraceLines)
                {
                    await context.Stdout.WriteLineAsync($"    {line}", ct);
                }
            }
        }

        /// <summary>
        /// ログエントリを整形する。
        /// </summary>
        private string FormatLogEntry(LogEntry entry)
        {
            var (prefix, color) = GetPrefixAndColor(entry.Type);
            return $"<color={color}>{prefix} {entry.Message}</color>";
        }

        /// <summary>
        /// ログタイプに応じたプレフィックスと色を取得する。
        /// </summary>
        private (string prefix, string color) GetPrefixAndColor(LogType type)
        {
            return type switch
            {
                LogType.Log => ("[INFO]", "white"),
                LogType.Warning => ("[WARN]", "yellow"),
                LogType.Error => ("[ERROR]", "red"),
                LogType.Exception => ("[EXCEPTION]", "red"),
                LogType.Assert => ("[ASSERT]", "red"),
                _ => ("[INFO]", "white")
            };
        }

        /// <summary>
        /// リアルタイムでログを監視
        /// Ctrl+CでCancellationTokenがキャンセルされると終了
        /// </summary>
        private async Task<ExitCode> FollowLogsAsync(
            CommandContext context,
            HashSet<LogType> filter,
            CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();

            void OnLogReceived(LogEntry entry)
            {
                if (!filter.Contains(entry.Type))
                    return;

                try
                {
                    // 同期的に出力（イベントハンドラ内なので）
                    var formattedMessage = FormatLogEntry(entry);
                    context.Stdout.WriteLineAsync(formattedMessage, ct).Wait(ct);

                    if (StackTrace && !string.IsNullOrEmpty(entry.StackTrace))
                    {
                        var stackTraceLines = entry.StackTrace.TrimEnd().Split('\n');
                        foreach (var line in stackTraceLines)
                        {
                            context.Stdout.WriteLineAsync($"    {line}", ct).Wait(ct);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetResult(true);
                }
            }

            context.LogBuffer.OnLogReceived += OnLogReceived;

            try
            {
                // Ctrl+C（CancellationToken）でキャンセルされるまで待機
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                // Ctrl+Cによる正常終了
            }
            finally
            {
                context.LogBuffer.OnLogReceived -= OnLogReceived;
            }

            return ExitCode.Success;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            return Enumerable.Empty<string>();
        }
    }
}
