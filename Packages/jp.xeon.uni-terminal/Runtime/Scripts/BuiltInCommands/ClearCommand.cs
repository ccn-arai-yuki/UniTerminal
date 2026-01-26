using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    [Command("clear", "Clear the terminal screen")]
    public class ClearCommand : ICommand
    {
        public string CommandName => "clear";

        public string Description => "Clear the terminal screen";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken cts)
        {
            context.Stdout.Clear();
            context.Stderr.Clear();
            await Task.CompletedTask;
            return ExitCode.Success;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
