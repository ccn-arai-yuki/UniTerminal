using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xeon.UniTerminal;

namespace Xeon.UniTerminal.Samples
{
    /// <summary>
    /// Example of creating a custom command for UniTerminal.
    /// </summary>
    [Command("greet", "A sample greeting command")]
    public class GreetCommand : ICommand
    {
        [Option("name", "n", Description = "Name to greet")]
        public string Name;

        [Option("times", "t", Description = "Number of times to greet")]
        public int Times = 1;

        [Option("uppercase", "u", Description = "Output in uppercase")]
        public bool Uppercase;

        public string CommandName => "greet";
        public string Description => "A sample greeting command that demonstrates custom command creation";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            var name = string.IsNullOrEmpty(Name) ? "World" : Name;

            for (int i = 0; i < Times; i++)
            {
                var message = $"Hello, {name}!";
                if (Uppercase)
                {
                    message = message.ToUpperInvariant();
                }
                await context.Stdout.WriteLineAsync(message, ct);
            }

            return ExitCode.Success;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // Provide completion suggestions for the --name option
            if (context.CurrentOption == "name" || context.CurrentOption == "n")
            {
                yield return "Alice";
                yield return "Bob";
                yield return "Charlie";
            }
        }
    }

    /// <summary>
    /// Example of a command that reads from stdin (for pipeline usage).
    /// </summary>
    [Command("count", "Count lines from input")]
    public class CountCommand : ICommand
    {
        [Option("words", "w", Description = "Count words instead of lines")]
        public bool CountWords;

        [Option("chars", "c", Description = "Count characters instead of lines")]
        public bool CountChars;

        public string CommandName => "count";
        public string Description => "Count lines, words, or characters from input";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            var input = await context.Stdin.ReadToEndAsync(ct);

            if (CountChars)
            {
                await context.Stdout.WriteLineAsync(input.Length.ToString(), ct);
            }
            else if (CountWords)
            {
                var words = input.Split(new[] { ' ', '\t', '\n', '\r' },
                    System.StringSplitOptions.RemoveEmptyEntries);
                await context.Stdout.WriteLineAsync(words.Length.ToString(), ct);
            }
            else
            {
                var lines = input.Split('\n');
                var count = string.IsNullOrEmpty(input) ? 0 : lines.Length;
                await context.Stdout.WriteLineAsync(count.ToString(), ct);
            }

            return ExitCode.Success;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            yield break;
        }
    }
}
