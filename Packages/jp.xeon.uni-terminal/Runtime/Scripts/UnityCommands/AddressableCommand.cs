#if UNITY_ADDRESSABLES
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xeon.UniTerminal.Assets;

namespace Xeon.UniTerminal.UnityCommands
{
    /// <summary>
    /// Addressables経由でアセットを管理するコマンド
    /// </summary>
    [Command("adr", "Load, release, and find assets via Addressables")]
    public class AddressableCommand : ICommand
    {
        #region Options

        [Option("type", "t", Description = "Filter by asset type")]
        public string TypeFilter;

        [Option("long", "l", Description = "Show detailed information")]
        public bool LongFormat;

        #endregion

        #region Provider

        private static AddressableAssetProvider provider;

        private static AddressableAssetProvider Provider
        {
            get
            {
                if (provider != null)
                    return provider;

                provider = new AddressableAssetProvider();
                AssetManager.Instance.RegisterProvider(provider);
                return provider;
            }
        }

        #endregion

        #region ICommand

        public string CommandName => "adr";
        public string Description => "Load, release, and find assets via Addressables";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
                return await ShowUsageAsync(context, ct);

            var subCommand = context.PositionalArguments[0].ToLower();
            var args = context.PositionalArguments.Skip(1).ToList();

            return subCommand switch
            {
                "load" => await LoadAsync(context, args, ct),
                "release" => await ReleaseAsync(context, args, ct),
                "find" => await FindAsync(context, args, ct),
                "list" => await ListAsync(context, ct),
                "labels" => await LabelsAsync(context, ct),
                _ => await UnknownSubCommandAsync(context, subCommand, ct)
            };
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";

            if (context.TokenIndex == 1)
                return GetSubCommandCompletions(token);

            var tokens = context.InputLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 2 && tokens[1].ToLower() == "release" && context.TokenIndex == 2)
                return GetReleaseCompletions(token);

            return Enumerable.Empty<string>();
        }

        #endregion

        #region Subcommands

        private async Task<ExitCode> LoadAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("adr load: usage: adr load <key>", ct);
                await context.Stderr.WriteLineAsync("  Example: adr load player_texture", ct);
                return ExitCode.UsageError;
            }

            var assetType = ResolveAssetType();
            if (!string.IsNullOrEmpty(TypeFilter) && assetType == null)
            {
                await context.Stderr.WriteLineAsync($"adr: unknown type '{TypeFilter}'", ct);
                return ExitCode.UsageError;
            }

            try
            {
                var entry = await AssetManager.Instance.LoadAsync(
                    Provider, args[0], assetType ?? typeof(UnityEngine.Object), ct);

                if (entry == null)
                {
                    await context.Stderr.WriteLineAsync($"adr: failed to load '{args[0]}'", ct);
                    return ExitCode.RuntimeError;
                }

                await context.Stdout.WriteLineAsync($"Loaded: {entry.Name} #{entry.InstanceId} ({entry.AssetType.Name})", ct);
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync($"adr: error loading '{args[0]}': {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
        }

        private async Task<ExitCode> ReleaseAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("adr release: usage: adr release <specifier>", ct);
                await context.Stderr.WriteLineAsync("  specifier: #instanceId, asset name, or key", ct);
                return ExitCode.UsageError;
            }

            var specifier = args[0];
            var manager = AssetManager.Instance;

            if (!manager.Registry.TryResolve(specifier, out var entry))
            {
                await context.Stderr.WriteLineAsync($"adr: '{specifier}' not found", ct);
                return ExitCode.RuntimeError;
            }

            if (entry.ProviderName != Provider.ProviderName)
            {
                await context.Stderr.WriteLineAsync($"adr: '{specifier}' was not loaded via Addressables", ct);
                return ExitCode.UsageError;
            }

            var name = entry.Name;
            var instanceId = entry.InstanceId;

            if (!manager.Unload(specifier))
            {
                await context.Stderr.WriteLineAsync($"adr: failed to release '{specifier}'", ct);
                return ExitCode.RuntimeError;
            }

            await context.Stdout.WriteLineAsync($"Released: {name} #{instanceId}", ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> FindAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            var assetType = ResolveAssetType();
            if (!string.IsNullOrEmpty(TypeFilter) && assetType == null)
            {
                await context.Stderr.WriteLineAsync($"adr: unknown type '{TypeFilter}'", ct);
                return ExitCode.UsageError;
            }

            var pattern = args.Count > 0 ? string.Join(" ", args) : "*";
            var results = Provider.Find(pattern, assetType).ToList();

            if (results.Count == 0)
            {
                await context.Stdout.WriteLineAsync("No addressable assets found.", ct);
                return ExitCode.Success;
            }

            await context.Stdout.WriteLineAsync($"Found {results.Count} addressable assets:", ct);
            await WriteAssetList(context, results, ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> ListAsync(CommandContext context, CancellationToken ct)
        {
            var assetType = ResolveAssetType();
            if (!string.IsNullOrEmpty(TypeFilter) && assetType == null)
            {
                await context.Stderr.WriteLineAsync($"adr: unknown type '{TypeFilter}'", ct);
                return ExitCode.UsageError;
            }

            var results = Provider.List(null, assetType).ToList();

            if (results.Count == 0)
            {
                await context.Stdout.WriteLineAsync("No addressable assets registered.", ct);
                return ExitCode.Success;
            }

            await context.Stdout.WriteLineAsync($"Addressable assets ({results.Count}):", ct);
            await WriteAssetList(context, results, ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> LabelsAsync(CommandContext context, CancellationToken ct)
        {
            var labels = Provider.GetLabels().ToList();

            if (labels.Count == 0)
            {
                await context.Stdout.WriteLineAsync("No labels found.", ct);
                return ExitCode.Success;
            }

            await context.Stdout.WriteLineAsync($"Addressable labels ({labels.Count}):", ct);
            foreach (var label in labels)
            {
                ct.ThrowIfCancellationRequested();
                await context.Stdout.WriteLineAsync($"  {label}", ct);
            }

            return ExitCode.Success;
        }

        #endregion

        #region Helpers

        private Type ResolveAssetType()
        {
            return string.IsNullOrEmpty(TypeFilter) ? null : TypeResolver.ResolveAssetType(TypeFilter);
        }

        private async Task WriteAssetList(CommandContext context, List<AssetInfo> results, CancellationToken ct)
        {
            foreach (var info in results)
            {
                ct.ThrowIfCancellationRequested();
                var line = LongFormat
                    ? $"  {info.AssetType?.Name,-20} {info.Key}"
                    : $"  {info.Key}";
                await context.Stdout.WriteLineAsync(line, ct);
            }
        }

        #endregion

        #region Output Helpers

        private async Task<ExitCode> ShowUsageAsync(CommandContext context, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync("adr: missing subcommand", ct);
            await context.Stderr.WriteLineAsync("Usage: adr <subcommand> [arguments]", ct);
            await context.Stderr.WriteLineAsync("Subcommands: load, release, find, list, labels", ct);
            return ExitCode.UsageError;
        }

        private async Task<ExitCode> UnknownSubCommandAsync(CommandContext context, string subCommand, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"adr: unknown subcommand '{subCommand}'", ct);
            await context.Stderr.WriteLineAsync("Subcommands: load, release, find, list, labels", ct);
            return ExitCode.UsageError;
        }

        #endregion

        #region Completion Helpers

        private static IEnumerable<string> GetSubCommandCompletions(string token)
        {
            var subCommands = new[] { "load", "release", "find", "list", "labels" };
            return subCommands.Where(cmd => cmd.StartsWith(token, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> GetReleaseCompletions(string token)
        {
            var registry = AssetManager.Instance.Registry;
            foreach (var entry in registry.GetAll().Where(e => e.ProviderName == Provider.ProviderName))
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
#endif
