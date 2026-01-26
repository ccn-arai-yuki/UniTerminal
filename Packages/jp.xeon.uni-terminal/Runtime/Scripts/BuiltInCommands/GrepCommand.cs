using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// パターンに一致する行をフィルタリングします
    /// </summary>
    [Command("grep", "Filter lines matching a pattern")]
    public class GrepCommand : ICommand
    {
        /// <summary>
        /// 検索パターン
        /// </summary>
        [Option("pattern", "p", isRequired: true, Description = "Pattern to search for")]
        public string Pattern;

        /// <summary>
        /// 大文字小文字を無視するかどうか
        /// </summary>
        [Option("ignorecase", "i", Description = "Ignore case distinctions")]
        public bool IgnoreCase;

        /// <summary>
        /// マッチしない行を選択するかどうか
        /// </summary>
        [Option("invert", "v", Description = "Select non-matching lines")]
        public bool InvertMatch;

        /// <summary>
        /// 件数のみ出力するかどうか
        /// </summary>
        [Option("count", "c", Description = "Only print count of matching lines")]
        public bool CountOnly;

        /// <summary>
        /// コマンド名
        /// </summary>
        public string CommandName => "grep";

        /// <summary>
        /// コマンドの説明
        /// </summary>
        public string Description => "Filter lines matching a pattern";

        /// <summary>
        /// コマンドを実行します
        /// </summary>
        /// <param name="context">実行コンテキスト</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>終了コード</returns>
        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(Pattern))
            {
                await context.Stderr.WriteLineAsync("grep: pattern is required", ct);
                return ExitCode.UsageError;
            }

            var options = IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            Regex regex;

            try
            {
                regex = new Regex(Pattern, options);
            }
            catch (System.Exception ex)
            {
                await context.Stderr.WriteLineAsync($"grep: invalid pattern: {ex.Message}", ct);
                return ExitCode.UsageError;
            }

            var (matchCount, hasMatch) = await ProcessLines(context, regex, ct);

            if (CountOnly)
            {
                await context.Stdout.WriteLineAsync(matchCount.ToString(), ct);
            }

            return hasMatch ? ExitCode.Success : ExitCode.RuntimeError;
        }

        private async Task<(int matchCount, bool hasMatch)> ProcessLines(CommandContext context, Regex regex, CancellationToken ct)
        {
            var matchCount = 0;
            await foreach (var line in context.Stdin.ReadLinesAsync(ct))
            {
                bool isMatch = regex.IsMatch(line);

                if (InvertMatch)
                    isMatch = !isMatch;

                if (!isMatch)
                    continue;
                
                matchCount++;

                if (!CountOnly)
                {
                    await context.Stdout.WriteLineAsync(line, ct);
                }
            }

            return (matchCount, matchCount > 0);
        }

        /// <summary>
        /// 補完候補を返します
        /// </summary>
        /// <param name="context">補完コンテキスト</param>
        /// <returns>補完候補</returns>
        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            yield break;
        }
    }
}
