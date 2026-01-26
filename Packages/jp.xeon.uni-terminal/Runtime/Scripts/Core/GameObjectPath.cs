using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// GameObjectのパス解決用ユーティリティ
    /// /Parent/Child/Target のようなパス形式でGameObjectを参照します
    /// </summary>
    public static class GameObjectPath
    {
        /// <summary>
        /// パス文字列からGameObjectを解決します
        /// </summary>
        /// <param name="path">GameObjectのパス（例: /Parent/Child）</param>
        /// <param name="scene">対象シーン（nullの場合はアクティブシーン）</param>
        /// <returns>見つかったGameObject、見つからない場合はnull</returns>
        public static GameObject Resolve(string path, Scene? scene = null)
        {
            if (string.IsNullOrEmpty(path) || path == "/")
                return null;

            var targetScene = scene ?? SceneManager.GetActiveScene();
            if (!targetScene.IsValid() || !targetScene.isLoaded)
                return null;

            var parts = path.TrimStart('/').Split('/');
            if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
                return null;

            var roots = targetScene.GetRootGameObjects();
            var current = roots.FirstOrDefault(r => r.name == parts[0]);

            for (int i = 1; i < parts.Length && current != null; i++)
            {
                var child = current.transform.Find(parts[i]);
                current = child?.gameObject;
            }

            return current;
        }

        /// <summary>
        /// GameObjectのパス文字列を取得します
        /// </summary>
        /// <param name="go">対象のGameObject</param>
        /// <returns>パス文字列（例: /Parent/Child/Target）</returns>
        public static string GetPath(GameObject go)
        {
            if (go == null)
                return null;

            var path = new List<string>();
            var current = go.transform;

            while (current != null)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }

            return "/" + string.Join("/", path);
        }

        /// <summary>
        /// 指定されたパスにGameObjectが存在するかどうかを確認します
        /// </summary>
        /// <param name="path">確認するパス</param>
        /// <param name="scene">対象シーン（nullの場合はアクティブシーン）</param>
        /// <returns>存在する場合はtrue</returns>
        public static bool Exists(string path, Scene? scene = null)
        {
            return Resolve(path, scene) != null;
        }

        /// <summary>
        /// 指定されたシーンのルートGameObjectを取得します
        /// </summary>
        /// <param name="scene">対象シーン（nullの場合はアクティブシーン）</param>
        /// <returns>ルートGameObjectの配列</returns>
        public static GameObject[] GetRootGameObjects(Scene? scene = null)
        {
            var targetScene = scene ?? SceneManager.GetActiveScene();
            if (!targetScene.IsValid() || !targetScene.isLoaded)
                return new GameObject[0];

            return targetScene.GetRootGameObjects();
        }

        /// <summary>
        /// 指定されたパスの子GameObjectのパス一覧を取得します（補完用）
        /// </summary>
        /// <param name="prefix">パスのプレフィックス</param>
        /// <param name="scene">対象シーン（nullの場合はアクティブシーン）</param>
        /// <returns>マッチするパスの列挙</returns>
        public static IEnumerable<string> GetCompletions(string prefix, Scene? scene = null)
        {
            var targetScene = scene ?? SceneManager.GetActiveScene();
            if (!targetScene.IsValid() || !targetScene.isLoaded)
                yield break;

            // プレフィックスがない場合、ルートオブジェクトを返す
            if (string.IsNullOrEmpty(prefix) || prefix == "/")
            {
                foreach (var root in targetScene.GetRootGameObjects())
                {
                    yield return "/" + root.name;
                }
                yield break;
            }

            // 最後の/までを親パス、それ以降を検索プレフィックスとして扱う
            var lastSlashIndex = prefix.LastIndexOf('/');
            string parentPath;
            string searchPrefix;

            if (lastSlashIndex <= 0)
            {
                parentPath = "/";
                searchPrefix = prefix.TrimStart('/');
            }
            else
            {
                parentPath = prefix.Substring(0, lastSlashIndex);
                searchPrefix = prefix.Substring(lastSlashIndex + 1);
            }

            // 親がルートの場合
            if (parentPath == "/" || string.IsNullOrEmpty(parentPath))
            {
                foreach (var root in targetScene.GetRootGameObjects())
                {
                    if (root.name.StartsWith(searchPrefix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        yield return "/" + root.name;
                    }
                }
                yield break;
            }

            // 親オブジェクトを解決
            var parent = Resolve(parentPath, targetScene);
            if (parent == null)
                yield break;

            // 子オブジェクトをフィルタリング
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                var child = parent.transform.GetChild(i);
                if (child.name.StartsWith(searchPrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    yield return parentPath + "/" + child.name;
                }
            }
        }

        /// <summary>
        /// シーンを名前で取得します
        /// </summary>
        /// <param name="sceneName">シーン名</param>
        /// <returns>見つかったシーン（見つからない場合はnull）</returns>
        public static Scene? GetSceneByName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return SceneManager.GetActiveScene();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName && scene.isLoaded)
                {
                    return scene;
                }
            }

            return null;
        }

        /// <summary>
        /// ロード済みのシーン一覧を取得します
        /// </summary>
        /// <returns>シーン情報の列挙（名前、アクティブかどうか）</returns>
        public static IEnumerable<(string name, bool isActive)> GetLoadedScenes()
        {
            var activeScene = SceneManager.GetActiveScene();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    yield return (scene.name, scene == activeScene);
                }
            }
        }
    }
}
