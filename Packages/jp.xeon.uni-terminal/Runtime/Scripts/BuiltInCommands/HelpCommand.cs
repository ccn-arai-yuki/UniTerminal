using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// コマンドのヘルプを表示します。
    /// </summary>
    [Command("help", "Display help for commands")]
    public class HelpCommand : ICommand
    {
        public string CommandName => "help";
        public string Description => "Display help for commands";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            var registry = context.Registry;
            if (registry == null)
            {
                await context.Stderr.WriteLineAsync("help: registry not configured", ct);
                return ExitCode.RuntimeError;
            }

            if (context.PositionalArguments.Count == 0)
            {
                // グローバルヘルプを表示
                await context.Stdout.WriteLineAsync(registry.GenerateGlobalHelp(), ct);
                return ExitCode.Success;
            }

            // 特定のコマンドのヘルプを表示
            var commandName = context.PositionalArguments[0];

            if (!registry.TryGetCommand(commandName, out var metadata))
            {
                await context.Stderr.WriteLineAsync($"help: unknown command: {commandName}", ct);
                return ExitCode.UsageError;
            }

            await context.Stdout.WriteLineAsync(metadata.GenerateHelp(), ct);
            return ExitCode.Success;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // 補完はレジストリを使用してCompletionEngineで処理される
            yield break;
        }
    }
}
