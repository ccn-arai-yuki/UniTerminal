using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        [Option("name", "n", Description = "Filter by name (supports * and ? wildcards)")]
        public string NameFilter;

        [Option("component", "c", Description = "Filter by component type")]
        public string ComponentFilter;

        [Option("tag", "t", Description = "Filter by tag")]
        public string TagFilter;

        [Option("layer", "y", Description = "Filter by layer name or number")]
        public string LayerFilter;

        private Regex nameFilterRegex;
        private Type componentFilterType;
        private int? layerFilterValue;

        public string CommandName => "hierarchy";
        public string Description => "Display scene hierarchy";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            // フィルター初期化
            if (!InitializeFilters(context, out var errorMessage))
            {
                await context.Stderr.WriteLineAsync(errorMessage, ct);
                return ExitCode.UsageError;
            }

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

            // フィルターが有効な場合、マッチするオブジェクトを含むルートのみ表示
            if (HasFilters())
            {
                roots = roots.Where(go => MatchesFilterRecursive(go)).ToList();
            }

            var filterInfo = HasFilters() ? " (filtered)" : "";
            await context.Stdout.WriteLineAsync($"Scene: {scene.name} ({roots.Count} root objects{filterInfo})", ct);

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

            // 子オブジェクトを取得
            var children = GetVisibleChildren(go.transform);

            // フィルターが有効な場合、マッチするオブジェクトを含む子のみ表示
            if (HasFilters())
            {
                children = children.Where(c => MatchesFilterRecursive(c.gameObject));
            }

            var childList = children.ToList();
            var filterInfo = HasFilters() ? " (filtered)" : "";
            await context.Stdout.WriteLineAsync($"{go.name} (children: {childList.Count}{filterInfo})", ct);

            for (int i = 0; i < childList.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                bool isLast = i == childList.Count - 1;
                await PrintTreeNodeAsync(context, childList[i].gameObject, "", isLast, 0, ct);
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

            // フィルターが有効な場合、マッチしないオブジェクトはスキップ（子孫にマッチがある場合は表示）
            bool matchesDirectly = HasFilters() && MatchesFilter(go);
            bool hasMatchingDescendants = HasFilters() && !matchesDirectly && MatchesFilterRecursive(go);

            if (HasFilters() && !matchesDirectly && !hasMatchingDescendants)
                return;

            // ツリー記号
            string connector = isLast ? "└── " : "├── ";
            string childPrefix = prefix + (isLast ? "    " : "│   ");

            // オブジェクト情報を構築
            string line = BuildObjectLine(go, matchesDirectly);
            await context.Stdout.WriteLineAsync(prefix + connector + line, ct);

            // 再帰表示
            if (Recursive || currentDepth == 0)
            {
                var children = GetVisibleChildren(go.transform);

                // フィルターが有効な場合、マッチするオブジェクトを含む子のみ表示
                if (HasFilters())
                {
                    children = children.Where(c => MatchesFilterRecursive(c.gameObject));
                }

                var childList = children.ToList();
                for (int i = 0; i < childList.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    bool childIsLast = i == childList.Count - 1;
                    await PrintTreeNodeAsync(
                        context,
                        childList[i].gameObject,
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
        private string BuildObjectLine(GameObject go, bool highlight = false)
        {
            string marker = highlight ? "* " : "";

            if (!LongFormat)
            {
                return marker + go.name;
            }

            // 詳細形式
            string active = go.activeSelf ? "[A]" : "[-]";
            int compCount = go.GetComponents<Component>().Length;
            string tag = go.tag;

            return $"{marker}{active} {go.name,-24} ({compCount} components) [{tag}]";
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
            var inputLine = context.InputLine ?? "";

            // オプション値の補完
            // -t/--tag の後のタグ名補完
            if (inputLine.EndsWith(" -t ") || inputLine.EndsWith(" --tag ") ||
                (context.TokenIndex > 1 && IsAfterOption(inputLine, "-t", "--tag")))
            {
#if UNITY_EDITOR
                foreach (var tag in UnityEditorInternal.InternalEditorUtility.tags)
                {
                    if (tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        yield return tag;
                }
#else
                // Runtime環境では一般的なタグのみ
                var commonTags = new[] { "Untagged", "Respawn", "Finish", "EditorOnly", "MainCamera", "Player", "GameController" };
                foreach (var tag in commonTags)
                {
                    if (tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        yield return tag;
                }
#endif
                yield break;
            }

            // -y/--layer の後のレイヤー名補完
            if (inputLine.EndsWith(" -y ") || inputLine.EndsWith(" --layer ") ||
                (context.TokenIndex > 1 && IsAfterOption(inputLine, "-y", "--layer")))
            {
                for (int i = 0; i < 32; i++)
                {
                    var layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName) && layerName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        yield return layerName;
                }
                yield break;
            }

            // -c/--component の後のコンポーネント名補完
            if (inputLine.EndsWith(" -c ") || inputLine.EndsWith(" --component ") ||
                (context.TokenIndex > 1 && IsAfterOption(inputLine, "-c", "--component")))
            {
                foreach (var typeName in TypeResolver.GetCommonComponentNames())
                {
                    if (typeName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        yield return typeName;
                }
                yield break;
            }

            // パス補完
            if (!prefix.StartsWith("-"))
            {
                foreach (var path in GameObjectPath.GetCompletions(prefix))
                {
                    yield return path;
                }
            }
        }

        /// <summary>
        /// 入力行が指定オプションの直後かどうかを判定します。
        /// </summary>
        private bool IsAfterOption(string inputLine, string shortOpt, string longOpt)
        {
            var tokens = inputLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2)
                return false;

            var prevToken = tokens[tokens.Length - 2];
            return prevToken == shortOpt || prevToken == longOpt;
        }

        /// <summary>
        /// フィルターを初期化します。
        /// </summary>
        private bool InitializeFilters(CommandContext context, out string errorMessage)
        {
            errorMessage = null;

            // 名前フィルター（ワイルドカードを正規表現に変換）
            if (!string.IsNullOrEmpty(NameFilter))
            {
                try
                {
                    var pattern = "^" + Regex.Escape(NameFilter)
                        .Replace("\\*", ".*")
                        .Replace("\\?", ".") + "$";
                    nameFilterRegex = new Regex(pattern, RegexOptions.IgnoreCase);
                }
                catch (Exception ex)
                {
                    errorMessage = $"hierarchy: invalid name filter pattern: {ex.Message}";
                    return false;
                }
            }

            // コンポーネントフィルター
            if (!string.IsNullOrEmpty(ComponentFilter))
            {
                componentFilterType = TypeResolver.ResolveComponentType(ComponentFilter);
                if (componentFilterType == null)
                {
                    errorMessage = $"hierarchy: unknown component type '{ComponentFilter}'";
                    return false;
                }
            }

            // レイヤーフィルター
            if (!string.IsNullOrEmpty(LayerFilter))
            {
                if (int.TryParse(LayerFilter, out int layerNum))
                {
                    if (layerNum < 0 || layerNum > 31)
                    {
                        errorMessage = $"hierarchy: invalid layer number '{LayerFilter}' (must be 0-31)";
                        return false;
                    }
                    layerFilterValue = layerNum;
                }
                else
                {
                    int layer = LayerMask.NameToLayer(LayerFilter);
                    if (layer == -1)
                    {
                        errorMessage = $"hierarchy: unknown layer '{LayerFilter}'";
                        return false;
                    }
                    layerFilterValue = layer;
                }
            }

            return true;
        }

        /// <summary>
        /// フィルターが有効かどうかを確認します。
        /// </summary>
        private bool HasFilters()
        {
            return nameFilterRegex != null ||
                   componentFilterType != null ||
                   !string.IsNullOrEmpty(TagFilter) ||
                   layerFilterValue.HasValue;
        }

        /// <summary>
        /// GameObjectがフィルター条件に一致するかどうかを確認します。
        /// </summary>
        private bool MatchesFilter(GameObject go)
        {
            // 名前フィルター
            if (nameFilterRegex != null && !nameFilterRegex.IsMatch(go.name))
            {
                return false;
            }

            // コンポーネントフィルター
            if (componentFilterType != null && go.GetComponent(componentFilterType) == null)
            {
                return false;
            }

            // タグフィルター
            if (!string.IsNullOrEmpty(TagFilter) && !go.CompareTag(TagFilter))
            {
                return false;
            }

            // レイヤーフィルター
            if (layerFilterValue.HasValue && go.layer != layerFilterValue.Value)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// オブジェクトまたはその子孫がフィルター条件に一致するかどうかを確認します。
        /// </summary>
        private bool MatchesFilterRecursive(GameObject go)
        {
            if (MatchesFilter(go))
                return true;

            for (int i = 0; i < go.transform.childCount; i++)
            {
                if (MatchesFilterRecursive(go.transform.GetChild(i).gameObject))
                    return true;
            }

            return false;
        }
    }
}
