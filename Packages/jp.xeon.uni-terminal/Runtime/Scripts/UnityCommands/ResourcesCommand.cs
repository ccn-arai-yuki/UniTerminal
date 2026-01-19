using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Xeon.UniTerminal.Assets;

namespace Xeon.UniTerminal.UnityCommands
{
    /// <summary>
    /// Resources経由でアセットを管理するコマンド。
    /// 注意: Resources APIはUnityが非推奨としています。
    /// 可能であればAddressablesの使用を推奨します。
    /// </summary>
    [Command("res", "Load assets via Resources (DEPRECATED - prefer Addressables)")]
    public class ResourcesCommand : ICommand
    {
        [Option("type", "t", Description = "Asset type to load")]
        public string TypeFilter;

        [Option("long", "l", Description = "Show detailed information")]
        public bool LongFormat;

        private static ResourcesAssetProvider provider;

        private static ResourcesAssetProvider Provider
        {
            get
            {
                if (provider == null)
                {
                    provider = new ResourcesAssetProvider();
                    AssetManager.Instance.RegisterProvider(provider);
                }
                return provider;
            }
        }

        public string CommandName => "res";
        public string Description => "Load assets via Resources (DEPRECATED)";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
            {
                await ShowHelpAsync(context, ct);
                return ExitCode.UsageError;
            }

            var subCommand = context.PositionalArguments[0].ToLower();
            var args = context.PositionalArguments.Skip(1).ToList();

            return subCommand switch
            {
                "load" => await LoadAsync(context, args, ct),
                "unload" => await UnloadAsync(context, args, ct),
                "unloadunused" => await UnloadUnusedAsync(context, ct),
                "find" => await FindAsync(context, args, ct),
                "help" => await ShowHelpAsync(context, ct),
                _ => await UnknownSubCommandAsync(context, subCommand, ct)
            };
        }

        private async Task<ExitCode> ShowHelpAsync(CommandContext context, CancellationToken ct)
        {
            await context.Stdout.WriteLineAsync("res - Load assets via Resources API", ct);
            await context.Stdout.WriteLineAsync("", ct);
            await context.Stdout.WriteLineAsync("WARNING: Resources API is deprecated by Unity.", ct);
            await context.Stdout.WriteLineAsync("  - All assets in Resources folders are included in builds", ct);
            await context.Stdout.WriteLineAsync("  - Cannot list available assets at runtime", ct);
            await context.Stdout.WriteLineAsync("  - Consider using Addressables (adr command) instead", ct);
            await context.Stdout.WriteLineAsync("", ct);
            await context.Stdout.WriteLineAsync("Usage: res <subcommand> [arguments]", ct);
            await context.Stdout.WriteLineAsync("", ct);
            await context.Stdout.WriteLineAsync("Subcommands:", ct);
            await context.Stdout.WriteLineAsync("  load <path>      Load asset from Resources folder", ct);
            await context.Stdout.WriteLineAsync("  unload <spec>    Unload a loaded asset", ct);
            await context.Stdout.WriteLineAsync("  unloadunused     Unload all unused assets", ct);
            await context.Stdout.WriteLineAsync("  find <pattern>   Find loaded assets (already in memory)", ct);
            await context.Stdout.WriteLineAsync("  help             Show this help message", ct);
            await context.Stdout.WriteLineAsync("", ct);
            await context.Stdout.WriteLineAsync("Options:", ct);
            await context.Stdout.WriteLineAsync("  -t, --type       Asset type (e.g., Texture2D, Material)", ct);
            await context.Stdout.WriteLineAsync("  -l, --long       Show detailed information", ct);
            await context.Stdout.WriteLineAsync("", ct);
            await context.Stdout.WriteLineAsync("Examples:", ct);
            await context.Stdout.WriteLineAsync("  res load Prefabs/Player", ct);
            await context.Stdout.WriteLineAsync("  res load -t Material Materials/Red", ct);
            await context.Stdout.WriteLineAsync("  res unload #12345", ct);
            await context.Stdout.WriteLineAsync("  res unloadunused", ct);

            return ExitCode.Success;
        }

        private async Task<ExitCode> LoadAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("res load: usage: res load <path>", ct);
                await context.Stderr.WriteLineAsync("  path: Path relative to Resources folder (without extension)", ct);
                await context.Stderr.WriteLineAsync("  Example: res load Prefabs/Player", ct);
                return ExitCode.UsageError;
            }

            var resourcePath = args[0];
            Type assetType = typeof(UnityEngine.Object);

            if (!string.IsNullOrEmpty(TypeFilter))
            {
                assetType = TypeResolver.ResolveAssetType(TypeFilter);
                if (assetType == null)
                {
                    await context.Stderr.WriteLineAsync($"res: unknown type '{TypeFilter}'", ct);
                    return ExitCode.UsageError;
                }
            }

            var entry = await AssetManager.Instance.LoadAsync(Provider, resourcePath, assetType, ct);

            if (entry == null)
            {
                await context.Stderr.WriteLineAsync($"res: failed to load '{resourcePath}'", ct);
                await context.Stderr.WriteLineAsync("  Make sure the asset exists in a Resources folder", ct);
                return ExitCode.RuntimeError;
            }

            await context.Stdout.WriteLineAsync($"Loaded: {entry.Name} #{entry.InstanceId} ({entry.AssetType.Name})", ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> UnloadAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("res unload: usage: res unload <specifier>", ct);
                await context.Stderr.WriteLineAsync("  specifier: #instanceId or asset name", ct);
                return ExitCode.UsageError;
            }

            var specifier = args[0];
            var manager = AssetManager.Instance;

            if (!manager.Registry.TryResolve(specifier, out var entry))
            {
                await context.Stderr.WriteLineAsync($"res: '{specifier}' not found", ct);
                return ExitCode.RuntimeError;
            }

            var name = entry.Name;
            var instanceId = entry.InstanceId;

            if (manager.Unload(specifier))
            {
                await context.Stdout.WriteLineAsync($"Unloaded: {name} #{instanceId}", ct);
                return ExitCode.Success;
            }

            await context.Stderr.WriteLineAsync($"res: failed to unload '{specifier}'", ct);
            return ExitCode.RuntimeError;
        }

        private async Task<ExitCode> UnloadUnusedAsync(CommandContext context, CancellationToken ct)
        {
            await context.Stdout.WriteLineAsync("Unloading unused assets...", ct);

            Provider.UnloadUnusedAssets();

            await context.Stdout.WriteLineAsync("Unused assets unloaded.", ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> FindAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            var pattern = args.Count > 0 ? string.Join(" ", args) : "*";
            Type assetType = null;

            if (!string.IsNullOrEmpty(TypeFilter))
            {
                assetType = TypeResolver.ResolveAssetType(TypeFilter);
                if (assetType == null)
                {
                    await context.Stderr.WriteLineAsync($"res: unknown type '{TypeFilter}'", ct);
                    return ExitCode.UsageError;
                }
            }

            await context.Stdout.WriteLineAsync("Note: Resources API cannot list unloaded assets.", ct);
            await context.Stdout.WriteLineAsync("Showing only already-loaded assets matching the pattern.", ct);
            await context.Stdout.WriteLineAsync("", ct);

            var results = Provider.Find(pattern, assetType).ToList();

            if (results.Count == 0)
            {
                await context.Stdout.WriteLineAsync("No loaded assets found.", ct);
                return ExitCode.Success;
            }

            await context.Stdout.WriteLineAsync($"Found {results.Count} loaded assets:", ct);

            foreach (var info in results)
            {
                ct.ThrowIfCancellationRequested();

                if (LongFormat)
                    await context.Stdout.WriteLineAsync($"  {info.AssetType?.Name,-20} {info.Name}", ct);
                else
                    await context.Stdout.WriteLineAsync($"  {info.Name}", ct);
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> UnknownSubCommandAsync(
            CommandContext context,
            string subCommand,
            CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"res: unknown subcommand '{subCommand}'", ct);
            await context.Stderr.WriteLineAsync("Subcommands: load, unload, unloadunused, find, help", ct);
            return ExitCode.UsageError;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";

            if (context.TokenIndex == 1)
            {
                var subCommands = new[] { "load", "unload", "unloadunused", "find", "help" };
                foreach (var cmd in subCommands)
                {
                    if (cmd.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                        yield return cmd;
                }
                yield break;
            }

            // unload の引数としてロード済みアセットを補完
            var tokens = context.InputLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 2 && tokens[1].ToLower() == "unload" && context.TokenIndex == 2)
            {
                var registry = AssetManager.Instance.Registry;
                foreach (var entry in registry.GetAll())
                {
                    if (entry.Name.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                        yield return entry.Name;
                    var idStr = $"#{entry.InstanceId}";
                    if (idStr.StartsWith(token))
                        yield return idStr;
                }
            }
        }
    }
}
