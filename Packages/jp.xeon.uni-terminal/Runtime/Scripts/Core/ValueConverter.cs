using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using Xeon.UniTerminal.UnityCommands;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// 文字列からUnity/C#の型への変換を行うユーティリティ。
    /// </summary>
    public static class ValueConverter
    {
        /// <summary>
        /// 文字列を指定された型に変換します。
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
            {
                targetType = underlyingType;
            }

            // 基本型
            if (targetType == typeof(int))
                return int.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(float))
                return float.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(double))
                return double.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(long))
                return long.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(bool))
                return ParseBool(value);
            if (targetType == typeof(string))
                return value.Trim('"');
            if (targetType == typeof(byte))
                return byte.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(short))
                return short.Parse(value, CultureInfo.InvariantCulture);

            // Unity型
            if (targetType == typeof(Vector2))
                return ParseVector2(value);
            if (targetType == typeof(Vector3))
                return ParseVector3(value);
            if (targetType == typeof(Vector4))
                return ParseVector4(value);
            if (targetType == typeof(Vector2Int))
                return ParseVector2Int(value);
            if (targetType == typeof(Vector3Int))
                return ParseVector3Int(value);
            if (targetType == typeof(Color))
                return ParseColor(value);
            if (targetType == typeof(Color32))
                return (Color32)ParseColor(value);
            if (targetType == typeof(Quaternion))
                return ParseQuaternion(value);
            if (targetType == typeof(Rect))
                return ParseRect(value);
            if (targetType == typeof(Bounds))
                return ParseBounds(value);

            // Enum
            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, ignoreCase: true);

            // LayerMask
            if (targetType == typeof(LayerMask))
                return ParseLayerMask(value);

            // Unity Object参照型
            if (typeof(GameObject).IsAssignableFrom(targetType))
                return ParseGameObjectReference(value);

            if (typeof(Component).IsAssignableFrom(targetType))
                return ParseComponentReference(value, targetType);

            // Material（特殊対応）
            if (targetType == typeof(Material))
                return ParseMaterialReference(value);

            throw new NotSupportedException($"Cannot convert to type: {targetType.Name}");
        }

        /// <summary>
        /// 文字列を指定された型に変換を試みます。
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
        /// パスからGameObject参照を解決します。
        /// </summary>
        private static GameObject ParseGameObjectReference(string value)
        {
            // null/none指定
            if (value.ToLowerInvariant() == "null" || value.ToLowerInvariant() == "none")
                return null;

            // パスで解決
            var go = GameObjectPath.Resolve(value);
            if (go == null)
            {
                throw new FormatException($"GameObject not found: {value}");
            }
            return go;
        }

        /// <summary>
        /// パスからComponent参照を解決します。
        /// </summary>
        private static Component ParseComponentReference(string value, Type componentType)
        {
            // null/none指定
            if (value.ToLowerInvariant() == "null" || value.ToLowerInvariant() == "none")
                return null;

            // パス/コンポーネント形式 (例: /Player:Rigidbody)
            var parts = value.Split(':');
            if (parts.Length == 2)
            {
                var go = GameObjectPath.Resolve(parts[0]);
                if (go == null)
                {
                    throw new FormatException($"GameObject not found: {parts[0]}");
                }

                var compType = TypeResolver.ResolveComponentType(parts[1]);
                if (compType == null || !componentType.IsAssignableFrom(compType))
                {
                    throw new FormatException($"Component type mismatch: expected {componentType.Name}, got {parts[1]}");
                }

                var comp = go.GetComponent(compType);
                if (comp == null)
                {
                    throw new FormatException($"Component '{parts[1]}' not found on {parts[0]}");
                }
                return comp;
            }

            // パスのみの場合、そのGameObjectから指定型のコンポーネントを取得
            var gameObject = GameObjectPath.Resolve(value);
            if (gameObject == null)
            {
                throw new FormatException($"GameObject not found: {value}");
            }

            var component = gameObject.GetComponent(componentType);
            if (component == null)
            {
                throw new FormatException($"Component '{componentType.Name}' not found on {value}");
            }
            return component;
        }

        /// <summary>
        /// Material参照を解決します。
        /// </summary>
        private static Material ParseMaterialReference(string value)
        {
            // null/none指定
            if (value.ToLowerInvariant() == "null" || value.ToLowerInvariant() == "none")
                return null;

            // パス/コンポーネント形式でRendererから取得
            // 例: /Cube:MeshRenderer.material または /Cube:MeshRenderer.materials[0]
            if (value.Contains(":"))
            {
                var colonIdx = value.IndexOf(':');
                var goPath = value.Substring(0, colonIdx);
                var rest = value.Substring(colonIdx + 1);

                var go = GameObjectPath.Resolve(goPath);
                if (go == null)
                {
                    throw new FormatException($"GameObject not found: {goPath}");
                }

                // Renderer.material or Renderer.materials[n]
                if (rest.Contains(".material"))
                {
                    var dotIdx = rest.IndexOf('.');
                    var rendererTypeName = rest.Substring(0, dotIdx);
                    var rendererType = TypeResolver.ResolveComponentType(rendererTypeName);

                    if (rendererType == null || !typeof(Renderer).IsAssignableFrom(rendererType))
                    {
                        throw new FormatException($"'{rendererTypeName}' is not a Renderer type");
                    }

                    var renderer = go.GetComponent(rendererType) as Renderer;
                    if (renderer == null)
                    {
                        throw new FormatException($"Renderer '{rendererTypeName}' not found on {goPath}");
                    }

                    // materials[n] 形式
                    var propPart = rest.Substring(dotIdx + 1);
                    if (propPart.StartsWith("materials["))
                    {
                        var indexStr = propPart.Substring(10, propPart.Length - 11);
                        if (int.TryParse(indexStr, out int index) && index >= 0 && index < renderer.sharedMaterials.Length)
                        {
                            return renderer.sharedMaterials[index];
                        }
                        throw new FormatException($"Material index out of range: {index}");
                    }

                    // material 形式
                    if (propPart == "material" || propPart == "sharedMaterial")
                    {
                        return renderer.sharedMaterial;
                    }
                }

                throw new FormatException($"Invalid material reference format: {value}");
            }

            // 名前でリソースから検索（フォールバック）
            var materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var mat in materials)
            {
                if (mat.name == value)
                    return mat;
            }

            throw new FormatException($"Material not found: {value}");
        }

        /// <summary>
        /// 値を表示用文字列にフォーマットします。
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
        /// 配列を表示用文字列にフォーマットします。
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
        /// リストを表示用文字列にフォーマットします。
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
    }
}
