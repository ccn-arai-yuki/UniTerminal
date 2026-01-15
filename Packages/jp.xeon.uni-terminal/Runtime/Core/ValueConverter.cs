using System;
using System.Globalization;
using UnityEngine;

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
        /// 値を表示用文字列にフォーマットします。
        /// </summary>
        public static string Format(object value)
        {
            if (value == null)
                return "(null)";

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
                UnityEngine.Object obj => obj != null ? $"\"{obj.name}\"" : "(null)",
                _ => value.ToString()
            };
        }
    }
}
