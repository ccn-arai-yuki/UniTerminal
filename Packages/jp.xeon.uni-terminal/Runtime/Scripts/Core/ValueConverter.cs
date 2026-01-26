using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using Xeon.UniTerminal.Assets;
using Xeon.UniTerminal.UnityCommands;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// 文字列からUnity/C#の型への変換を行うユーティリティ
    /// </summary>
    public static class ValueConverter
    {
        /// <summary>
        /// 文字列を指定された型に変換します
        /// </summary>
        /// <param name="value">変換する文字列</param>
        /// <param name="targetType">変換先の型</param>
        /// <returns>変換された値</returns>
        /// <exception cref="NotSupportedException">サポートされていない型の場合</exception>
        /// <exception cref="FormatException">変換に失敗した場合</exception>
        public static object Convert(string value, Type targetType)
        {
            // null/空チェック
            if (string.IsNullOrEmpty(value))
            {
                if (targetType.IsValueType)
                    return Activator.CreateInstance(targetType);
                return null;
            }

            // Nullable型の場合
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
                targetType = underlyingType;

            // 基本型
            if (TryConvertPrimitive(value, targetType, out var primitiveResult))
                return primitiveResult;

            // Unity構造体型
            if (TryConvertUnityStruct(value, targetType, out var unityStructResult))
                return unityStructResult;

            // Enum
            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, ignoreCase: true);

            // LayerMask
            if (targetType == typeof(LayerMask))
                return ParseLayerMask(value);

            // Unity Object参照型
            if (TryConvertUnityObject(value, targetType, out var unityObjectResult))
                return unityObjectResult;

            throw new NotSupportedException($"Cannot convert to type: {targetType.Name}");
        }

        /// <summary>
        /// 基本型への変換を試みます
        /// </summary>
        private static bool TryConvertPrimitive(string value, Type targetType, out object result)
        {
            result = null;

            if (targetType == typeof(int))
                result = int.Parse(value, CultureInfo.InvariantCulture);
            else if (targetType == typeof(float))
                result = float.Parse(value, CultureInfo.InvariantCulture);
            else if (targetType == typeof(double))
                result = double.Parse(value, CultureInfo.InvariantCulture);
            else if (targetType == typeof(long))
                result = long.Parse(value, CultureInfo.InvariantCulture);
            else if (targetType == typeof(bool))
                result = ParseBool(value);
            else if (targetType == typeof(string))
                result = value.Trim('"');
            else if (targetType == typeof(byte))
                result = byte.Parse(value, CultureInfo.InvariantCulture);
            else if (targetType == typeof(short))
                result = short.Parse(value, CultureInfo.InvariantCulture);
            else
                return false;

            return true;
        }

        /// <summary>
        /// Unity構造体型への変換を試みます
        /// </summary>
        private static bool TryConvertUnityStruct(string value, Type targetType, out object result)
        {
            result = null;

            if (targetType == typeof(Vector2))
                result = ParseVector2(value);
            else if (targetType == typeof(Vector3))
                result = ParseVector3(value);
            else if (targetType == typeof(Vector4))
                result = ParseVector4(value);
            else if (targetType == typeof(Vector2Int))
                result = ParseVector2Int(value);
            else if (targetType == typeof(Vector3Int))
                result = ParseVector3Int(value);
            else if (targetType == typeof(Color))
                result = ParseColor(value);
            else if (targetType == typeof(Color32))
                result = (Color32)ParseColor(value);
            else if (targetType == typeof(Quaternion))
                result = ParseQuaternion(value);
            else if (targetType == typeof(Rect))
                result = ParseRect(value);
            else if (targetType == typeof(Bounds))
                result = ParseBounds(value);
            else
                return false;

            return true;
        }

        /// <summary>
        /// Unity Object参照型への変換を試みます
        /// </summary>
        private static bool TryConvertUnityObject(string value, Type targetType, out object result)
        {
            result = null;

            if (typeof(GameObject).IsAssignableFrom(targetType))
                result = ParseGameObjectReference(value);
            else if (typeof(Component).IsAssignableFrom(targetType))
                result = ParseComponentReference(value, targetType);
            else if (targetType == typeof(Material))
                result = ParseMaterialReference(value);
            else if (targetType == typeof(Shader))
                result = ParseShaderReference(value);
            else if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                result = ParseAssetReference(value, targetType);
            else
                return false;

            return true;
        }

        /// <summary>
        /// 文字列を指定された型に変換を試みます
        /// </summary>
        public static bool TryConvert(string value, Type targetType, out object result)
        {
            try
            {
                result = Convert(value, targetType);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        private static bool ParseBool(string value)
        {
            var lower = value.ToLowerInvariant();
            if (lower == "true" || lower == "1" || lower == "yes" || lower == "on")
                return true;
            if (lower == "false" || lower == "0" || lower == "no" || lower == "off")
                return false;
            throw new FormatException($"Cannot parse '{value}' as bool");
        }

        /// <summary>
        /// 値がnullまたはnone指定かどうかを判定します
        /// </summary>
        private static bool IsNullOrNone(string value)
        {
            var lower = value.ToLowerInvariant();
            return lower == "null" || lower == "none";
        }

        /// <summary>
        /// インスタンスID形式（#12345）のパースを試みます
        /// </summary>
        private static bool TryParseInstanceId(string value, out int instanceId)
        {
            instanceId = 0;
            return value.StartsWith("#") && int.TryParse(value.Substring(1), out instanceId);
        }

        private static Vector2 ParseVector2(string value)
        {
            var parts = value.Split(',');
            if (parts.Length == 1)
            {
                var single = float.Parse(parts[0], CultureInfo.InvariantCulture);
                return new Vector2(single, single);
            }
            return new Vector2(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture)
            );
        }

        private static Vector3 ParseVector3(string value)
        {
            var parts = value.Split(',');
            if (parts.Length == 1)
            {
                var single = float.Parse(parts[0], CultureInfo.InvariantCulture);
                return new Vector3(single, single, single);
            }
            if (parts.Length == 2)
            {
                return new Vector3(
                    float.Parse(parts[0], CultureInfo.InvariantCulture),
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    0f
                );
            }
            return new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture)
            );
        }

        private static Vector4 ParseVector4(string value)
        {
            var parts = value.Split(',');
            return new Vector4(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture),
                parts.Length > 3 ? float.Parse(parts[3], CultureInfo.InvariantCulture) : 0f
            );
        }

        private static Vector2Int ParseVector2Int(string value)
        {
            var parts = value.Split(',');
            return new Vector2Int(
                int.Parse(parts[0], CultureInfo.InvariantCulture),
                int.Parse(parts[1], CultureInfo.InvariantCulture)
            );
        }

        private static Vector3Int ParseVector3Int(string value)
        {
            var parts = value.Split(',');
            return new Vector3Int(
                int.Parse(parts[0], CultureInfo.InvariantCulture),
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                int.Parse(parts[2], CultureInfo.InvariantCulture)
            );
        }

        private static Color ParseColor(string value)
        {
            // 名前で指定
            switch (value.ToLowerInvariant())
            {
                case "red": return Color.red;
                case "green": return Color.green;
                case "blue": return Color.blue;
                case "white": return Color.white;
                case "black": return Color.black;
                case "yellow": return Color.yellow;
                case "cyan": return Color.cyan;
                case "magenta": return Color.magenta;
                case "gray":
                case "grey": return Color.gray;
                case "clear": return Color.clear;
            }

            // HTMLカラーコード
            if (value.StartsWith("#"))
            {
                if (ColorUtility.TryParseHtmlString(value, out var htmlColor))
                    return htmlColor;
            }

            // RGBA値で指定
            var parts = value.Split(',');
            return new Color(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture),
                parts.Length > 3 ? float.Parse(parts[3], CultureInfo.InvariantCulture) : 1f
            );
        }

        private static Quaternion ParseQuaternion(string value)
        {
            // euler: プレフィックスでEuler角指定
            if (value.StartsWith("euler:", StringComparison.OrdinalIgnoreCase))
            {
                var euler = ParseVector3(value.Substring(6));
                return Quaternion.Euler(euler);
            }

            // 直接Quaternion値
            var parts = value.Split(',');
            if (parts.Length == 3)
            {
                // 3つの値の場合はEuler角として扱う
                return Quaternion.Euler(
                    float.Parse(parts[0], CultureInfo.InvariantCulture),
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture)
                );
            }
            return new Quaternion(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture),
                float.Parse(parts[3], CultureInfo.InvariantCulture)
            );
        }

        private static Rect ParseRect(string value)
        {
            var parts = value.Split(',');
            return new Rect(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture),
                float.Parse(parts[3], CultureInfo.InvariantCulture)
            );
        }

        private static Bounds ParseBounds(string value)
        {
            // center:x,y,z;size:x,y,z 形式
            var parts = value.Split(';');
            Vector3 center = Vector3.zero;
            Vector3 size = Vector3.one;

            foreach (var part in parts)
            {
                if (part.StartsWith("center:", StringComparison.OrdinalIgnoreCase))
                {
                    center = ParseVector3(part.Substring(7));
                }
                else if (part.StartsWith("size:", StringComparison.OrdinalIgnoreCase))
                {
                    size = ParseVector3(part.Substring(5));
                }
            }

            return new Bounds(center, size);
        }

        private static LayerMask ParseLayerMask(string value)
        {
            // 数値指定
            if (int.TryParse(value, out int layerValue))
            {
                return layerValue;
            }

            // レイヤー名指定
            int layer = LayerMask.NameToLayer(value);
            if (layer != -1)
            {
                return 1 << layer;
            }

            throw new FormatException($"Unknown layer: {value}");
        }

        /// <summary>
        /// パスからGameObject参照を解決します
        /// </summary>
        private static GameObject ParseGameObjectReference(string value)
        {
            if (IsNullOrNone(value))
                return null;

            if (TryParseInstanceId(value, out int instanceId))
            {
                var obj = FindObjectByInstanceId<GameObject>(instanceId);
                if (obj == null)
                    throw new FormatException($"GameObject not found with instance ID: {value}");
                return obj;
            }

            var go = GameObjectPath.Resolve(value);
            if (go == null)
                throw new FormatException($"GameObject not found: {value}");
            return go;
        }

        /// <summary>
        /// パスからComponent参照を解決します
        /// </summary>
        private static Component ParseComponentReference(string value, Type componentType)
        {
            if (IsNullOrNone(value))
                return null;

            if (TryParseInstanceId(value, out int instanceId))
                return ResolveComponentByInstanceId(instanceId, componentType, value);

            // パス/コンポーネント形式 (例: /Player:Rigidbody)
            var colonIndex = value.IndexOf(':');
            if (colonIndex >= 0)
                return ResolveComponentByPathAndType(value, colonIndex, componentType);

            // パスのみの場合、そのGameObjectから指定型のコンポーネントを取得
            return ResolveComponentByPath(value, componentType);
        }

        private static Component ResolveComponentByInstanceId(int instanceId, Type componentType, string originalValue)
        {
            var obj = FindObjectByInstanceId<Component>(instanceId);
            if (obj == null)
                throw new FormatException($"Component not found with instance ID: {originalValue}");
            if (!componentType.IsAssignableFrom(obj.GetType()))
                throw new FormatException($"Component type mismatch: expected {componentType.Name}, got {obj.GetType().Name}");
            return obj;
        }

        private static Component ResolveComponentByPathAndType(string value, int colonIndex, Type componentType)
        {
            var goPath = value.Substring(0, colonIndex);
            var typeName = value.Substring(colonIndex + 1);

            var go = GameObjectPath.Resolve(goPath);
            if (go == null)
                throw new FormatException($"GameObject not found: {goPath}");

            var compType = TypeResolver.ResolveComponentType(typeName);
            if (compType == null || !componentType.IsAssignableFrom(compType))
                throw new FormatException($"Component type mismatch: expected {componentType.Name}, got {typeName}");

            var comp = go.GetComponent(compType);
            if (comp == null)
                throw new FormatException($"Component '{typeName}' not found on {goPath}");
            return comp;
        }

        private static Component ResolveComponentByPath(string value, Type componentType)
        {
            var gameObject = GameObjectPath.Resolve(value);
            if (gameObject == null)
                throw new FormatException($"GameObject not found: {value}");

            var component = gameObject.GetComponent(componentType);
            if (component == null)
                throw new FormatException($"Component '{componentType.Name}' not found on {value}");
            return component;
        }

        /// <summary>
        /// Material参照を解決します
        /// </summary>
        private static Material ParseMaterialReference(string value)
        {
            if (IsNullOrNone(value))
                return null;

            if (TryParseInstanceId(value, out int instanceId))
                return ResolveMaterialByInstanceId(instanceId, value);

            // パス/コンポーネント形式でRendererから取得
            // 例: /Cube:MeshRenderer.material または /Cube:MeshRenderer.materials[0]
            if (value.Contains(":"))
                return ResolveMaterialFromRenderer(value);

            // 名前でリソースから検索（フォールバック）
            return ResolveMaterialByName(value);
        }

        private static Material ResolveMaterialByInstanceId(int instanceId, string originalValue)
        {
            // ロード済みアセットレジストリから検索
            var entry = AssetManager.Instance.Registry.GetByInstanceId(instanceId);
            if (entry != null && entry.Asset is Material regMat)
                return regMat;

            // Resources.FindObjectsOfTypeAllから検索
            foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (mat.GetInstanceID() == instanceId)
                    return mat;
            }

            throw new FormatException($"Material not found with instance ID: {originalValue}");
        }

        private static Material ResolveMaterialFromRenderer(string value)
        {
            var colonIdx = value.IndexOf(':');
            var goPath = value.Substring(0, colonIdx);
            var rest = value.Substring(colonIdx + 1);

            var go = GameObjectPath.Resolve(goPath);
            if (go == null)
                throw new FormatException($"GameObject not found: {goPath}");

            if (!rest.Contains(".material"))
                throw new FormatException($"Invalid material reference format: {value}");

            var dotIdx = rest.IndexOf('.');
            var rendererTypeName = rest.Substring(0, dotIdx);
            var propPart = rest.Substring(dotIdx + 1);

            var renderer = ResolveRenderer(go, rendererTypeName, goPath);
            return GetMaterialFromRenderer(renderer, propPart);
        }

        private static Renderer ResolveRenderer(GameObject go, string rendererTypeName, string goPath)
        {
            var rendererType = TypeResolver.ResolveComponentType(rendererTypeName);
            if (rendererType == null || !typeof(Renderer).IsAssignableFrom(rendererType))
                throw new FormatException($"'{rendererTypeName}' is not a Renderer type");

            var renderer = go.GetComponent(rendererType) as Renderer;
            if (renderer == null)
                throw new FormatException($"Renderer '{rendererTypeName}' not found on {goPath}");

            return renderer;
        }

        private static Material GetMaterialFromRenderer(Renderer renderer, string propPart)
        {
            // materials[n] 形式
            if (propPart.StartsWith("materials["))
            {
                var indexStr = propPart.Substring(10, propPart.Length - 11);
                if (int.TryParse(indexStr, out int index) && index >= 0 && index < renderer.sharedMaterials.Length)
                    return renderer.sharedMaterials[index];
                throw new FormatException($"Material index out of range: {index}");
            }

            // material 形式
            if (propPart == "material" || propPart == "sharedMaterial")
                return renderer.sharedMaterial;

            throw new FormatException($"Invalid material property: {propPart}");
        }

        private static Material ResolveMaterialByName(string name)
        {
            foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (mat.name == name)
                    return mat;
            }
            throw new FormatException($"Material not found: {name}");
        }

        /// <summary>
        /// 値を表示用文字列にフォーマットします
        /// </summary>
        public static string Format(object value)
        {
            if (value == null)
                return "(null)";

            // 配列/リストの特別処理
            if (value is Array array)
            {
                return FormatArray(array);
            }
            if (value is IList list && !(value is string))
            {
                return FormatList(list);
            }

            return value switch
            {
                Vector2 v => $"({v.x:F2}, {v.y:F2})",
                Vector3 v => $"({v.x:F2}, {v.y:F2}, {v.z:F2})",
                Vector4 v => $"({v.x:F2}, {v.y:F2}, {v.z:F2}, {v.w:F2})",
                Vector2Int v => $"({v.x}, {v.y})",
                Vector3Int v => $"({v.x}, {v.y}, {v.z})",
                Quaternion q => $"euler:({q.eulerAngles.x:F1}, {q.eulerAngles.y:F1}, {q.eulerAngles.z:F1})",
                Color c => $"({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})",
                Color32 c => $"({c.r}, {c.g}, {c.b}, {c.a})",
                Rect r => $"({r.x:F1}, {r.y:F1}, {r.width:F1}, {r.height:F1})",
                Bounds b => $"center:({b.center.x:F1}, {b.center.y:F1}, {b.center.z:F1});size:({b.size.x:F1}, {b.size.y:F1}, {b.size.z:F1})",
                string s => $"\"{s}\"",
                bool b => b.ToString().ToLowerInvariant(),
                float f => f.ToString("F2", CultureInfo.InvariantCulture),
                double d => d.ToString("F2", CultureInfo.InvariantCulture),
                GameObject go => go != null ? $"<GameObject:{GameObjectPath.GetPath(go)}>" : "(null)",
                Component comp => comp != null ? $"<{comp.GetType().Name}:{GameObjectPath.GetPath(comp.gameObject)}>" : "(null)",
                UnityEngine.Object obj => obj != null ? $"\"{obj.name}\"" : "(null)",
                _ => value.ToString()
            };
        }

        /// <summary>
        /// 配列を表示用文字列にフォーマットします
        /// </summary>
        private static string FormatArray(Array array)
        {
            if (array.Length == 0)
                return "[]";

            var elementType = array.GetType().GetElementType();
            if (array.Length <= 5)
            {
                var elements = new string[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    elements[i] = Format(array.GetValue(i));
                }
                return $"[{string.Join(", ", elements)}]";
            }

            return $"[{elementType.Name}[{array.Length}]]";
        }

        /// <summary>
        /// リストを表示用文字列にフォーマットします
        /// </summary>
        private static string FormatList(IList list)
        {
            if (list.Count == 0)
                return "[]";

            var listType = list.GetType();
            var elementType = listType.IsGenericType
                ? listType.GetGenericArguments()[0]
                : typeof(object);

            if (list.Count <= 5)
            {
                var elements = new string[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    elements[i] = Format(list[i]);
                }
                return $"[{string.Join(", ", elements)}]";
            }

            return $"[List<{elementType.Name}>[{list.Count}]]";
        }

        /// <summary>
        /// インスタンスIDからオブジェクトを検索します
        /// </summary>
        /// <typeparam name="T">検索対象の型（GameObjectまたはComponent）</typeparam>
        /// <param name="instanceId">検索するインスタンスID</param>
        /// <returns>見つかったオブジェクト、見つからない場合はnull</returns>
        private static T FindObjectByInstanceId<T>(int instanceId) where T : UnityEngine.Object
        {
            // GameObjectを検索
            if (typeof(T) == typeof(GameObject))
            {
                foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (go.GetInstanceID() == instanceId)
                        return go as T;
                }
                return null;
            }

            // Componentを検索
            if (typeof(Component).IsAssignableFrom(typeof(T)))
            {
                foreach (var comp in Resources.FindObjectsOfTypeAll<Component>())
                {
                    if (comp.GetInstanceID() == instanceId)
                        return comp as T;
                }
                return null;
            }

            return null;
        }

        /// <summary>
        /// アセット参照を解決します（Texture, Mesh, Sprite, AudioClip等）
        /// </summary>
        /// <param name="value">参照文字列（#instanceId, 名前, またはキー）</param>
        /// <param name="assetType">アセットの型</param>
        /// <returns>解決されたアセット</returns>
        private static UnityEngine.Object ParseAssetReference(string value, Type assetType)
        {
            if (IsNullOrNone(value))
                return null;

            if (TryParseInstanceId(value, out int instanceId))
                return ResolveAssetByInstanceId(instanceId, assetType, value);

            return ResolveAssetByName(value, assetType);
        }

        private static UnityEngine.Object ResolveAssetByInstanceId(int instanceId, Type assetType, string originalValue)
        {
            // まずロード済みアセットレジストリから検索
            var entry = AssetManager.Instance.Registry.GetByInstanceId(instanceId);
            if (entry != null && assetType.IsAssignableFrom(entry.AssetType))
                return entry.Asset;

            // Resources.FindObjectsOfTypeAllから検索
            foreach (var obj in Resources.FindObjectsOfTypeAll(assetType))
            {
                if (obj.GetInstanceID() == instanceId)
                    return obj;
            }

            throw new FormatException($"{assetType.Name} not found with instance ID: {originalValue}");
        }

        private static UnityEngine.Object ResolveAssetByName(string name, Type assetType)
        {
            var registry = AssetManager.Instance.Registry;

            // ロード済みアセットレジストリから名前で検索
            if (registry.TryResolve(name, out var registryEntry))
            {
                if (assetType.IsAssignableFrom(registryEntry.AssetType))
                    return registryEntry.Asset;
            }

            // 同名のアセットが複数ある場合
            var byName = registry.GetByName(name);
            if (byName.Count > 1)
            {
                var matching = new System.Collections.Generic.List<LoadedAssetEntry>();
                foreach (var e in byName)
                {
                    if (assetType.IsAssignableFrom(e.AssetType))
                        matching.Add(e);
                }

                if (matching.Count == 1)
                    return matching[0].Asset;

                if (matching.Count > 1)
                    throw new FormatException($"Multiple {assetType.Name} assets found with name '{name}'. Use instance ID (#xxxxx) to specify.");
            }

            // Resources.FindObjectsOfTypeAllから名前で検索
            foreach (var obj in Resources.FindObjectsOfTypeAll(assetType))
            {
                if (obj.name == name)
                    return obj;
            }

            throw new FormatException($"{assetType.Name} not found: {name}");
        }

        /// <summary>
        /// Shader参照を解決します
        /// </summary>
        private static Shader ParseShaderReference(string value)
        {
            if (IsNullOrNone(value))
                return null;

            if (TryParseInstanceId(value, out int instanceId))
                return ResolveShaderByInstanceId(instanceId, value);

            return ResolveShaderByName(value);
        }

        private static Shader ResolveShaderByInstanceId(int instanceId, string originalValue)
        {
            foreach (var shader in Resources.FindObjectsOfTypeAll<Shader>())
            {
                if (shader.GetInstanceID() == instanceId)
                    return shader;
            }
            throw new FormatException($"Shader not found with instance ID: {originalValue}");
        }

        private static Shader ResolveShaderByName(string name)
        {
            // Shader.Findで検索（シェーダー名で直接検索）
            var found = Shader.Find(name);
            if (found != null)
                return found;

            // Resources.FindObjectsOfTypeAllから名前で検索
            foreach (var shader in Resources.FindObjectsOfTypeAll<Shader>())
            {
                if (shader.name == name)
                    return shader;
            }

            throw new FormatException($"Shader not found: {name}");
        }
    }
}
