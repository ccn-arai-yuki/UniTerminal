using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Xeon.UniTerminal.UnityCommands
{
    /// <summary>
    /// GameObjectの作成・削除・管理を行うコマンド。
    /// </summary>
    [Command("go", "Manage GameObjects (create, delete, find, rename, active, clone, info)")]
    public class GoCommand : ICommand
    {
        [Option("primitive", "p", Description = "Primitive type (Cube, Sphere, Capsule, Cylinder, Plane, Quad)")]
        public string Primitive;

        [Option("parent", "", Description = "Parent object path")]
        public string ParentPath;

        [Option("position", "", Description = "Initial position (x,y,z)")]
        public string Position;

        [Option("rotation", "", Description = "Initial rotation in euler angles (x,y,z)")]
        public string Rotation;

        [Option("tag", "t", Description = "Tag for find or create")]
        public string Tag;

        [Option("name", "n", Description = "Name pattern for find, or new name for clone/rename")]
        public string Name;

        [Option("component", "c", Description = "Component type for find")]
        public string Component;

        [Option("inactive", "i", Description = "Include inactive objects in find")]
        public bool IncludeInactive;

        [Option("set", "s", Description = "Set active state (true/false)")]
        public string SetActive;

        [Option("toggle", "", Description = "Toggle active state")]
        public bool Toggle;

        [Option("immediate", "", Description = "Use DestroyImmediate")]
        public bool Immediate;

        [Option("children", "", Description = "Delete children only")]
        public bool ChildrenOnly;

        [Option("count", "", Description = "Clone count")]
        public int CloneCount = 1;

        public string CommandName => "go";
        public string Description => "Manage GameObjects";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
            {
                await context.Stderr.WriteLineAsync("go: missing subcommand", ct);
                await context.Stderr.WriteLineAsync("Usage: go <subcommand> [options] [arguments]", ct);
                await context.Stderr.WriteLineAsync("Subcommands: create, delete, find, rename, active, clone, info", ct);
                return ExitCode.UsageError;
            }

            var subCommand = context.PositionalArguments[0].ToLower();
            var args = context.PositionalArguments.Skip(1).ToList();

            return subCommand switch
            {
                "create" => await CreateAsync(context, args, ct),
                "delete" => await DeleteAsync(context, args, ct),
                "find" => await FindAsync(context, ct),
                "rename" => await RenameAsync(context, args, ct),
                "active" => await ActiveAsync(context, args, ct),
                "clone" => await CloneAsync(context, args, ct),
                "info" => await InfoAsync(context, args, ct),
                _ => await UnknownSubCommandAsync(context, subCommand, ct)
            };
        }

        /// <summary>
        /// 新しいGameObjectを作成します。
        /// </summary>
        private async Task<ExitCode> CreateAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            string name = args.Count > 0 ? args[0] : "GameObject";

            GameObject go;

            // プリミティブ作成
            if (!string.IsNullOrEmpty(Primitive))
            {
                if (!Enum.TryParse<PrimitiveType>(Primitive, true, out var primitiveType))
                {
                    await context.Stderr.WriteLineAsync($"go: '{Primitive}': Invalid primitive type", ct);
                    await context.Stderr.WriteLineAsync("Valid types: Cube, Sphere, Capsule, Cylinder, Plane, Quad", ct);
                    return ExitCode.UsageError;
                }
                go = GameObject.CreatePrimitive(primitiveType);
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
            }

            // 親の設定
            if (!string.IsNullOrEmpty(ParentPath))
            {
                var parent = GameObjectPath.Resolve(ParentPath);
                if (parent == null)
                {
                    Object.DestroyImmediate(go);
                    await context.Stderr.WriteLineAsync($"go: '{ParentPath}': Parent not found", ct);
                    return ExitCode.RuntimeError;
                }
                go.transform.SetParent(parent.transform, false);
            }

            // 位置の設定
            if (!string.IsNullOrEmpty(Position))
            {
                if (TryParseVector3(Position, out var pos))
                {
                    go.transform.position = pos;
                }
                else
                {
                    Object.DestroyImmediate(go);
                    await context.Stderr.WriteLineAsync($"go: invalid position: '{Position}'", ct);
                    return ExitCode.UsageError;
                }
            }

            // 回転の設定
            if (!string.IsNullOrEmpty(Rotation))
            {
                if (TryParseVector3(Rotation, out var rot))
                {
                    go.transform.eulerAngles = rot;
                }
                else
                {
                    Object.DestroyImmediate(go);
                    await context.Stderr.WriteLineAsync($"go: invalid rotation: '{Rotation}'", ct);
                    return ExitCode.UsageError;
                }
            }

            // タグの設定
            if (!string.IsNullOrEmpty(Tag))
            {
                try
                {
                    go.tag = Tag;
                }
                catch (Exception)
                {
                    await context.Stderr.WriteLineAsync($"go: warning: tag '{Tag}' not found, using 'Untagged'", ct);
                }
            }

            await context.Stdout.WriteLineAsync($"Created: {GameObjectPath.GetPath(go)}", ct);
            return ExitCode.Success;
        }

        /// <summary>
        /// GameObjectを削除します。
        /// </summary>
        private async Task<ExitCode> DeleteAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("go delete: missing path argument", ct);
                return ExitCode.UsageError;
            }

            var path = args[0];
            var go = GameObjectPath.Resolve(path);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"go: '{path}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            if (ChildrenOnly)
            {
                // 子オブジェクトのみ削除
                int childCount = go.transform.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    var child = go.transform.GetChild(i).gameObject;
                    if (Immediate)
                        Object.DestroyImmediate(child);
                    else
                        Object.Destroy(child);
                }
                await context.Stdout.WriteLineAsync($"Deleted {childCount} children of: {path}", ct);
            }
            else
            {
                if (Immediate)
                    Object.DestroyImmediate(go);
                else
                    Object.Destroy(go);
                await context.Stdout.WriteLineAsync($"Deleted: {path}", ct);
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// GameObjectを検索します。
        /// </summary>
        private async Task<ExitCode> FindAsync(CommandContext context, CancellationToken ct)
        {
            var results = new List<GameObject>();

            // 名前検索
            if (!string.IsNullOrEmpty(Name))
            {
                var findInactive = IncludeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
                var all = Object.FindObjectsByType<GameObject>(findInactive, FindObjectsSortMode.None);
                results.AddRange(all.Where(go =>
                    go.name.IndexOf(Name, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            // タグ検索
            if (!string.IsNullOrEmpty(Tag))
            {
                try
                {
                    var tagged = GameObject.FindGameObjectsWithTag(Tag);
                    if (!string.IsNullOrEmpty(Name))
                    {
                        // 名前フィルタと組み合わせ
                        results = results.Where(r => tagged.Contains(r)).ToList();
                    }
                    else
                    {
                        results.AddRange(tagged);
                    }
                }
                catch (Exception)
                {
                    await context.Stderr.WriteLineAsync($"go: tag '{Tag}' not found", ct);
                    return ExitCode.RuntimeError;
                }
            }

            // コンポーネント検索
            if (!string.IsNullOrEmpty(Component))
            {
                var type = FindComponentType(Component);
                if (type != null)
                {
                    var findInactive = IncludeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
                    var withComponent = Object.FindObjectsByType(type, findInactive, FindObjectsSortMode.None)
                        .Cast<UnityEngine.Component>()
                        .Select(c => c.gameObject)
                        .ToList();

                    if (results.Count > 0)
                    {
                        // 既存結果と組み合わせ
                        results = results.Where(r => withComponent.Contains(r)).ToList();
                    }
                    else
                    {
                        results.AddRange(withComponent);
                    }
                }
                else
                {
                    await context.Stderr.WriteLineAsync($"go: component type '{Component}' not found", ct);
                    return ExitCode.RuntimeError;
                }
            }

            // 検索条件がない場合
            if (string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Tag) && string.IsNullOrEmpty(Component))
            {
                await context.Stderr.WriteLineAsync("go find: specify --name, --tag, or --component", ct);
                return ExitCode.UsageError;
            }

            // 重複排除
            results = results.Distinct().ToList();

            if (results.Count == 0)
            {
                await context.Stdout.WriteLineAsync("No GameObjects found.", ct);
                return ExitCode.Success;
            }

            await context.Stdout.WriteLineAsync($"Found {results.Count} GameObjects:", ct);
            foreach (var go in results)
            {
                ct.ThrowIfCancellationRequested();
                string active = go.activeInHierarchy ? "Active" : "Inactive";
                string goPath = GameObjectPath.GetPath(go);
                await context.Stdout.WriteLineAsync($"  {goPath,-40} [{go.tag}] {active}", ct);
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// GameObjectの名前を変更します。
        /// </summary>
        private async Task<ExitCode> RenameAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count < 2)
            {
                await context.Stderr.WriteLineAsync("go rename: usage: go rename <path> <new-name>", ct);
                return ExitCode.UsageError;
            }

            var path = args[0];
            var newName = args[1];

            var go = GameObjectPath.Resolve(path);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"go: '{path}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            string oldName = go.name;
            go.name = newName;

            await context.Stdout.WriteLineAsync($"Renamed: {oldName} -> {newName}", ct);
            return ExitCode.Success;
        }

        /// <summary>
        /// GameObjectのアクティブ状態を変更します。
        /// </summary>
        private async Task<ExitCode> ActiveAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("go active: missing path argument", ct);
                return ExitCode.UsageError;
            }

            var path = args[0];
            var go = GameObjectPath.Resolve(path);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"go: '{path}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            // トグル
            if (Toggle)
            {
                bool newState = !go.activeSelf;
                go.SetActive(newState);
                await context.Stdout.WriteLineAsync($"{path}: {(newState ? "Active" : "Inactive")}", ct);
                return ExitCode.Success;
            }

            // 明示的に設定
            if (!string.IsNullOrEmpty(SetActive))
            {
                if (bool.TryParse(SetActive, out bool state))
                {
                    go.SetActive(state);
                    await context.Stdout.WriteLineAsync($"{path}: {(state ? "Active" : "Inactive")}", ct);
                    return ExitCode.Success;
                }
                else
                {
                    await context.Stderr.WriteLineAsync($"go: invalid value for --set: '{SetActive}'", ct);
                    return ExitCode.UsageError;
                }
            }

            // 状態を表示
            await context.Stdout.WriteLineAsync($"{path}:", ct);
            await context.Stdout.WriteLineAsync($"  activeSelf: {go.activeSelf}", ct);
            await context.Stdout.WriteLineAsync($"  activeInHierarchy: {go.activeInHierarchy}", ct);
            return ExitCode.Success;
        }

        /// <summary>
        /// GameObjectを複製します。
        /// </summary>
        private async Task<ExitCode> CloneAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("go clone: missing path argument", ct);
                return ExitCode.UsageError;
            }

            var path = args[0];
            var go = GameObjectPath.Resolve(path);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"go: '{path}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            // 複製先の親を解決
            Transform parent = null;
            if (!string.IsNullOrEmpty(ParentPath))
            {
                var parentGo = GameObjectPath.Resolve(ParentPath);
                if (parentGo == null)
                {
                    await context.Stderr.WriteLineAsync($"go: '{ParentPath}': Parent not found", ct);
                    return ExitCode.RuntimeError;
                }
                parent = parentGo.transform;
            }
            else
            {
                parent = go.transform.parent;
            }

            int count = Math.Max(1, CloneCount);
            var clonedPaths = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var clone = Object.Instantiate(go, parent);

                // 名前を設定
                if (!string.IsNullOrEmpty(Name))
                {
                    clone.name = count > 1 ? $"{Name}_{i + 1}" : Name;
                }
                else
                {
                    // "(Clone)"を除去
                    clone.name = clone.name.Replace("(Clone)", "").Trim();
                    if (count > 1)
                        clone.name = $"{clone.name}_{i + 1}";
                }

                clonedPaths.Add(GameObjectPath.GetPath(clone));
            }

            if (count == 1)
            {
                await context.Stdout.WriteLineAsync($"Cloned: {clonedPaths[0]}", ct);
            }
            else
            {
                await context.Stdout.WriteLineAsync($"Cloned {count} objects:", ct);
                foreach (var clonePath in clonedPaths)
                {
                    await context.Stdout.WriteLineAsync($"  {clonePath}", ct);
                }
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// GameObjectの詳細情報を表示します。
        /// </summary>
        private async Task<ExitCode> InfoAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("go info: missing path argument", ct);
                return ExitCode.UsageError;
            }

            var path = args[0];
            var go = GameObjectPath.Resolve(path);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"go: '{path}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            var t = go.transform;

            await context.Stdout.WriteLineAsync($"GameObject: {go.name}", ct);
            await context.Stdout.WriteLineAsync($"  Path: {GameObjectPath.GetPath(go)}", ct);
            await context.Stdout.WriteLineAsync($"  Active: {go.activeInHierarchy} (self: {go.activeSelf})", ct);
            await context.Stdout.WriteLineAsync($"  Tag: {go.tag}", ct);
            await context.Stdout.WriteLineAsync($"  Layer: {LayerMask.LayerToName(go.layer)} ({go.layer})", ct);
            await context.Stdout.WriteLineAsync($"  Static: {go.isStatic}", ct);
            await context.Stdout.WriteLineAsync($"  Transform:", ct);
            await context.Stdout.WriteLineAsync($"    Position: {FormatVector3(t.position)}", ct);
            await context.Stdout.WriteLineAsync($"    Rotation: {FormatVector3(t.eulerAngles)}", ct);
            await context.Stdout.WriteLineAsync($"    Scale: {FormatVector3(t.localScale)}", ct);

            var components = go.GetComponents<UnityEngine.Component>();
            await context.Stdout.WriteLineAsync($"  Components ({components.Length}):", ct);
            foreach (var comp in components)
            {
                if (comp != null)
                    await context.Stdout.WriteLineAsync($"    - {comp.GetType().Name}", ct);
            }

            if (t.childCount > 0)
            {
                await context.Stdout.WriteLineAsync($"  Children ({t.childCount}):", ct);
                for (int i = 0; i < Math.Min(t.childCount, 10); i++)
                {
                    await context.Stdout.WriteLineAsync($"    - {t.GetChild(i).name}", ct);
                }
                if (t.childCount > 10)
                {
                    await context.Stdout.WriteLineAsync($"    ... and {t.childCount - 10} more", ct);
                }
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// 不明なサブコマンドのエラーを表示します。
        /// </summary>
        private async Task<ExitCode> UnknownSubCommandAsync(CommandContext context, string subCommand, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"go: unknown subcommand '{subCommand}'", ct);
            await context.Stderr.WriteLineAsync("Subcommands: create, delete, find, rename, active, clone, info", ct);
            return ExitCode.UsageError;
        }

        /// <summary>
        /// Vector3文字列をパースします。
        /// </summary>
        private bool TryParseVector3(string input, out Vector3 result)
        {
            result = Vector3.zero;
            if (string.IsNullOrEmpty(input))
                return false;

            var parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                switch (parts.Length)
                {
                    case 1:
                        float single = float.Parse(parts[0]);
                        result = new Vector3(single, single, single);
                        return true;
                    case 2:
                        result = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), 0);
                        return true;
                    case 3:
                        result = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Vector3を文字列にフォーマットします。
        /// </summary>
        private string FormatVector3(Vector3 v)
        {
            return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
        }

        /// <summary>
        /// コンポーネント型を名前から検索します。
        /// </summary>
        private Type FindComponentType(string typeName)
        {
            // Unity標準のコンポーネント
            var unityType = Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (unityType != null && typeof(UnityEngine.Component).IsAssignableFrom(unityType))
                return unityType;

            // アセンブリ全体を検索
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetTypes().FirstOrDefault(t =>
                        t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                        typeof(UnityEngine.Component).IsAssignableFrom(t));
                    if (type != null)
                        return type;
                }
                catch
                {
                    // アセンブリのロードエラーは無視
                }
            }

            return null;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";

            // サブコマンド補完
            if (context.TokenIndex == 1)
            {
                var subCommands = new[] { "create", "delete", "find", "rename", "active", "clone", "info" };
                foreach (var cmd in subCommands)
                {
                    if (cmd.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                        yield return cmd;
                }
                yield break;
            }

            // パス補完
            if (!token.StartsWith("-"))
            {
                foreach (var path in GameObjectPath.GetCompletions(token))
                {
                    yield return path;
                }
            }
        }
    }
}
