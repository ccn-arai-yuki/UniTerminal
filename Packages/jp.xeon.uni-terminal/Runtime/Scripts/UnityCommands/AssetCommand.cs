using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xeon.UniTerminal.Assets;

namespace Xeon.UniTerminal.UnityCommands
{
    /// <summary>
    /// ロード済みアセットを管理するコマンド。
    /// </summary>
    [Command("asset", "Manage loaded assets (list, info, unload)")]
    public class AssetCommand : ICommand
    {
        #region Options

        [Option("type", "t", Description = "Filter by asset type")]
        public string TypeFilter;

        [Option("name", "n", Description = "Filter by name pattern (wildcards supported)")]
        public string NameFilter;

        [Option("long", "l", Description = "Show detailed information")]
        public bool LongFormat;

        #endregion

        #region ICommand

        public string CommandName => "asset";
        public string Description => "Manage loaded assets";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
                return await ShowUsageAsync(context, ct);

            var subCommand = context.PositionalArguments[0].ToLower();
            var args = context.PositionalArguments.Skip(1).ToList();

            return subCommand switch
            {
                "list" => await ListAsync(context, ct),
                "info" => await InfoAsync(context, args, ct),
                "unload" => await UnloadAsync(context, args, ct),
                "providers" => await ProvidersAsync(context, ct),
                _ => await UnknownSubCommandAsync(context, subCommand, ct)
            };
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";

            if (context.TokenIndex == 1)
                return GetSubCommandCompletions(token);

            if (context.TokenIndex == 2)
                return GetAssetCompletions(token);

            return Enumerable.Empty<string>();
        }

        #endregion

        #region Subcommands

        private async Task<ExitCode> ListAsync(CommandContext context, CancellationToken ct)
        {
            var registry = AssetManager.Instance.Registry;
            var assetType = ResolveAssetType();

            if (!string.IsNullOrEmpty(TypeFilter) && assetType == null)
            {
                await context.Stderr.WriteLineAsync($"asset: unknown type '{TypeFilter}'", ct);
                return ExitCode.UsageError;
            }

            var entries = !string.IsNullOrEmpty(NameFilter)
                ? registry.Find(NameFilter, assetType)
                : registry.GetAll(assetType);

            var entryList = entries.ToList();

            if (entryList.Count == 0)
            {
                await context.Stdout.WriteLineAsync("No loaded assets found.", ct);
                return ExitCode.Success;
            }

            await context.Stdout.WriteLineAsync($"Loaded assets ({entryList.Count}):", ct);

            foreach (var entry in entryList)
            {
                ct.ThrowIfCancellationRequested();
                await WriteAssetEntry(context, entry, ct);
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> InfoAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("asset info: usage: asset info <specifier>", ct);
                await context.Stderr.WriteLineAsync("  specifier: #instanceId, asset name, or key", ct);
                return ExitCode.UsageError;
            }

            var specifier = args[0];
            var registry = AssetManager.Instance.Registry;

            if (!registry.TryResolve(specifier, out var entry))
            {
                var byName = registry.GetByName(specifier);
                if (byName.Count > 1)
                    return await ShowAmbiguousAssets(context, specifier, byName.ToList(), ct);

                await context.Stderr.WriteLineAsync($"asset: '{specifier}' not found", ct);
                return ExitCode.RuntimeError;
            }

            await WriteAssetInfo(context, entry, ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> UnloadAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("asset unload: usage: asset unload <specifier>", ct);
                return ExitCode.UsageError;
            }

            var specifier = args[0];
            var manager = AssetManager.Instance;

            if (!manager.Registry.TryResolve(specifier, out var entry))
            {
                await context.Stderr.WriteLineAsync($"asset: '{specifier}' not found", ct);
                return ExitCode.RuntimeError;
            }

            var name = entry.Name;
            var instanceId = entry.InstanceId;

            if (!manager.Unload(specifier))
            {
                await context.Stderr.WriteLineAsync($"asset: failed to unload '{specifier}'", ct);
                return ExitCode.RuntimeError;
            }

            await context.Stdout.WriteLineAsync($"Unloaded: {name} #{instanceId}", ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> ProvidersAsync(CommandContext context, CancellationToken ct)
        {
            var providers = AssetManager.Instance.GetAvailableProviders().ToList();

            if (providers.Count == 0)
            {
                await context.Stdout.WriteLineAsync("No asset providers registered.", ct);
                return ExitCode.Success;
            }

            await context.Stdout.WriteLineAsync("Available asset providers:", ct);
            foreach (var provider in providers)
            {
                var status = provider.IsAvailable ? "[Available]" : "[Unavailable]";
                await context.Stdout.WriteLineAsync($"  {provider.ProviderName,-20} {status}", ct);
            }

            return ExitCode.Success;
        }

        #endregion

        #region Helpers

        private Type ResolveAssetType()
        {
            return string.IsNullOrEmpty(TypeFilter) ? null : TypeResolver.ResolveAssetType(TypeFilter);
        }

        private async Task WriteAssetEntry(CommandContext context, LoadedAssetEntry entry, CancellationToken ct)
        {
            if (LongFormat)
            {
                var line = $"  #{entry.InstanceId,-10} {entry.Name,-30} {entry.AssetType.Name,-20} [{entry.ProviderName}]";
                if (!string.IsNullOrEmpty(entry.Key))
                    line += $" ({entry.Key})";
                await context.Stdout.WriteLineAsync(line, ct);
            }
            else
            {
                await context.Stdout.WriteLineAsync($"  {entry.Name} #{entry.InstanceId}", ct);
            }
        }

        private async Task WriteAssetInfo(CommandContext context, LoadedAssetEntry entry, CancellationToken ct)
        {
            await context.Stdout.WriteLineAsync($"Asset: {entry.Name}", ct);
            await context.Stdout.WriteLineAsync($"  Instance ID: #{entry.InstanceId}", ct);
            await context.Stdout.WriteLineAsync($"  Type:        {entry.AssetType.FullName}", ct);
            await context.Stdout.WriteLineAsync($"  Provider:    {entry.ProviderName}", ct);

            if (!string.IsNullOrEmpty(entry.Key))
                await context.Stdout.WriteLineAsync($"  Key:         {entry.Key}", ct);

            await context.Stdout.WriteLineAsync($"  Loaded at:   {entry.LoadedAt:yyyy-MM-dd HH:mm:ss}", ct);
        }

        private async Task<ExitCode> ShowAmbiguousAssets(
            CommandContext context, string specifier, List<LoadedAssetEntry> byName, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"asset: '{specifier}' matches multiple assets:", ct);
            foreach (var e in byName)
                await context.Stderr.WriteLineAsync($"  #{e.InstanceId} {e.Name} ({e.AssetType.Name})", ct);
            await context.Stderr.WriteLineAsync("Use instance ID (#xxxxx) to specify.", ct);
            return ExitCode.UsageError;
        }

        #endregion

        #region Output Helpers

        private async Task<ExitCode> ShowUsageAsync(CommandContext context, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync("asset: missing subcommand", ct);
            await context.Stderr.WriteLineAsync("Usage: asset <subcommand> [arguments]", ct);
            await context.Stderr.WriteLineAsync("Subcommands: list, info, unload, providers", ct);
            return ExitCode.UsageError;
        }

        private async Task<ExitCode> UnknownSubCommandAsync(CommandContext context, string subCommand, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"asset: unknown subcommand '{subCommand}'", ct);
            await context.Stderr.WriteLineAsync("Subcommands: list, info, unload, providers", ct);
            return ExitCode.UsageError;
        }

        #endregion

        #region Completion Helpers

        private static IEnumerable<string> GetSubCommandCompletions(string token)
        {
            var subCommands = new[] { "list", "info", "unload", "providers" };
            return subCommands.Where(cmd => cmd.StartsWith(token, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> GetAssetCompletions(string token)
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

        #endregion
    }
}
