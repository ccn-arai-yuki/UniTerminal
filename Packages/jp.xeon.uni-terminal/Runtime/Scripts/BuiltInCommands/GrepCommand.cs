using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// パターンに一致する行をフィルタリングします。
    /// </summary>
    [Command("grep", "Filter lines matching a pattern")]
    public class GrepCommand : ICommand
    {
        [Option("pattern", "p", isRequired: true, Description = "Pattern to search for")]
        public string Pattern;

        [Option("ignorecase", "i", Description = "Ignore case distinctions")]
        public bool IgnoreCase;

        [Option("invert", "v", Description = "Select non-matching lines")]
        public bool InvertMatch;

        [Option("count", "c", Description = "Only print count of matching lines")]
        public bool CountOnly;

        public string CommandName => "grep";
        public string Description => "Filter lines matching a pattern";

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

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            yield break;
        }
    }
}
