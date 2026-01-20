using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Xeon.UniTerminal.UnityCommands
{
    /// <summary>
    /// シーン管理コマンド。
    /// </summary>
    [Command("scene", "Manage scenes (list, load, unload, active, info, create)")]
    public class SceneCommand : ICommand
    {
        [Option("all", "a", Description = "Show all scenes in Build Settings")]
        public bool All;

        [Option("long", "l", Description = "Show detailed information")]
        public bool Long;

        [Option("additive", "", Description = "Load/create in additive mode")]
        public bool Additive;

        [Option("async", "", Description = "Use async operation")]
        public bool Async;

        [Option("setup", "s", Description = "Add default objects (Camera, Light) for create")]
        public bool Setup;

        public string CommandName => "scene";
        public string Description => "Manage scenes";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
            {
                await context.Stderr.WriteLineAsync("scene: missing subcommand", ct);
                await context.Stderr.WriteLineAsync("Usage: scene <subcommand> [options] [arguments]", ct);
                await context.Stderr.WriteLineAsync("Subcommands: list, load, unload, active, info, create", ct);
                return ExitCode.UsageError;
            }

            var subCommand = context.PositionalArguments[0].ToLower();
            var args = context.PositionalArguments.Skip(1).ToList();

            return subCommand switch
            {
                "list" => await ListAsync(context, ct),
                "load" => await LoadAsync(context, args, ct),
                "unload" => await UnloadAsync(context, args, ct),
                "active" => await ActiveAsync(context, args, ct),
                "info" => await InfoAsync(context, args, ct),
                "create" => await CreateAsync(context, args, ct),
                _ => await UnknownSubCommandAsync(context, subCommand, ct)
            };
        }

        private async Task<ExitCode> ListAsync(CommandContext context, CancellationToken ct)
        {
            var activeScene = SceneManager.GetActiveScene();

            if (All)
                return await ListAllScenesAsync(context, activeScene, ct);

            return await ListLoadedScenesAsync(context, activeScene, ct);
        }

        private async Task<ExitCode> ListLoadedScenesAsync(CommandContext context, Scene activeScene, CancellationToken ct)
        {
            var sceneCount = SceneManager.sceneCount;

            if (sceneCount == 0)
            {
                await context.Stdout.WriteLineAsync("No scenes loaded", ct);
                return ExitCode.Success;
            }

            for (int i = 0; i < sceneCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                var scene = SceneManager.GetSceneAt(i);
                var isActive = scene == activeScene;
                var marker = isActive ? "*" : " ";

                if (Long)
                {
                    var status = scene.isLoaded ? "Loaded" : "Loading";
                    var rootCount = scene.isLoaded ? scene.rootCount : 0;
                    await context.Stdout.WriteLineAsync(
                        $"{marker} {scene.name,-20} {status,-10} {scene.path,-40} ({rootCount} root objects)", ct);
                }
                else
                {
                    var suffix = isActive ? " (active)" : "";
                    await context.Stdout.WriteLineAsync($"{marker} {scene.name}{suffix}", ct);
                }
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> ListAllScenesAsync(CommandContext context, Scene activeScene, CancellationToken ct)
        {
            var buildSceneCount = SceneManager.sceneCountInBuildSettings;

            if (buildSceneCount == 0)
            {
                await context.Stdout.WriteLineAsync("No scenes in Build Settings", ct);
                return ExitCode.Success;
            }

            for (int i = 0; i < buildSceneCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                var scene = SceneManager.GetSceneByBuildIndex(i);
                var isLoaded = scene.IsValid() && scene.isLoaded;
                var isActive = scene == activeScene;
                var marker = isActive ? "*" : " ";

                if (Long)
                {
                    var status = isLoaded ? "Loaded" : "Not loaded";
                    await context.Stdout.WriteLineAsync(
                        $"{marker} {sceneName,-20} {status,-12} BuildIndex: {i,-3} {scenePath}", ct);
                }
                else
                {
                    var status = isLoaded ? (isActive ? "active" : "loaded") : "not loaded";
                    await context.Stdout.WriteLineAsync($"{marker} {sceneName,-20} ({status}) BuildIndex: {i}", ct);
                }
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> LoadAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("scene load: missing scene name", ct);
                await context.Stderr.WriteLineAsync("Usage: scene load <scene> [--additive]", ct);
                return ExitCode.UsageError;
            }

            var sceneName = args[0];
            var mode = Additive ? LoadSceneMode.Additive : LoadSceneMode.Single;

            var sceneToLoad = ResolveSceneName(sceneName);
            if (sceneToLoad == null)
            {
                await context.Stderr.WriteLineAsync($"scene load: '{sceneName}': scene not found in Build Settings", ct);
                return ExitCode.RuntimeError;
            }

            if (Async)
                return await LoadSceneAsyncInternal(context, sceneToLoad, mode, ct);

            return await LoadSceneSync(context, sceneToLoad, mode, ct);
        }

        private async Task<ExitCode> LoadSceneSync(CommandContext context, string sceneName, LoadSceneMode mode, CancellationToken ct)
        {
            try
            {
                SceneManager.LoadScene(sceneName, mode);
                var modeStr = mode == LoadSceneMode.Additive ? "additively" : "as single";
                await context.Stdout.WriteLineAsync($"Loaded scene '{sceneName}' {modeStr}", ct);
                return ExitCode.Success;
            }
            catch (System.Exception ex)
            {
                await context.Stderr.WriteLineAsync($"scene load: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
        }

        private async Task<ExitCode> LoadSceneAsyncInternal(CommandContext context, string sceneName, LoadSceneMode mode, CancellationToken ct)
        {
            try
            {
                var operation = SceneManager.LoadSceneAsync(sceneName, mode);
                if (operation == null)
                {
                    await context.Stderr.WriteLineAsync($"scene load: failed to start async load for '{sceneName}'", ct);
                    return ExitCode.RuntimeError;
                }

                while (!operation.isDone)
                {
                    ct.ThrowIfCancellationRequested();
                    await context.Stdout.WriteLineAsync($"Loading '{sceneName}'... {operation.progress * 100:F0}%", ct);
                    await Task.Yield();
                }

                var modeStr = mode == LoadSceneMode.Additive ? "additively" : "as single";
                await context.Stdout.WriteLineAsync($"Loaded scene '{sceneName}' {modeStr}", ct);
                return ExitCode.Success;
            }
            catch (System.Exception ex)
            {
                await context.Stderr.WriteLineAsync($"scene load: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
        }

        private async Task<ExitCode> UnloadAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("scene unload: missing scene name", ct);
                await context.Stderr.WriteLineAsync("Usage: scene unload <scene>", ct);
                return ExitCode.UsageError;
            }

            var sceneName = args[0];
            var scene = GetLoadedScene(sceneName);

            if (!scene.IsValid())
            {
                await context.Stderr.WriteLineAsync($"scene unload: '{sceneName}': scene not loaded", ct);
                return ExitCode.RuntimeError;
            }

            if (SceneManager.sceneCount <= 1)
            {
                await context.Stderr.WriteLineAsync("scene unload: cannot unload the only loaded scene", ct);
                return ExitCode.RuntimeError;
            }

            if (scene == SceneManager.GetActiveScene())
            {
                await context.Stderr.WriteLineAsync("scene unload: cannot unload active scene. Change active scene first.", ct);
                return ExitCode.RuntimeError;
            }

            try
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if (operation == null)
                {
                    await context.Stderr.WriteLineAsync($"scene unload: failed to unload '{sceneName}'", ct);
                    return ExitCode.RuntimeError;
                }

                if (Async)
                {
                    while (!operation.isDone)
                    {
                        ct.ThrowIfCancellationRequested();
                        await Task.Yield();
                    }
                }

                await context.Stdout.WriteLineAsync($"Unloaded scene '{scene.name}'", ct);
                return ExitCode.Success;
            }
            catch (System.Exception ex)
            {
                await context.Stderr.WriteLineAsync($"scene unload: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
        }

        private async Task<ExitCode> ActiveAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            var activeScene = SceneManager.GetActiveScene();

            if (args.Count == 0)
            {
                await context.Stdout.WriteLineAsync($"Active scene: {activeScene.name} ({activeScene.path})", ct);
                return ExitCode.Success;
            }

            var sceneName = args[0];
            var scene = GetLoadedScene(sceneName);

            if (!scene.IsValid())
            {
                await context.Stderr.WriteLineAsync($"scene active: '{sceneName}': scene not loaded", ct);
                return ExitCode.RuntimeError;
            }

            var oldActive = activeScene.name;
            if (!SceneManager.SetActiveScene(scene))
            {
                await context.Stderr.WriteLineAsync($"scene active: failed to set '{sceneName}' as active", ct);
                return ExitCode.RuntimeError;
            }

            await context.Stdout.WriteLineAsync($"Active scene changed: {oldActive} -> {scene.name}", ct);
            return ExitCode.Success;
        }

        private async Task<ExitCode> InfoAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
            Scene scene;

            if (args.Count == 0)
            {
                scene = SceneManager.GetActiveScene();
            }
            else
            {
                scene = GetLoadedScene(args[0]);
                if (!scene.IsValid())
                {
                    await context.Stderr.WriteLineAsync($"scene info: '{args[0]}': scene not loaded", ct);
                    return ExitCode.RuntimeError;
                }
            }

            await context.Stdout.WriteLineAsync($"Scene: {scene.name}", ct);
            await context.Stdout.WriteLineAsync($"  Path:         {scene.path}", ct);
            await context.Stdout.WriteLineAsync($"  Build Index:  {scene.buildIndex}", ct);
            await context.Stdout.WriteLineAsync($"  Is Loaded:    {scene.isLoaded}", ct);
            await context.Stdout.WriteLineAsync($"  Is Active:    {scene == SceneManager.GetActiveScene()}", ct);

#if UNITY_EDITOR
            await context.Stdout.WriteLineAsync($"  Is Dirty:     {scene.isDirty}", ct);
#endif

            if (scene.isLoaded)
            {
                var rootObjects = scene.GetRootGameObjects();
                await context.Stdout.WriteLineAsync($"  Root Count:   {rootObjects.Length}", ct);
                await context.Stdout.WriteLineAsync("  Root Objects:", ct);

                var displayCount = Mathf.Min(rootObjects.Length, 10);
                for (int i = 0; i < displayCount; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    await context.Stdout.WriteLineAsync($"    - {rootObjects[i].name}", ct);
                }

                if (rootObjects.Length > 10)
                    await context.Stdout.WriteLineAsync($"    ... and {rootObjects.Length - 10} more", ct);
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> CreateAsync(CommandContext context, List<string> args, CancellationToken ct)
        {
#if UNITY_EDITOR
            if (args.Count == 0)
            {
                await context.Stderr.WriteLineAsync("scene create: missing scene name", ct);
                await context.Stderr.WriteLineAsync("Usage: scene create <name> [--additive] [--setup]", ct);
                return ExitCode.UsageError;
            }

            var sceneName = args[0];
            var mode = Additive
                ? UnityEditor.SceneManagement.NewSceneMode.Additive
                : UnityEditor.SceneManagement.NewSceneMode.Single;
            var setup = Setup
                ? UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects
                : UnityEditor.SceneManagement.NewSceneSetup.EmptyScene;

            try
            {
                var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(setup, mode);
                newScene.name = sceneName;

                var modeStr = Additive ? "additively" : "as single";
                var setupStr = Setup ? "with default objects" : "empty";
                await context.Stdout.WriteLineAsync($"Created scene '{sceneName}' {modeStr} ({setupStr})", ct);
                return ExitCode.Success;
            }
            catch (System.Exception ex)
            {
                await context.Stderr.WriteLineAsync($"scene create: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
#else
            await context.Stderr.WriteLineAsync("scene create: only available in Editor", ct);
            return ExitCode.RuntimeError;
#endif
        }

        private async Task<ExitCode> UnknownSubCommandAsync(CommandContext context, string subCommand, CancellationToken ct)
        {
            await context.Stderr.WriteLineAsync($"scene: unknown subcommand '{subCommand}'", ct);
            await context.Stderr.WriteLineAsync("Available subcommands: list, load, unload, active, info, create", ct);
            return ExitCode.UsageError;
        }

        private string ResolveSceneName(string input)
        {
            if (int.TryParse(input, out int buildIndex))
            {
                if (buildIndex >= 0 && buildIndex < SceneManager.sceneCountInBuildSettings)
                    return SceneUtility.GetScenePathByBuildIndex(buildIndex);

                return null;
            }

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);

                if (name == input || path == input)
                    return path;
            }

            return null;
        }

        private Scene GetLoadedScene(string input)
        {
            if (int.TryParse(input, out int buildIndex))
                return SceneManager.GetSceneByBuildIndex(buildIndex);

            var scene = SceneManager.GetSceneByName(input);
            if (scene.IsValid())
                return scene;

            scene = SceneManager.GetSceneByPath(input);
            return scene;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            var token = context.CurrentToken ?? "";

            // サブコマンド補完
            if (context.TokenIndex == 1)
            {
                var subCommands = new[] { "list", "load", "unload", "active", "info", "create" };
                foreach (var cmd in subCommands)
                {
                    if (cmd.StartsWith(token, System.StringComparison.OrdinalIgnoreCase))
                        yield return cmd;
                }
                yield break;
            }

            // シーン名補完（TokenIndex >= 2）
            if (context.TokenIndex >= 2 && !token.StartsWith("-"))
            {
                foreach (var sceneName in GetSceneNameCompletions(token))
                    yield return sceneName;
            }
        }

        private IEnumerable<string> GetSceneNameCompletions(string prefix)
        {
            var scenes = new List<string>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scenes.Contains(scene.name))
                    scenes.Add(scene.name);
            }

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!scenes.Contains(name))
                    scenes.Add(name);
            }

            if (string.IsNullOrEmpty(prefix))
                return scenes;

            return scenes.Where(s => s.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
