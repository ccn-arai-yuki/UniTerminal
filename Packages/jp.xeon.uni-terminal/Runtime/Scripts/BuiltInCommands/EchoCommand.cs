using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// 引数を標準出力にエコーします。
    /// </summary>
    [Command("echo", "Echo arguments to stdout")]
    public class EchoCommand : ICommand
    {
        [Option("newline", "n", Description = "Do not output trailing newline")]
        public bool NoNewline;

        public string CommandName => "echo";
        public string Description => "Echo arguments to stdout";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            var output = string.Join(" ", context.PositionalArguments);

            if (NoNewline)
            {
                await context.Stdout.WriteAsync(output, ct);
            }
            else
            {
                await context.Stdout.WriteLineAsync(output, ct);
            }

            return ExitCode.Success;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            yield break;
        }
    }
}
