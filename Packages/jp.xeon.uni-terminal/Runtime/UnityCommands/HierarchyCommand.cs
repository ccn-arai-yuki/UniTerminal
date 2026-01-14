using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Xeon.UniTerminal.UnityCommands
{
    /// <summary>
    /// シーンのヒエラルキー構造を表示するコマンド。
    /// </summary>
    [Command("hierarchy", "Display scene hierarchy")]
    public class HierarchyCommand : ICommand
    {
        [Option("recursive", "r", Description = "Show children recursively")]
        public bool Recursive;

        [Option("depth", "d", Description = "Maximum depth to display (-1 = unlimited)")]
        public int MaxDepth = -1;

        [Option("all", "a", Description = "Include inactive objects")]
        public bool ShowInactive;

        [Option("long", "l", Description = "Show detailed information")]
        public bool LongFormat;

        [Option("scene", "s", Description = "Target scene name (or 'list' to show loaded scenes)")]
        public string SceneName;

        public string CommandName => "hierarchy";
        public string Description => "Display scene hierarchy";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            // シーン一覧を表示
            if (SceneName == "list")
            {
                return await ListScenesAsync(context, ct);
            }

            // 対象シーンを決定
            Scene targetScene;
            if (!string.IsNullOrEmpty(SceneName))
            {
                var scene = GameObjectPath.GetSceneByName(SceneName);
                if (!scene.HasValue)
                {
                    await context.Stderr.WriteLineAsync($"hierarchy: '{SceneName}': Scene not loaded", ct);
                    return ExitCode.RuntimeError;
                }
                targetScene = scene.Value;
            }
            else
            {
                targetScene = SceneManager.GetActiveScene();
            }

            // パスが指定されている場合、そのオブジェクトの子を表示
            if (context.PositionalArguments.Count > 0)
            {
                var path = context.PositionalArguments[0];
                return await DisplayFromPathAsync(context, path, targetScene, ct);
            }

            // シーン全体のルートオブジェクトを表示
            return await DisplaySceneAsync(context, targetScene, ct);
        }

        /// <summary>
        /// ロード済みシーン一覧を表示します。
        /// </summary>
        private async Task<ExitCode> ListScenesAsync(CommandContext context, CancellationToken ct)
        {
            await context.Stdout.WriteLineAsync("Loaded Scenes:", ct);

            foreach (var (name, isActive) in GameObjectPath.GetLoadedScenes())
            {
                var marker = isActive ? "*" : " ";
                await context.Stdout.WriteLineAsync($"  {marker} {name}{(isActive ? " (active)" : "")}", ct);
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// シーン全体のヒエラルキーを表示します。
        /// </summary>
        private async Task<ExitCode> DisplaySceneAsync(CommandContext context, Scene scene, CancellationToken ct)
        {
            var roots = scene.GetRootGameObjects()
                .Where(go => ShowInactive || go.activeInHierarchy)
                .OrderBy(go => go.transform.GetSiblingIndex())
                .ToList();

            await context.Stdout.WriteLineAsync($"Scene: {scene.name} ({roots.Count} root objects)", ct);

            for (int i = 0; i < roots.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                bool isLast = i == roots.Count - 1;
                await PrintTreeNodeAsync(context, roots[i], "", isLast, 0, ct);
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// 指定パスからヒエラルキーを表示します。
        /// </summary>
        private async Task<ExitCode> DisplayFromPathAsync(
            CommandContext context,
            string path,
            Scene scene,
            CancellationToken ct)
        {
            var go = GameObjectPath.Resolve(path, scene);
            if (go == null)
            {
                await context.Stderr.WriteLineAsync($"hierarchy: '{path}': GameObject not found", ct);
                return ExitCode.RuntimeError;
            }

            var childCount = GetVisibleChildCount(go.transform);
            await context.Stdout.WriteLineAsync($"{go.name} (children: {childCount})", ct);

            // 子オブジェクトを表示
            var children = GetVisibleChildren(go.transform).ToList();
            for (int i = 0; i < children.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                bool isLast = i == children.Count - 1;
                await PrintTreeNodeAsync(context, children[i].gameObject, "", isLast, 0, ct);
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// ツリーノードを出力します。
        /// </summary>
        private async Task PrintTreeNodeAsync(
            CommandContext context,
            GameObject go,
            string prefix,
            bool isLast,
            int currentDepth,
            CancellationToken ct)
        {
            // 深度制限チェック
            if (MaxDepth >= 0 && currentDepth > MaxDepth)
                return;

            // 非アクティブをスキップ
            if (!ShowInactive && !go.activeInHierarchy)
                return;

            // ツリー記号
            string connector = isLast ? "└── " : "├── ";
            string childPrefix = prefix + (isLast ? "    " : "│   ");

            // オブジェクト情報を構築
            string line = BuildObjectLine(go);
            await context.Stdout.WriteLineAsync(prefix + connector + line, ct);

            // 再帰表示
            if (Recursive || currentDepth == 0)
            {
                var children = GetVisibleChildren(go.transform).ToList();
                for (int i = 0; i < children.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    bool childIsLast = i == children.Count - 1;
                    await PrintTreeNodeAsync(
                        context,
                        children[i].gameObject,
                        childPrefix,
                        childIsLast,
                        currentDepth + 1,
                        ct);
                }
            }
        }

        /// <summary>
        /// オブジェクト行を構築します。
        /// </summary>
        private string BuildObjectLine(GameObject go)
        {
            if (!LongFormat)
            {
                return go.name;
            }

            // 詳細形式
            string active = go.activeSelf ? "[A]" : "[-]";
            int compCount = go.GetComponents<Component>().Length;
            string tag = go.tag;

            return $"{active} {go.name,-24} ({compCount} components) [{tag}]";
        }

        /// <summary>
        /// 可視の子オブジェクト数を取得します。
        /// </summary>
        private int GetVisibleChildCount(Transform transform)
        {
            if (ShowInactive)
                return transform.childCount;

            int count = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeInHierarchy)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 可視の子オブジェクトを取得します。
        /// </summary>
        private IEnumerable<Transform> GetVisibleChildren(Transform transform)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (ShowInactive || child.gameObject.activeInHierarchy)
                {
                    yield return child;
                }
            }
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var prefix = context.CurrentToken ?? "";

            // パス補完
            if (!prefix.StartsWith("-"))
            {
                foreach (var path in GameObjectPath.GetCompletions(prefix))
                {
                    yield return path;
                }
            }
        }
    }
}
