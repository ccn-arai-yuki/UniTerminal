using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Xeon.UniTerminal.Common
{
    /// <summary>
    /// TextMeshPro向けのテキスト計測・整形ユーティリティ。
    /// </summary>
    public static class TextMeshUtility
    {
        /// <summary>
        /// 指定文字列を利用して1行に収まる最大文字数を推定します。
        /// </summary>
        /// <param name="self">対象のTMP_Text。</param>
        /// <param name="sampleCharacters">測定に使用するサンプル文字列。</param>
        /// <returns>1行に収まる最大文字数。</returns>
        public static int GetMaxCharacterCountInOneLine(this TMP_Text self, string sampleCharacters)
        {
            float maxWidth = self.rectTransform.rect.width;

            int low = 0;
            int high = sampleCharacters.Length;
            int result = 0;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                string test = sampleCharacters.Substring(0, mid);

                Vector2 size = self.GetPreferredValues(test);

                if (size.x <= maxWidth)
                {
                    result = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return result;
        }

        /// <summary>
        /// テキストを指定した最大文字数でワードラップして複数行に分割する
        /// 英単語の途中では改行しないように試みる
        /// </summary>
        /// <param name="text">分割するテキスト</param>
        /// <param name="maxCharsPerLine">1行あたりの最大幅（全角=2、半角=1としてカウント）</param>
        /// <returns>分割された行のリスト</returns>
        public static List<string> WrapText(string text, int maxCharsPerLine)
        {
            var lines = new List<string>();

            if (string.IsNullOrEmpty(text) || maxCharsPerLine <= 0)
            {
                lines.Add(text ?? string.Empty);
                return lines;
            }

            // 1行に収まる場合はそのまま返す
            if (CalculateWidth(text) <= maxCharsPerLine)
            {
                lines.Add(text);
                return lines;
            }

            var remaining = text;
            while (remaining.Length > 0)
            {
                if (CalculateWidth(remaining) <= maxCharsPerLine)
                {
                    lines.Add(remaining);
                    break;
                }

                // 最大文字数以内で最後のスペースを探す
                var breakIndex = FindBreakIndex(remaining, maxCharsPerLine);

                if (breakIndex > 0)
                {
                    lines.Add(remaining.Substring(0, breakIndex));
                    // スペースで区切った場合はスペースをスキップ
                    remaining = remaining.Substring(breakIndex).TrimStart(' ');
                }
                else
                {
                    // breakIndexが0の場合は1文字も入らないので、最低1文字は取る
                    lines.Add(remaining.Substring(0, 1));
                    remaining = remaining.Substring(1);
                }
            }

            return lines;
        }

        /// <summary>
        /// ワードラップのための分割位置を探す
        /// </summary>
        private static int FindBreakIndex(string text, int maxChars)
        {
            // 最大表示幅（maxChars）以内で最後のスペースを探す
            // マルチバイト（全角）文字は幅2として扱う
            var lastSpace = -1;
            var width = 0;
            var lastFitIndex = 0;

            for (var i = 0; i < text.Length; i++)
            {
                var codepoint = 0;
                var charCount = 1;
                // サロゲートペアの処理
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    codepoint = char.ConvertToUtf32(text[i], text[i + 1]);
                    charCount = 2;
                }
                else
                {
                    codepoint = text[i];
                }

                int w = IsWide(codepoint) ? 2 : 1;

                if (width + w > maxChars)
                    break;

                // スペースは幅1
                if (codepoint == ' ')
                    lastSpace = i;

                width += w;
                lastFitIndex = i + charCount;

                // サロゲートペアをスキップするための調整
                if (charCount == 2)
                    i++;
            }

            // スペースが見つかった場合はその位置で分割（先頭のスペースは除外）
            if (lastSpace > 0)
                return lastSpace;

            // スペースが見つからない場合は、幅内に収まる最大文字数で分割
            return lastFitIndex;
        }

        // 簡易的な全角判定
        // 多くのCJK文字や全角記号を幅2と見なすための範囲をチェックする
        private static bool IsWide(int codepoint)
        {
            // 基本的な全角/広幅の範囲を列挙
            return (
                (codepoint >= 0x1100 && codepoint <= 0x115F) ||
                (codepoint >= 0x2329 && codepoint <= 0x232A) ||
                (codepoint >= 0x2E80 && codepoint <= 0xA4CF) ||
                (codepoint >= 0xAC00 && codepoint <= 0xD7A3) ||
                (codepoint >= 0xF900 && codepoint <= 0xFAFF) ||
                (codepoint >= 0xFE10 && codepoint <= 0xFE19) ||
                (codepoint >= 0xFE30 && codepoint <= 0xFE6F) ||
                (codepoint >= 0xFF00 && codepoint <= 0xFF60) ||
                (codepoint >= 0xFFE0 && codepoint <= 0xFFE6) ||
                (codepoint >= 0x20000 && codepoint <= 0x2FFFD) ||
                (codepoint >= 0x30000 && codepoint <= 0x3FFFD)
            );
        }

        /// <summary>
        /// テキストの表示幅を計算する（全角=2、半角=1）
        /// </summary>
        private static int CalculateWidth(string text)
        {
            var width = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var codepoint = 0;
                // サロゲートペアの処理
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    codepoint = char.ConvertToUtf32(text[i], text[i + 1]);
                    i++;
                }
                else
                {
                    codepoint = text[i];
                }

                width += IsWide(codepoint) ? 2 : 1;
            }
            return width;
        }
    }
}
