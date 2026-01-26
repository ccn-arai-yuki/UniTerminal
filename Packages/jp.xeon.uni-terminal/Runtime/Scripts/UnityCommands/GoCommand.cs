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
    /// GameObjectの作成・削除・管理を行うコマンド
    /// </summary>
    [Command("go", "Manage GameObjects (create, delete, find, rename, active, clone, info)")]
    public class GoCommand : ICommand
    {
        #region Options

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

        #endregion

        #region ICommand

        public string CommandName => "go";
        public string Description => "Manage GameObjects";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
                return await ShowUsageAsync(context, ct);

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

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";

            if (context.TokenIndex == 1)
                return GetSubCommandCompletions(token);

            if (!token.StartsWith("-"))
                return GameObjectPath.GetCompletions(token);

            return Enumerable.Empty<string>();
        }

        #endregion

        #region Subcommands

        private async Task<ExitCode> CreateAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            string name = args.Count > 0 ? args[0] : "GameObject";

            var go = CreateGameObject(name);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"go: '{Primitive}': Invalid primitive type", ct);
                await context.Stderr.WriteLineAsync("Valid types: Cube, Sphere, Capsule, Cylinder, Plane, Quad", ct);
                return ExitCode.UsageError;
            }

            // 親の設定
            if (!string.IsNullOrEmpty(ParentPath))
            {
                var result = await SetParentForNewObject(context, go, ct);
                if (result != ExitCode.Success)
                    return result;
            }

            // 位置の設定
            if (!string.IsNullOrEmpty(Position))
            {
                if (!TryParseVector3(Position, out var pos))
                {
                    Object.DestroyImmediate(go);
                    await context.Stderr.WriteLineAsync($"go: invalid position: '{Position}'", ct);
                    return ExitCode.UsageError;
                }
                go.transform.position = pos;
            }

            // 回転の設定
            if (!string.IsNullOrEmpty(Rotation))
            {
                if (!TryParseVector3(Rotation, out var rot))
                {
                    Object.DestroyImmediate(go);
                    await context.Stderr.WriteLineAsync($"go: invalid rotation: '{Rotation}'", ct);
                    return ExitCode.UsageError;
                }
                go.transform.eulerAngles = rot;
            }

            // タグの設定
            if (!string.IsNullOrEmpty(Tag))
            {
                try { go.tag = Tag; }
                catch { await context.Stderr.WriteLineAsync($"go: warning: tag '{Tag}' not found, using 'Untagged'", ct); }
            }

            await context.Stdout.WriteLineAsync($"Created: {GameObjectPath.GetPath(go)}", ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> DeleteAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("go delete: missing path argument", ct);
                return ExitCode.UsageError;
            }

            var go = GameObjectPath.Resolve(args[0]);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"go: '{args[0]}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            if (ChildrenOnly)
                return await DeleteChildrenAsync(context, go, ct);

            DestroyObject(go);
            await context.Stdout.WriteLineAsync($"Deleted: {args[0]}", ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> FindAsync(CommandContext context, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Tag) && string.IsNullOrEmpty(Component))
            {
                await context.Stderr.WriteLineAsync("go find: specify --name, --tag, or --component", ct);
                return ExitCode.UsageError;
            }

            var results = FindGameObjects();
            if (results == null)
            {
                await context.Stderr.WriteLineAsync($"go: tag '{Tag}' not found", ct);
                return ExitCode.RuntimeError;
            }

            var componentType = ResolveComponentType();
            if (!string.IsNullOrEmpty(Component) && componentType == null)
            {
                await context.Stderr.WriteLineAsync($"go: component type '{Component}' not found", ct);
                return ExitCode.RuntimeError;
            }

            if (componentType != null)
                results = FilterByComponent(results, componentType);

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
                var active = go.activeInHierarchy ? "Active" : "Inactive";
                await context.Stdout.WriteLineAsync($"  {GameObjectPath.GetPath(go),-40} [{go.tag}] {active}", ct);
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> RenameAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count < 2)
            {
                await context.Stderr.WriteLineAsync("go rename: usage: go rename <path> <new-name>", ct);
                return ExitCode.UsageError;
            }

            var go = GameObjectPath.Resolve(args[0]);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"go: '{args[0]}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            var oldName = go.name;
            go.name = args[1];

            await context.Stdout.WriteLineAsync($"Renamed: {oldName} -> {args[1]}", ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> ActiveAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("go active: missing path argument", ct);
                return ExitCode.UsageError;
            }

            var go = GameObjectPath.Resolve(args[0]);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"go: '{args[0]}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            // トグル
            if (Toggle)
            {
                var newState = !go.activeSelf;
                go.SetActive(newState);
                await context.Stdout.WriteLineAsync($"{args[0]}: {(newState ? "Active" : "Inactive")}", ct);
                return ExitCode.Success;
            }

            // 明示的に設定
            if (!string.IsNullOrEmpty(SetActive))
            {
                if (!bool.TryParse(SetActive, out bool state))
                {
                    await context.Stderr.WriteLineAsync($"go: invalid value for --set: '{SetActive}'", ct);
                    return ExitCode.UsageError;
                }
                go.SetActive(state);
                await context.Stdout.WriteLineAsync($"{args[0]}: {(state ? "Active" : "Inactive")}", ct);
                return ExitCode.Success;
            }

            // 状態を表示
            await context.Stdout.WriteLineAsync($"{args[0]}:", ct);
            await context.Stdout.WriteLineAsync($"  activeSelf: {go.activeSelf}", ct);
            await context.Stdout.WriteLineAsync($"  activeInHierarchy: {go.activeInHierarchy}", ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> CloneAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("go clone: missing path argument", ct);
                return ExitCode.UsageError;
            }

            var go = GameObjectPath.Resolve(args[0]);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"go: '{args[0]}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            var parent = ResolveCloneParent(go);
            if (!string.IsNullOrEmpty(ParentPath) && parent == null)
            {
                await context.Stderr.WriteLineAsync($"go: '{ParentPath}': Parent not found", ct);
                return ExitCode.RuntimeError;
            }

            var count = Math.Max(1, CloneCount);
            var clonedPaths = CreateClones(go, parent, count);

            if (count == 1)
                await context.Stdout.WriteLineAsync($"Cloned: {clonedPaths[0]}", ct);
            else
            {
                await context.Stdout.WriteLineAsync($"Cloned {count} objects:", ct);
                foreach (var path in clonedPaths)
                    await context.Stdout.WriteLineAsync($"  {path}", ct);
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> InfoAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("go info: missing path argument", ct);
                return ExitCode.UsageError;
            }

            var go = GameObjectPath.Resolve(args[0]);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"go: '{args[0]}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            await WriteGameObjectInfo(context, go, ct);
            return ExitCode.Success;
        }

        #endregion

        #region Create Helpers

        private GameObject CreateGameObject(string name)
        {
            if (!string.IsNullOrEmpty(Primitive))
            {
                if (!Enum.TryParse<PrimitiveType>(Primitive, true, out var primitiveType))
                    return null;

                var go = GameObject.CreatePrimitive(primitiveType);
                go.name = name;
                return go;
            }

            return new GameObject(name);
        }

        private async Task<ExitCode> SetParentForNewObject(CommandContext context, GameObject go, CancellationToken ct)
        {
            var parent = GameObjectPath.Resolve(ParentPath);
            if (parent == null)
            {
                Object.DestroyImmediate(go);
                await context.Stderr.WriteLineAsync($"go: '{ParentPath}': Parent not found", ct);
                return ExitCode.RuntimeError;
            }

            go.transform.SetParent(parent.transform, false);
            return ExitCode.Success;
        }

        #endregion

        #region Delete Helpers

        private async Task<ExitCode> DeleteChildrenAsync(CommandContext context, GameObject go, CancellationToken ct)
        {
            var childCount = go.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                DestroyObject(go.transform.GetChild(i).gameObject);
            }
            await context.Stdout.WriteLineAsync($"Deleted {childCount} children of: {go.name}", ct);
            return ExitCode.Success;
        }

        private void DestroyObject(GameObject go)
        {
            if (Immediate)
                Object.DestroyImmediate(go);
            else
                Object.Destroy(go);
        }

        #endregion

        #region Find Helpers

        private List<GameObject> FindGameObjects()
        {
            var results = new List<GameObject>();
            var findInactive = IncludeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;

            // 名前検索
            if (!string.IsNullOrEmpty(Name))
            {
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
                        results = results.Where(r => tagged.Contains(r)).ToList();
                    else
                        results.AddRange(tagged);
                }
                catch
                {
                    return null; // タグが見つからない
                }
            }

            return results;
        }

        private Type ResolveComponentType()
        {
            if (string.IsNullOrEmpty(Component))
                return null;

            // Unity標準のコンポーネント
            var unityType = Type.GetType($"UnityEngine.{Component}, UnityEngine");
            if (unityType != null && typeof(UnityEngine.Component).IsAssignableFrom(unityType))
                return unityType;

            // アセンブリ全体を検索
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetTypes().FirstOrDefault(t =>
                        t.Name.Equals(Component, StringComparison.OrdinalIgnoreCase) &&
                        typeof(UnityEngine.Component).IsAssignableFrom(t));
                    if (type != null)
                        return type;
                }
                catch { /* アセンブリのロードエラーは無視 */ }
            }

            return null;
        }

        private List<GameObject> FilterByComponent(List<GameObject> results, Type componentType)
        {
            var findInactive = IncludeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            var withComponent = Object.FindObjectsByType(componentType, findInactive, FindObjectsSortMode.None)
                .Cast<UnityEngine.Component>()
                .Select(c => c.gameObject)
                .ToList();

            return results.Count > 0
                ? results.Where(r => withComponent.Contains(r)).ToList()
                : withComponent;
        }

        #endregion

        #region Clone Helpers

        private Transform ResolveCloneParent(GameObject original)
        {
            if (!string.IsNullOrEmpty(ParentPath))
            {
                var parentGo = GameObjectPath.Resolve(ParentPath);
                return parentGo?.transform;
            }
            return original.transform.parent;
        }

        private List<string> CreateClones(GameObject original, Transform parent, int count)
        {
            var paths = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var clone = Object.Instantiate(original, parent);
                clone.name = GetCloneName(original, i, count);
                paths.Add(GameObjectPath.GetPath(clone));
            }

            return paths;
        }

        private string GetCloneName(GameObject original, int index, int count)
        {
            if (!string.IsNullOrEmpty(Name))
                return count > 1 ? $"{Name}_{index + 1}" : Name;

            var baseName = original.name.Replace("(Clone)", "").Trim();
            return count > 1 ? $"{baseName}_{index + 1}" : baseName;
        }

        #endregion

        #region Info Output

        private async Task WriteGameObjectInfo(CommandContext context, GameObject go, CancellationToken ct)
        {
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

            await WriteComponentsInfo(context, go, ct);
            await WriteChildrenInfo(context, t, ct);
        }

        private async Task WriteComponentsInfo(CommandContext context, GameObject go, CancellationToken ct)
        {
            var components = go.GetComponents<UnityEngine.Component>();
            await context.Stdout.WriteLineAsync($"  Components ({components.Length}):", ct);
            foreach (var comp in components)
            {
                if (comp != null)
                    await context.Stdout.WriteLineAsync($"    - {comp.GetType().Name}", ct);
            }
        }

        private async Task WriteChildrenInfo(CommandContext context, Transform t, CancellationToken ct)
        {
            if (t.childCount == 0)
                return;

            await context.Stdout.WriteLineAsync($"  Children ({t.childCount}):", ct);
            var displayCount = Math.Min(t.childCount, 10);
            for (int i = 0; i < displayCount; i++)
                await context.Stdout.WriteLineAsync($"    - {t.GetChild(i).name}", ct);

            if (t.childCount > 10)
                await context.Stdout.WriteLineAsync($"    ... and {t.childCount - 10} more", ct);
        }

        #endregion

        #region Usage/Error Output

        private async Task<ExitCode> ShowUsageAsync(CommandContext context, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync("go: missing subcommand", ct);
            await context.Stderr.WriteLineAsync("Usage: go <subcommand> [options] [arguments]", ct);
            await context.Stderr.WriteLineAsync("Subcommands: create, delete, find, rename, active, clone, info", ct);
            return ExitCode.UsageError;
        }

        private async Task<ExitCode> UnknownSubCommandAsync(CommandContext context, string subCommand, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"go: unknown subcommand '{subCommand}'", ct);
            await context.Stderr.WriteLineAsync("Subcommands: create, delete, find, rename, active, clone, info", ct);
            return ExitCode.UsageError;
        }

        #endregion

        #region Utility

        private static bool TryParseVector3(string input, out Vector3 result)
        {
            result = Vector3.zero;
            if (string.IsNullOrEmpty(input))
                return false;

            var parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                return parts.Length switch
                {
                    1 => ParseSingleValue(parts, out result),
                    2 => ParseTwoValues(parts, out result),
                    3 => ParseThreeValues(parts, out result),
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        private static bool ParseSingleValue(string[] parts, out Vector3 result)
        {
            var single = float.Parse(parts[0]);
            result = new Vector3(single, single, single);
            return true;
        }

        private static bool ParseTwoValues(string[] parts, out Vector3 result)
        {
            result = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), 0);
            return true;
        }

        private static bool ParseThreeValues(string[] parts, out Vector3 result)
        {
            result = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            return true;
        }

        private static string FormatVector3(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";

        private static IEnumerable<string> GetSubCommandCompletions(string token)
        {
            var subCommands = new[] { "create", "delete", "find", "rename", "active", "clone", "info" };
            return subCommands.Where(cmd => cmd.StartsWith(token, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}
