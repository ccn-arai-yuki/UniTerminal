using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// 文字列からComponent型を解決するユーティリティ
    /// </summary>
    public static class TypeResolver
    {
        private static readonly string[] DefaultNamespaces = new[]
        {
            "UnityEngine",
            "UnityEngine.UI",
            "UnityEngine.EventSystems",
            "TMPro"
        };

        /// <summary>
        /// 型名からComponent型を解決します
        /// </summary>
        /// <param name="typeName">型名（例: "Rigidbody", "MyGame.MyComponent"）</param>
        /// <param name="customNamespace">カスタム名前空間（オプション）</param>
        /// <returns>解決されたType、見つからない場合はnull</returns>
        public static Type ResolveComponentType(string typeName, string customNamespace = null)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            // フルネームで指定された場合
            if (typeName.Contains("."))
            {
                return FindTypeInAllAssemblies(typeName);
            }

            // カスタム名前空間が指定された場合
            if (!string.IsNullOrEmpty(customNamespace))
            {
                var type = FindTypeInAllAssemblies($"{customNamespace}.{typeName}");
                if (type != null) return type;
            }

            // デフォルト名前空間を検索
            foreach (var ns in DefaultNamespaces)
            {
                var type = FindTypeInAllAssemblies($"{ns}.{typeName}");
                if (type != null) return type;
            }

            // 名前空間なしで全アセンブリ検索（名前のみマッチ）
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                                       typeof(Component).IsAssignableFrom(t));
                    if (type != null) return type;
                }
                catch (ReflectionTypeLoadException)
                {
                    // 一部のアセンブリはロードに失敗する可能性
                    continue;
                }
                catch
                {
                    // その他のエラーも無視
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// フルネームで全アセンブリから型を検索します
        /// </summary>
        private static Type FindTypeInAllAssemblies(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(fullName);
                    if (type != null && typeof(Component).IsAssignableFrom(type))
                        return type;
                }
                catch
                {
                    // エラーは無視
                    continue;
                }
            }
            return null;
        }

        /// <summary>
        /// 一般的なコンポーネント型名のリストを取得します（補完用）
        /// </summary>
        public static string[] GetCommonComponentNames()
        {
            return new[]
            {
                // Physics
                "Rigidbody", "Rigidbody2D",
                "BoxCollider", "SphereCollider", "CapsuleCollider", "MeshCollider",
                "BoxCollider2D", "CircleCollider2D", "PolygonCollider2D",
                "CharacterController",

                // Audio
                "AudioSource", "AudioListener",

                // Rendering
                "Camera", "Light",
                "MeshFilter", "MeshRenderer", "SkinnedMeshRenderer",
                "SpriteRenderer", "LineRenderer", "TrailRenderer",
                "ParticleSystem",

                // Animation
                "Animator", "Animation",

                // UI
                "Canvas", "CanvasScaler", "GraphicRaycaster",
                "Image", "RawImage", "Text",
                "Button", "Toggle", "Slider", "Scrollbar", "Dropdown", "InputField",
                "ScrollRect", "Mask", "RectMask2D",
                "LayoutGroup", "HorizontalLayoutGroup", "VerticalLayoutGroup", "GridLayoutGroup",
                "ContentSizeFitter", "AspectRatioFitter",

                // Misc
                "EventSystem", "StandaloneInputModule"
            };
        }

        /// <summary>
        /// 型名からアセット型を解決します
        /// </summary>
        /// <param name="typeName">型名（例: "Texture2D", "Material"）</param>
        /// <returns>解決されたType、見つからない場合はnull</returns>
        public static Type ResolveAssetType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            // フルネームで指定された場合
            if (typeName.Contains("."))
                return FindAssetTypeInAllAssemblies(typeName);

            // よく使われるアセット型のエイリアス
            var aliasType = typeName.ToLowerInvariant() switch
            {
                "texture" => typeof(Texture),
                "texture2d" => typeof(Texture2D),
                "sprite" => typeof(Sprite),
                "material" => typeof(Material),
                "mesh" => typeof(Mesh),
                "audioclip" or "audio" => typeof(AudioClip),
                "shader" => typeof(Shader),
                "font" => typeof(Font),
                "prefab" or "gameobject" => typeof(GameObject),
                "animationclip" or "animation" => typeof(AnimationClip),
                "scriptableobject" or "so" => typeof(ScriptableObject),
                "textasset" or "text" => typeof(TextAsset),
                _ => null
            };

            if (aliasType != null)
                return aliasType;

            // デフォルト名前空間を検索
            foreach (var ns in DefaultNamespaces)
            {
                var type = FindAssetTypeInAllAssemblies($"{ns}.{typeName}");
                if (type != null)
                    return type;
            }

            // 名前空間なしで全アセンブリ検索
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                                       typeof(UnityEngine.Object).IsAssignableFrom(t));
                    if (type != null)
                        return type;
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// フルネームで全アセンブリからアセット型を検索します
        /// </summary>
        private static Type FindAssetTypeInAllAssemblies(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(fullName);
                    if (type != null && typeof(UnityEngine.Object).IsAssignableFrom(type))
                        return type;
                }
                catch
                {
                    continue;
                }
            }
            return null;
        }

        /// <summary>
        /// 一般的なアセット型名のリストを取得します（補完用）
        /// </summary>
        public static string[] GetCommonAssetTypeNames()
        {
            return new[]
            {
                "Texture2D", "Texture", "Sprite", "Material", "Mesh",
                "AudioClip", "Shader", "Font", "GameObject", "Prefab",
                "AnimationClip", "ScriptableObject", "TextAsset"
            };
        }
    }
}
