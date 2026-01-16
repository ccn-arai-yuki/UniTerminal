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
    /// GameObjectのコンポーネントを管理するコマンド。
    /// </summary>
    [Command("component", "Manage GameObject components (list, add, remove, info, enable, disable)")]
    public class ComponentCommand : ICommand
    {
        [Option("all", "a", Description = "Include all / remove all matching components")]
        public bool All;

        [Option("verbose", "v", Description = "Show verbose output (full type names)")]
        public bool Verbose;

        [Option("immediate", "i", Description = "Use DestroyImmediate for removal")]
        public bool Immediate;

        [Option("namespace", "n", Description = "Component namespace for type resolution")]
        public string Namespace;

        public string CommandName => "component";
        public string Description => "Manage GameObject components";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
            {
                await context.Stderr.WriteLineAsync("component: missing subcommand", ct);
                await context.Stderr.WriteLineAsync("Usage: component <subcommand> <path> [arguments]", ct);
                await context.Stderr.WriteLineAsync("Subcommands: list, add, remove, info, enable, disable", ct);
                return ExitCode.UsageError;
            }

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

        /// <summary>
        /// コンポーネント一覧を表示します。
        /// </summary>
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
                var comp = components[i];

                if (comp == null)
                {
                    await context.Stdout.WriteLineAsync($"  [{i}] (Missing Script)", ct);
                    continue;
                }

                var type = comp.GetType();
                string enabledStr = "";

                // Behaviourの場合はenabled状態を表示
                if (comp is Behaviour behaviour)
                {
                    enabledStr = behaviour.enabled ? " (enabled)" : " (disabled)";
                }

                if (Verbose)
                {
                    await context.Stdout.WriteLineAsync($"  [{i}] {type.Name,-30}{enabledStr} {type.FullName}", ct);
                }
                else
                {
                    await context.Stdout.WriteLineAsync($"  [{i}] {type.Name}{enabledStr}", ct);
                }
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// コンポーネントを追加します。
        /// </summary>
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

            var typeName = args[1];
            var type = TypeResolver.ResolveComponentType(typeName, Namespace);

            if (type == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{typeName}': Component type not found", ct);
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

        /// <summary>
        /// コンポーネントを削除します。
        /// </summary>
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

            // インデックス指定かどうか
            if (int.TryParse(identifier, out int index))
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

            // 型名指定
            var type = TypeResolver.ResolveComponentType(identifier, Namespace);
            if (type == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{identifier}': Component type not found", ct);
                return ExitCode.RuntimeError;
            }

            if (type == typeof(Transform))
            {
                await context.Stderr.WriteLineAsync("component: Cannot remove Transform component", ct);
                return ExitCode.RuntimeError;
            }

            var toRemove = All
                ? go.GetComponents(type).ToList()
                : new List<Component> { go.GetComponent(type) };

            toRemove = toRemove.Where(c => c != null).ToList();

            if (toRemove.Count == 0)
            {
                await context.Stderr.WriteLineAsync($"component: '{identifier}' not found on {go.name}", ct);
                return ExitCode.RuntimeError;
            }

            foreach (var comp in toRemove)
            {
                await DestroyComponentAsync(context, go, comp, ct);
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// コンポーネントを削除します。
        /// </summary>
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

        /// <summary>
        /// コンポーネントの詳細情報を表示します。
        /// </summary>
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

            var identifier = args[1];
            Component target;

            // インデックス指定
            if (int.TryParse(identifier, out int index))
            {
                var components = go.GetComponents<Component>();
                if (index < 0 || index >= components.Length)
                {
                    await context.Stderr.WriteLineAsync($"component: Index {index} out of range (0-{components.Length - 1})", ct);
                    return ExitCode.UsageError;
                }
                target = components[index];
            }
            else
            {
                // 型名指定
                var type = TypeResolver.ResolveComponentType(identifier, Namespace);
                if (type == null)
                {
                    await context.Stderr.WriteLineAsync($"component: '{identifier}': Component type not found", ct);
                    return ExitCode.RuntimeError;
                }
                target = go.GetComponent(type);
            }

            if (target == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{identifier}' not found on {go.name}", ct);
                return ExitCode.RuntimeError;
            }

            var targetType = target.GetType();

            await context.Stdout.WriteLineAsync($"Component: {targetType.Name}", ct);
            await context.Stdout.WriteLineAsync($"  Type: {targetType.FullName}", ct);
            await context.Stdout.WriteLineAsync($"  GameObject: {GameObjectPath.GetPath(go)}", ct);

            // Behaviourの場合はenabled状態を表示
            if (target is Behaviour behaviour)
            {
                await context.Stdout.WriteLineAsync($"  Enabled: {behaviour.enabled}", ct);
            }

            // プロパティを表示
            await context.Stdout.WriteLineAsync("  Properties:", ct);

            var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !p.GetIndexParameters().Any())
                .OrderBy(p => p.Name)
                .Take(20);

            foreach (var prop in properties)
            {
                ct.ThrowIfCancellationRequested();
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

            return ExitCode.Success;
        }

        /// <summary>
        /// コンポーネントの有効/無効を切り替えます。
        /// </summary>
        private async Task<ExitCode> SetEnabledAsync(CommandContext context, List<string> args, bool enabled, CancellationToken ct)
        {
            if (args.Count < 2)
            {
                var cmd = enabled ? "enable" : "disable";
                await context.Stderr.WriteLineAsync($"component {cmd}: usage: component {cmd} <path> <type|index>", ct);
                return ExitCode.UsageError;
            }

            var go = GameObjectPath.Resolve(args[0]);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{args[0]}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            var identifier = args[1];
            Component target;

            // インデックス指定
            if (int.TryParse(identifier, out int index))
            {
                var components = go.GetComponents<Component>();
                if (index < 0 || index >= components.Length)
                {
                    await context.Stderr.WriteLineAsync($"component: Index {index} out of range (0-{components.Length - 1})", ct);
                    return ExitCode.UsageError;
                }
                target = components[index];
            }
            else
            {
                // 型名指定
                var type = TypeResolver.ResolveComponentType(identifier, Namespace);
                if (type == null)
                {
                    await context.Stderr.WriteLineAsync($"component: '{identifier}': Component type not found", ct);
                    return ExitCode.RuntimeError;
                }
                target = go.GetComponent(type);
            }

            if (target == null)
            {
                await context.Stderr.WriteLineAsync($"component: '{identifier}' not found on {go.name}", ct);
                return ExitCode.RuntimeError;
            }

            // Behaviour, Collider, Renderer はそれぞれ enabled プロパティを持つ
            if (target is Behaviour behaviour)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(behaviour, enabled ? "Enable Component" : "Disable Component");
#endif
                behaviour.enabled = enabled;
                var state = enabled ? "enabled" : "disabled";
                await context.Stdout.WriteLineAsync($"{target.GetType().Name}: {state}", ct);
                return ExitCode.Success;
            }
            else if (target is Collider collider)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(collider, enabled ? "Enable Component" : "Disable Component");
#endif
                collider.enabled = enabled;
                var state = enabled ? "enabled" : "disabled";
                await context.Stdout.WriteLineAsync($"{target.GetType().Name}: {state}", ct);
                return ExitCode.Success;
            }
            else if (target is Renderer renderer)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(renderer, enabled ? "Enable Component" : "Disable Component");
#endif
                renderer.enabled = enabled;
                var state = enabled ? "enabled" : "disabled";
                await context.Stdout.WriteLineAsync($"{target.GetType().Name}: {state}", ct);
                return ExitCode.Success;
            }
            else
            {
                await context.Stderr.WriteLineAsync($"component: '{target.GetType().Name}' does not support enable/disable", ct);
                return ExitCode.RuntimeError;
            }
        }

        /// <summary>
        /// 不明なサブコマンドのエラーを表示します。
        /// </summary>
        private async Task<ExitCode> UnknownSubCommandAsync(CommandContext context, string subCommand, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"component: unknown subcommand '{subCommand}'", ct);
            await context.Stderr.WriteLineAsync("Subcommands: list, add, remove, info, enable, disable", ct);
            return ExitCode.UsageError;
        }

        /// <summary>
        /// 値を表示用にフォーマットします。
        /// </summary>
        private string FormatValue(object value)
        {
            if (value == null)
                return "null";

            if (value is string s)
                return $"\"{s}\"";

            if (value is Vector3 v3)
                return $"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})";

            if (value is Vector2 v2)
                return $"({v2.x:F2}, {v2.y:F2})";

            if (value is Quaternion q)
                return $"({q.eulerAngles.x:F1}, {q.eulerAngles.y:F1}, {q.eulerAngles.z:F1})";

            if (value is Color c)
                return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";

            if (value is bool b)
                return b.ToString().ToLower();

            if (value is float f)
                return f.ToString("F2");

            if (value is double d)
                return d.ToString("F2");

            if (value is UnityEngine.Object obj)
                return obj != null ? obj.name : "null";

            return value.ToString();
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";
            var tokens = context.InputLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // サブコマンド補完
            if (context.TokenIndex == 1)
            {
                var subCommands = new[] { "list", "add", "remove", "info", "enable", "disable" };
                foreach (var cmd in subCommands)
                {
                    if (cmd.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                        yield return cmd;
                }
                yield break;
            }

            // パス補完（サブコマンドの後の最初の引数）
            if (context.TokenIndex == 2 && !token.StartsWith("-"))
            {
                foreach (var path in GameObjectPath.GetCompletions(token))
                {
                    yield return path;
                }
                yield break;
            }

            // 型名補完（TokenIndex == 3）
            if (context.TokenIndex == 3 && tokens.Length >= 3)
            {
                var inputLower = context.InputLine?.ToLower() ?? "";

                // addサブコマンドの場合は追加可能なコンポーネント
                if (inputLower.Contains(" add "))
                {
                    foreach (var typeName in TypeResolver.GetCommonComponentNames())
                    {
                        if (typeName.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                            yield return typeName;
                    }
                    yield break;
                }

                // remove/info/enable/disableサブコマンドの場合は既存のコンポーネント
                if (inputLower.Contains(" remove ") || inputLower.Contains(" info ") ||
                    inputLower.Contains(" enable ") || inputLower.Contains(" disable "))
                {
                    var go = GameObjectPath.Resolve(tokens[2]);
                    if (go != null)
                    {
                        var components = go.GetComponents<Component>();
                        var seenTypes = new HashSet<string>();
                        foreach (var comp in components)
                        {
                            if (comp == null) continue;
                            var typeName = comp.GetType().Name;
                            if (seenTypes.Add(typeName) && typeName.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                            {
                                yield return typeName;
                            }
                        }
                    }
                }
            }
        }
    }
}
