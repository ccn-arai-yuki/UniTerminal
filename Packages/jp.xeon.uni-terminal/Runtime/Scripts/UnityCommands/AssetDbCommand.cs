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
    /// AssetDatabase経由でアセットを管理するコマンド（エディタ専用）。
    /// </summary>
    [Command("assetdb", "Load and find assets via AssetDatabase (Editor only)")]
    public class AssetDbCommand : ICommand
    {
        [Option("type", "t", Description = "Filter by asset type")]
        public string TypeFilter;

        [Option("long", "l", Description = "Show detailed information")]
        public bool LongFormat;

        private static AssetDatabaseProvider provider;

        private static AssetDatabaseProvider Provider
        {
            get
            {
                if (provider == null)
                {
                    provider = new AssetDatabaseProvider();
                    AssetManager.Instance.RegisterProvider(provider);
                }
                return provider;
            }
        }

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
            {
                await context.Stderr.WriteLineAsync("assetdb: missing subcommand", ct);
                await context.Stderr.WriteLineAsync("Usage: assetdb <subcommand> [arguments]", ct);
                await context.Stderr.WriteLineAsync("Subcommands: load, find, list, path", ct);
                return ExitCode.UsageError;
            }

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

        private async Task<ExitCode> LoadAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("assetdb load: usage: assetdb load <asset-path>", ct);
                await context.Stderr.WriteLineAsync("  Example: assetdb load Assets/Textures/icon.png", ct);
                return ExitCode.UsageError;
            }

            var assetPath = args[0];
            Type assetType = typeof(UnityEngine.Object);

            if (!string.IsNullOrEmpty(TypeFilter))
            {
                assetType = TypeResolver.ResolveAssetType(TypeFilter);
                if (assetType == null)
                {
                    await context.Stderr.WriteLineAsync($"assetdb: unknown type '{TypeFilter}'", ct);
                    return ExitCode.UsageError;
                }
            }

            var entry = await AssetManager.Instance.LoadAsync(Provider, assetPath, assetType, ct);

            if (entry == null)
            {
                await context.Stderr.WriteLineAsync($"assetdb: failed to load '{assetPath}'", ct);
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

            var pattern = string.Join(" ", args);
            Type assetType = null;

            if (!string.IsNullOrEmpty(TypeFilter))
            {
                assetType = TypeResolver.ResolveAssetType(TypeFilter);
                if (assetType == null)
                {
                    await context.Stderr.WriteLineAsync($"assetdb: unknown type '{TypeFilter}'", ct);
                    return ExitCode.UsageError;
                }
            }

            var results = Provider.Find(pattern, assetType).ToList();

            if (results.Count == 0)
            {
                await context.Stdout.WriteLineAsync("No assets found.", ct);
                return ExitCode.Success;
            }

            await context.Stdout.WriteLineAsync($"Found {results.Count} assets:", ct);

            foreach (var info in results)
            {
                ct.ThrowIfCancellationRequested();

                if (LongFormat)
                {
                    var size = info.Size > 0 ? FormatSize(info.Size) : "-";
                    await context.Stdout.WriteLineAsync($"  {info.AssetType?.Name,-20} {size,10} {info.Path}", ct);
                }
                else
                {
                    await context.Stdout.WriteLineAsync($"  {info.Path}", ct);
                }
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> ListAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            var path = args.Count > 0 ? args[0] : "Assets";
            Type assetType = null;

            if (!string.IsNullOrEmpty(TypeFilter))
            {
                assetType = TypeResolver.ResolveAssetType(TypeFilter);
                if (assetType == null)
                {
                    await context.Stderr.WriteLineAsync($"assetdb: unknown type '{TypeFilter}'", ct);
                    return ExitCode.UsageError;
                }
            }

            var results = Provider.List(path, assetType).ToList();

            if (results.Count == 0)
            {
                await context.Stdout.WriteLineAsync($"No assets in '{path}'.", ct);
                return ExitCode.Success;
            }

            await context.Stdout.WriteLineAsync($"Assets in '{path}' ({results.Count}):", ct);

            foreach (var info in results)
            {
                ct.ThrowIfCancellationRequested();

                if (LongFormat)
                {
                    var size = info.Size > 0 ? FormatSize(info.Size) : "-";
                    await context.Stdout.WriteLineAsync($"  {info.AssetType?.Name,-20} {size,10} {info.Name}", ct);
                }
                else
                {
                    await context.Stdout.WriteLineAsync($"  {info.Name}", ct);
                }
            }

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
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    await context.Stdout.WriteLineAsync(entry.Key, ct);
                    return ExitCode.Success;
                }

                var path = Provider.GetAssetPathFromInstanceId(entry.InstanceId);
                if (!string.IsNullOrEmpty(path))
                {
                    await context.Stdout.WriteLineAsync(path, ct);
                    return ExitCode.Success;
                }
            }

            await context.Stderr.WriteLineAsync($"assetdb: '{specifier}' not found or no path available", ct);
            return ExitCode.RuntimeError;
        }

        private async Task<ExitCode> UnknownSubCommandAsync(
            CommandContext context,
            string subCommand,
            CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"assetdb: unknown subcommand '{subCommand}'", ct);
            await context.Stderr.WriteLineAsync("Subcommands: load, find, list, path", ct);
            return ExitCode.UsageError;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";

            if (context.TokenIndex == 1)
            {
                var subCommands = new[] { "load", "find", "list", "path" };
                foreach (var cmd in subCommands)
                {
                    if (cmd.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                        yield return cmd;
                }
                yield break;
            }

            // パス補完（Assets/で始まる場合）
            if (context.TokenIndex == 2 && (token.StartsWith("Assets") || token == ""))
            {
                var searchPath = string.IsNullOrEmpty(token) ? "Assets" : token;

                // フォルダ補完
                if (AssetDatabase.IsValidFolder(searchPath))
                {
                    var subFolders = AssetDatabase.GetSubFolders(searchPath);
                    foreach (var folder in subFolders)
                    {
                        if (folder.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                            yield return folder;
                    }
                }

                // アセット補完
                var guids = AssetDatabase.FindAssets("", new[] { "Assets" });
                foreach (var guid in guids.Take(50))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                        yield return path;
                }
            }
        }
    }
}
#endif
