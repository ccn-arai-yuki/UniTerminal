#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Xeon.UniTerminal.Assets;

namespace Xeon.UniTerminal.UnityCommands
{
    /// <summary>
    /// AssetDatabase経由でアセットを管理するコマンド（エディタ専用）
    /// </summary>
    [Command("assetdb", "Load and find assets via AssetDatabase (Editor only)")]
    public class AssetDbCommand : ICommand
    {
        #region Options

        [Option("type", "t", Description = "Filter by asset type")]
        public string TypeFilter;

        [Option("long", "l", Description = "Show detailed information")]
        public bool LongFormat;

        #endregion

        #region Provider

        private static AssetDatabaseProvider provider;

        private static AssetDatabaseProvider Provider
        {
            get
            {
                if (provider != null)
                    return provider;

                provider = new AssetDatabaseProvider();
                AssetManager.Instance.RegisterProvider(provider);
                return provider;
            }
        }

        #endregion

        #region ICommand

        public string CommandName => "assetdb";
        public string Description => "Load and find assets via AssetDatabase (Editor only)";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (!Application.isEditor)
            {
                await context.Stderr.WriteLineAsync("assetdb: only available in Editor", ct);
                return ExitCode.RuntimeError;
            }

            if (context.PositionalArguments.Count == 0)
                return await ShowUsageAsync(context, ct);

            var subCommand = context.PositionalArguments[0].ToLower();
            var args = context.PositionalArguments.Skip(1).ToList();

            return subCommand switch
            {
                "load" => await LoadAsync(context, args, ct),
                "find" => await FindAsync(context, args, ct),
                "list" => await ListAsync(context, args, ct),
                "path" => await PathAsync(context, args, ct),
                _ => await UnknownSubCommandAsync(context, subCommand, ct)
            };
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";

            if (context.TokenIndex == 1)
                return GetSubCommandCompletions(token);

            if (context.TokenIndex == 2 && (token.StartsWith("Assets") || token == ""))
                return GetAssetPathCompletions(token);

            return Enumerable.Empty<string>();
        }

        #endregion

        #region Subcommands

        private async Task<ExitCode> LoadAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("assetdb load: usage: assetdb load <asset-path>", ct);
                await context.Stderr.WriteLineAsync("  Example: assetdb load Assets/Textures/icon.png", ct);
                return ExitCode.UsageError;
            }

            var assetType = ResolveAssetType();
            if (!string.IsNullOrEmpty(TypeFilter) && assetType == null)
            {
                await context.Stderr.WriteLineAsync($"assetdb: unknown type '{TypeFilter}'", ct);
                return ExitCode.UsageError;
            }

            var entry = await AssetManager.Instance.LoadAsync(
                Provider, args[0], assetType ?? typeof(UnityEngine.Object), ct);

            if (entry == null)
            {
                await context.Stderr.WriteLineAsync($"assetdb: failed to load '{args[0]}'", ct);
                return ExitCode.RuntimeError;
            }

            await context.Stdout.WriteLineAsync($"Loaded: {entry.Name} #{entry.InstanceId} ({entry.AssetType.Name})", ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> FindAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("assetdb find: usage: assetdb find <search-filter>", ct);
                await context.Stderr.WriteLineAsync("  Example: assetdb find \"t:Texture2D icon\"", ct);
                await context.Stderr.WriteLineAsync("  Example: assetdb find -t Texture2D icon", ct);
                return ExitCode.UsageError;
            }

            var assetType = ResolveAssetType();
            if (!string.IsNullOrEmpty(TypeFilter) && assetType == null)
            {
                await context.Stderr.WriteLineAsync($"assetdb: unknown type '{TypeFilter}'", ct);
                return ExitCode.UsageError;
            }

            var pattern = string.Join(" ", args);
            var results = Provider.Find(pattern, assetType).ToList();

            if (results.Count == 0)
            {
                await context.Stdout.WriteLineAsync("No assets found.", ct);
                return ExitCode.Success;
            }

            await context.Stdout.WriteLineAsync($"Found {results.Count} assets:", ct);
            await WriteAssetList(context, results, ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> ListAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            var assetType = ResolveAssetType();
            if (!string.IsNullOrEmpty(TypeFilter) && assetType == null)
            {
                await context.Stderr.WriteLineAsync($"assetdb: unknown type '{TypeFilter}'", ct);
                return ExitCode.UsageError;
            }

            var path = args.Count > 0 ? args[0] : "Assets";
            var results = Provider.List(path, assetType).ToList();

            if (results.Count == 0)
            {
                await context.Stdout.WriteLineAsync($"No assets in '{path}'.", ct);
                return ExitCode.Success;
            }

            await context.Stdout.WriteLineAsync($"Assets in '{path}' ({results.Count}):", ct);
            await WriteAssetList(context, results, ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> PathAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("assetdb path: usage: assetdb path <specifier>", ct);
                await context.Stderr.WriteLineAsync("  specifier: #instanceId or asset name", ct);
                return ExitCode.UsageError;
            }

            var specifier = args[0];

            // インスタンスID指定
            if (specifier.StartsWith("#") && int.TryParse(specifier.Substring(1), out int instanceId))
            {
                var path = Provider.GetAssetPathFromInstanceId(instanceId);
                if (string.IsNullOrEmpty(path))
                {
                    await context.Stderr.WriteLineAsync($"assetdb: no asset path for '{specifier}'", ct);
                    return ExitCode.RuntimeError;
                }

                await context.Stdout.WriteLineAsync(path, ct);
                return ExitCode.Success;
            }

            // レジストリから検索
            if (AssetManager.Instance.Registry.TryResolve(specifier, out var entry))
            {
                var path = !string.IsNullOrEmpty(entry.Key)
                    ? entry.Key
                    : Provider.GetAssetPathFromInstanceId(entry.InstanceId);

                if (!string.IsNullOrEmpty(path))
                {
                    await context.Stdout.WriteLineAsync(path, ct);
                    return ExitCode.Success;
                }
            }

            await context.Stderr.WriteLineAsync($"assetdb: '{specifier}' not found or no path available", ct);
            return ExitCode.RuntimeError;
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

                if (LongFormat)
                {
                    var size = info.Size > 0 ? FormatSize(info.Size) : "-";
                    var displayPath = !string.IsNullOrEmpty(info.Path) ? info.Path : info.Name;
                    await context.Stdout.WriteLineAsync($"  {info.AssetType?.Name,-20} {size,10} {displayPath}", ct);
                }
                else
                {
                    var displayPath = !string.IsNullOrEmpty(info.Path) ? info.Path : info.Name;
                    await context.Stdout.WriteLineAsync($"  {displayPath}", ct);
                }
            }
        }

        private static string FormatSize(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes < KB) return $"{bytes} B";
            if (bytes < MB) return $"{bytes / (double)KB:F1} KB";
            if (bytes < GB) return $"{bytes / (double)MB:F1} MB";
            return $"{bytes / (double)GB:F1} GB";
        }

        #endregion

        #region Output Helpers

        private async Task<ExitCode> ShowUsageAsync(CommandContext context, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync("assetdb: missing subcommand", ct);
            await context.Stderr.WriteLineAsync("Usage: assetdb <subcommand> [arguments]", ct);
            await context.Stderr.WriteLineAsync("Subcommands: load, find, list, path", ct);
            return ExitCode.UsageError;
        }

        private async Task<ExitCode> UnknownSubCommandAsync(CommandContext context, string subCommand, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"assetdb: unknown subcommand '{subCommand}'", ct);
            await context.Stderr.WriteLineAsync("Subcommands: load, find, list, path", ct);
            return ExitCode.UsageError;
        }

        #endregion

        #region Completion Helpers

        private static IEnumerable<string> GetSubCommandCompletions(string token)
        {
            var subCommands = new[] { "load", "find", "list", "path" };
            return subCommands.Where(cmd => cmd.StartsWith(token, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> GetAssetPathCompletions(string token)
        {
            var searchPath = string.IsNullOrEmpty(token) ? "Assets" : token;

            // フォルダ補完
            if (AssetDatabase.IsValidFolder(searchPath))
            {
                foreach (var folder in AssetDatabase.GetSubFolders(searchPath))
                {
                    if (folder.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                        yield return folder;
                }
            }

            // アセット補完 (上限50件)
            var guids = AssetDatabase.FindAssets("", new[] { "Assets" });
            foreach (var guid in guids.Take(50))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                    yield return path;
            }
        }

        #endregion
    }
}
#endif
