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
    /// ロード済みアセットを管理するコマンド。
    /// </summary>
    [Command("asset", "Manage loaded assets (list, info, unload)")]
    public class AssetCommand : ICommand
    {
        [Option("type", "t", Description = "Filter by asset type")]
        public string TypeFilter;

        [Option("name", "n", Description = "Filter by name pattern (wildcards supported)")]
        public string NameFilter;

        [Option("long", "l", Description = "Show detailed information")]
        public bool LongFormat;

        public string CommandName => "asset";
        public string Description => "Manage loaded assets";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
            {
                await context.Stderr.WriteLineAsync("asset: missing subcommand", ct);
                await context.Stderr.WriteLineAsync("Usage: asset <subcommand> [arguments]", ct);
                await context.Stderr.WriteLineAsync("Subcommands: list, info, unload, providers", ct);
                return ExitCode.UsageError;
            }

            var subCommand = context.PositionalArguments[0].ToLower();
            var args = context.PositionalArguments.Skip(1).ToList();

            return subCommand switch
            {
                "list" => await ListAsync(context, args, ct),
                "info" => await InfoAsync(context, args, ct),
                "unload" => await UnloadAsync(context, args, ct),
                "providers" => await ProvidersAsync(context, ct),
                _ => await UnknownSubCommandAsync(context, subCommand, ct)
            };
        }

        private async Task<ExitCode> ListAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            var registry = AssetManager.Instance.Registry;
            Type assetType = null;

            if (!string.IsNullOrEmpty(TypeFilter))
            {
                assetType = TypeResolver.ResolveAssetType(TypeFilter);
                if (assetType == null)
                {
                    await context.Stderr.WriteLineAsync($"asset: unknown type '{TypeFilter}'", ct);
                    return ExitCode.UsageError;
                }
            }

            IEnumerable<LoadedAssetEntry> entries;
            if (!string.IsNullOrEmpty(NameFilter))
                entries = registry.Find(NameFilter, assetType);
            else
                entries = registry.GetAll(assetType);

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
                // 同名アセットが複数ある場合
                var byName = registry.GetByName(specifier);
                if (byName.Count > 1)
                {
                    await context.Stderr.WriteLineAsync($"asset: '{specifier}' matches multiple assets:", ct);
                    foreach (var e in byName)
                    {
                        await context.Stderr.WriteLineAsync($"  #{e.InstanceId} {e.Name} ({e.AssetType.Name})", ct);
                    }
                    await context.Stderr.WriteLineAsync("Use instance ID (#xxxxx) to specify.", ct);
                    return ExitCode.UsageError;
                }

                await context.Stderr.WriteLineAsync($"asset: '{specifier}' not found", ct);
                return ExitCode.RuntimeError;
            }

            await context.Stdout.WriteLineAsync($"Asset: {entry.Name}", ct);
            await context.Stdout.WriteLineAsync($"  Instance ID: #{entry.InstanceId}", ct);
            await context.Stdout.WriteLineAsync($"  Type:        {entry.AssetType.FullName}", ct);
            await context.Stdout.WriteLineAsync($"  Provider:    {entry.ProviderName}", ct);

            if (!string.IsNullOrEmpty(entry.Key))
                await context.Stdout.WriteLineAsync($"  Key:         {entry.Key}", ct);

            await context.Stdout.WriteLineAsync($"  Loaded at:   {entry.LoadedAt:yyyy-MM-dd HH:mm:ss}", ct);

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

            if (manager.Unload(specifier))
            {
                await context.Stdout.WriteLineAsync($"Unloaded: {name} #{instanceId}", ct);
                return ExitCode.Success;
            }

            await context.Stderr.WriteLineAsync($"asset: failed to unload '{specifier}'", ct);
            return ExitCode.RuntimeError;
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

        private async Task<ExitCode> UnknownSubCommandAsync(
            CommandContext context,
            string subCommand,
            CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"asset: unknown subcommand '{subCommand}'", ct);
            await context.Stderr.WriteLineAsync("Subcommands: list, info, unload, providers", ct);
            return ExitCode.UsageError;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";

            if (context.TokenIndex == 1)
            {
                var subCommands = new[] { "list", "info", "unload", "providers" };
                foreach (var cmd in subCommands)
                {
                    if (cmd.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                        yield return cmd;
                }
                yield break;
            }

            // info/unload の引数としてロード済みアセットを補完
            if (context.TokenIndex == 2)
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
