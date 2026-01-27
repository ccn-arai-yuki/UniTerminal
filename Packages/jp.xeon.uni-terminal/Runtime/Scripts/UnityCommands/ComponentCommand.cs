using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Xeon.UniTerminal.UnityCommands
{
    /// <summary>
    /// GameObjectのコンポーネントを管理するコマンド
    /// </summary>
    [Command("component", "Manage GameObject components (list, add, remove, info, enable, disable)")]
    public class ComponentCommand : ICommand
    {
        #region Options

        [Option("all", "a", Description = "Include all / remove all matching components")]
        public bool All;

        [Option("verbose", "v", Description = "Show verbose output (full type names)")]
        public bool Verbose;

        [Option("immediate", "i", Description = "Use DestroyImmediate for removal")]
        public bool Immediate;

        [Option("namespace", "n", Description = "Component namespace for type resolution")]
        public string Namespace;

        #endregion

        #region ICommand

        public string CommandName => "component";
        public string Description => "Manage GameObject components";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
                return await ShowUsageAsync(context, ct);

            var subCommand = context.PositionalArguments[0].ToLower();
            var args = context.PositionalArguments.Skip(1).ToList();

            return subCommand switch
            {
                "list" => await ListAsync(context, args, ct),
                "add" => await AddAsync(context, args, ct),
                "remove" => await RemoveAsync(context, args, ct),
                "info" => await InfoAsync(context, args, ct),
                "enable" => await SetEnabledAsync(context, args, true, ct),
                "disable" => await SetEnabledAsync(context, args, false, ct),
                _ => await UnknownSubCommandAsync(context, subCommand, ct)
            };
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";
            var tokens = context.InputLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (context.TokenIndex == 1)
                return GetSubCommandCompletions(token);

            if (context.TokenIndex == 2 && !token.StartsWith("-"))
                return GameObjectPath.GetCompletions(token);

            if (context.TokenIndex == 3 && tokens.Length >= 3)
                return GetComponentCompletions(tokens, token, context.InputLine);

            return Enumerable.Empty<string>();
        }

        #endregion

        #region Subcommands

        private async Task<ExitCode> ListAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("component list: missing path", ct);
                return ExitCode.UsageError;
            }

            var go = GameObjectPath.Resolve(args[0]);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{args[0]}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            var components = go.GetComponents<Component>();
            await context.Stdout.WriteLineAsync($"Components on {go.name} ({components.Length}):", ct);

            for (int i = 0; i < components.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                await WriteComponentListEntry(context, components[i], i, ct);
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> AddAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count < 2)
            {
                await context.Stderr.WriteLineAsync("component add: usage: component add <path> <type>", ct);
                return ExitCode.UsageError;
            }

            var go = GameObjectPath.Resolve(args[0]);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{args[0]}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            var type = TypeResolver.ResolveComponentType(args[1], Namespace);
            if (type == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{args[1]}': Component type not found", ct);
                return ExitCode.RuntimeError;
            }

            try
            {
#if UNITY_EDITOR
                UnityEditor.Undo.AddComponent(go, type);
#else
                go.AddComponent(type);
#endif
                await context.Stdout.WriteLineAsync($"Added: {type.Name} to {GameObjectPath.GetPath(go)}", ct);
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync($"component: Cannot add '{type.Name}' to '{go.name}': {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
        }

        private async Task<ExitCode> RemoveAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count < 2)
            {
                await context.Stderr.WriteLineAsync("component remove: usage: component remove <path> <type|index>", ct);
                return ExitCode.UsageError;
            }

            var go = GameObjectPath.Resolve(args[0]);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{args[0]}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            var identifier = args[1];
            var components = go.GetComponents<Component>();

            // インデックス指定
            if (int.TryParse(identifier, out int index))
                return await RemoveByIndexAsync(context, go, components, index, ct);

            // 型名指定
            return await RemoveByTypeAsync(context, go, identifier, ct);
        }

        private async Task<ExitCode> InfoAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count < 2)
            {
                await context.Stderr.WriteLineAsync("component info: usage: component info <path> <type|index>", ct);
                return ExitCode.UsageError;
            }

            var go = GameObjectPath.Resolve(args[0]);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{args[0]}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            var target = ResolveComponent(go, args[1]);
            if (target == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{args[1]}' not found on {go.name}", ct);
                return ExitCode.RuntimeError;
            }

            await WriteComponentInfo(context, go, target, ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> SetEnabledAsync(CommandContext context, List<string> args, bool enabled, CancellationToken ct)
        {
            var cmd = enabled ? "enable" : "disable";

            if (args.Count < 2)
            {
                await context.Stderr.WriteLineAsync($"component {cmd}: usage: component {cmd} <path> <type|index>", ct);
                return ExitCode.UsageError;
            }

            var go = GameObjectPath.Resolve(args[0]);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{args[0]}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            var target = ResolveComponent(go, args[1]);
            if (target == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{args[1]}' not found on {go.name}", ct);
                return ExitCode.RuntimeError;
            }

            return await TrySetComponentEnabled(context, target, enabled, ct);
        }

        #endregion

        #region Remove Helpers

        private async Task<ExitCode> RemoveByIndexAsync(
            CommandContext context, GameObject go, Component[] components, int index, CancellationToken ct)
        {
            if (index < 0 || index >= components.Length)
            {
                await context.Stderr.WriteLineAsync($"component: Index {index} out of range (0-{components.Length - 1})", ct);
                return ExitCode.UsageError;
            }

            var comp = components[index];
            if (comp == null)
            {
                await context.Stderr.WriteLineAsync($"component: Component at index {index} is missing", ct);
                return ExitCode.RuntimeError;
            }

            if (comp is Transform)
            {
                await context.Stderr.WriteLineAsync("component: Cannot remove Transform component", ct);
                return ExitCode.RuntimeError;
            }

            return await DestroyComponentAsync(context, go, comp, ct);
        }

        private async Task<ExitCode> RemoveByTypeAsync(
            CommandContext context, GameObject go, string typeName, CancellationToken ct)
        {
            var type = TypeResolver.ResolveComponentType(typeName, Namespace);
            if (type == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{typeName}': Component type not found", ct);
                return ExitCode.RuntimeError;
            }

            if (type == typeof(Transform))
            {
                await context.Stderr.WriteLineAsync("component: Cannot remove Transform component", ct);
                return ExitCode.RuntimeError;
            }

            var toRemove = All
                ? go.GetComponents(type).Where(c => c != null).ToList()
                : new List<Component> { go.GetComponent(type) }.Where(c => c != null).ToList();

            if (toRemove.Count == 0)
            {
                await context.Stderr.WriteLineAsync($"component: '{typeName}' not found on {go.name}", ct);
                return ExitCode.RuntimeError;
            }

            foreach (var comp in toRemove)
            {
                await DestroyComponentAsync(context, go, comp, ct);
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> DestroyComponentAsync(CommandContext context, GameObject go, Component comp, CancellationToken ct)
        {
            var typeName = comp.GetType().Name;

#if UNITY_EDITOR
            if (Immediate)
                Object.DestroyImmediate(comp);
            else
                UnityEditor.Undo.DestroyObjectImmediate(comp);
#else
            if (Immediate)
                Object.DestroyImmediate(comp);
            else
                Object.Destroy(comp);
#endif

            await context.Stdout.WriteLineAsync($"Removed: {typeName} from {GameObjectPath.GetPath(go)}", ct);
            return ExitCode.Success;
        }

        #endregion

        #region Enable/Disable Helpers

        private async Task<ExitCode> TrySetComponentEnabled(CommandContext context, Component target, bool enabled, CancellationToken ct)
        {
            var stateStr = enabled ? "enabled" : "disabled";

#if UNITY_EDITOR
            var undoName = enabled ? "Enable Component" : "Disable Component";
#endif

            switch (target)
            {
                case Behaviour behaviour:
#if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(behaviour, undoName);
#endif
                    behaviour.enabled = enabled;
                    await context.Stdout.WriteLineAsync($"{target.GetType().Name}: {stateStr}", ct);
                    return ExitCode.Success;

                case Collider collider:
#if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(collider, undoName);
#endif
                    collider.enabled = enabled;
                    await context.Stdout.WriteLineAsync($"{target.GetType().Name}: {stateStr}", ct);
                    return ExitCode.Success;

                case Renderer renderer:
#if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(renderer, undoName);
#endif
                    renderer.enabled = enabled;
                    await context.Stdout.WriteLineAsync($"{target.GetType().Name}: {stateStr}", ct);
                    return ExitCode.Success;

                default:
                    await context.Stderr.WriteLineAsync($"component: '{target.GetType().Name}' does not support enable/disable", ct);
                    return ExitCode.RuntimeError;
            }
        }

        #endregion

        #region Component Resolution

        private Component ResolveComponent(GameObject go, string identifier)
        {
            // インデックス指定
            if (int.TryParse(identifier, out int index))
            {
                var components = go.GetComponents<Component>();
                if (index >= 0 && index < components.Length)
                    return components[index];
                return null;
            }

            // 型名指定
            var type = TypeResolver.ResolveComponentType(identifier, Namespace);
            if (type == null)
                return null;

            return go.GetComponent(type);
        }

        #endregion

        #region Output Helpers

        private async Task WriteComponentListEntry(CommandContext context, Component comp, int index, CancellationToken ct)
        {
            if (comp == null)
            {
                await context.Stdout.WriteLineAsync($"  [{index}] (Missing Script)", ct);
                return;
            }

            var type = comp.GetType();
            var enabledStr = comp is Behaviour behaviour
                ? (behaviour.enabled ? " (enabled)" : " (disabled)")
                : "";

            if (Verbose)
                await context.Stdout.WriteLineAsync($"  [{index}] {type.Name,-30}{enabledStr} {type.FullName}", ct);
            else
                await context.Stdout.WriteLineAsync($"  [{index}] {type.Name}{enabledStr}", ct);
        }

        private async Task WriteComponentInfo(CommandContext context, GameObject go, Component target, CancellationToken ct)
        {
            var targetType = target.GetType();

            await context.Stdout.WriteLineAsync($"Component: {targetType.Name}", ct);
            await context.Stdout.WriteLineAsync($"  Type: {targetType.FullName}", ct);
            await context.Stdout.WriteLineAsync($"  GameObject: {GameObjectPath.GetPath(go)}", ct);

            if (target is Behaviour behaviour)
                await context.Stdout.WriteLineAsync($"  Enabled: {behaviour.enabled}", ct);

            await context.Stdout.WriteLineAsync("  Properties:", ct);

            var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !p.GetIndexParameters().Any())
                .OrderBy(p => p.Name)
                .Take(20);

            foreach (var prop in properties)
            {
                ct.ThrowIfCancellationRequested();
                await WritePropertyValue(context, target, prop, ct);
            }
        }

        private async Task WritePropertyValue(CommandContext context, Component target, PropertyInfo prop, CancellationToken ct)
        {
            try
            {
                var value = prop.GetValue(target);
                var valueStr = FormatValue(value);
                await context.Stdout.WriteLineAsync($"    {prop.Name}: {valueStr} ({prop.PropertyType.Name})", ct);
            }
            catch
            {
                // プロパティ取得エラーは無視
            }
        }

        private async Task<ExitCode> ShowUsageAsync(CommandContext context, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync("component: missing subcommand", ct);
            await context.Stderr.WriteLineAsync("Usage: component <subcommand> <path> [arguments]", ct);
            await context.Stderr.WriteLineAsync("Subcommands: list, add, remove, info, enable, disable", ct);
            return ExitCode.UsageError;
        }

        private async Task<ExitCode> UnknownSubCommandAsync(CommandContext context, string subCommand, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"component: unknown subcommand '{subCommand}'", ct);
            await context.Stderr.WriteLineAsync("Subcommands: list, add, remove, info, enable, disable", ct);
            return ExitCode.UsageError;
        }

        #endregion

        #region Value Formatting

        private string FormatValue(object value)
        {
            return value switch
            {
                null => "null",
                string s => $"\"{s}\"",
                Vector3 v3 => $"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})",
                Vector2 v2 => $"({v2.x:F2}, {v2.y:F2})",
                Quaternion q => $"({q.eulerAngles.x:F1}, {q.eulerAngles.y:F1}, {q.eulerAngles.z:F1})",
                Color c => $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})",
                bool b => b.ToString().ToLower(),
                float f => f.ToString("F2"),
                double d => d.ToString("F2"),
                UnityEngine.Object obj => obj != null ? obj.name : "null",
                _ => value.ToString()
            };
        }

        #endregion

        #region Completion Helpers

        private static IEnumerable<string> GetSubCommandCompletions(string token)
        {
            var subCommands = new[] { "list", "add", "remove", "info", "enable", "disable" };
            return subCommands.Where(cmd => cmd.StartsWith(token, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<string> GetComponentCompletions(string[] tokens, string token, string inputLine)
        {
            var inputLower = inputLine?.ToLower() ?? "";

            // addサブコマンドの場合は追加可能なコンポーネント
            if (inputLower.Contains(" add "))
                return TypeResolver.GetCommonComponentNames()
                    .Where(t => t.StartsWith(token, StringComparison.OrdinalIgnoreCase));

            // remove/info/enable/disableサブコマンドの場合は既存のコンポーネント
            if (inputLower.Contains(" remove ") || inputLower.Contains(" info ") ||
                inputLower.Contains(" enable ") || inputLower.Contains(" disable "))
            {
                var go = GameObjectPath.Resolve(tokens[2]);
                if (go == null)
                    return Enumerable.Empty<string>();

                return go.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .Distinct()
                    .Where(t => t.StartsWith(token, StringComparison.OrdinalIgnoreCase));
            }

            return Enumerable.Empty<string>();
        }

        #endregion
    }
}
