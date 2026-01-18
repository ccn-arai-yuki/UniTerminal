using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Xeon.UniTerminal.Common
{
    public static class TextMeshUtility
    {
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
        /// <param name="maxCharsPerLine">1行あたりの最大文字数</param>
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
            if (text.Length <= maxCharsPerLine)
            {
                lines.Add(text);
                return lines;
            }

            var remaining = text;
            while (remaining.Length > 0)
            {
                if (remaining.Length <= maxCharsPerLine)
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
                    // スペースが見つからない場合は強制的に切る
                    lines.Add(remaining.Substring(0, maxCharsPerLine));
                    remaining = remaining.Substring(maxCharsPerLine);
                }
            }

            return lines;
        }

        /// <summary>
        /// ワードラップのための分割位置を探す
        /// </summary>
        private static int FindBreakIndex(string text, int maxChars)
        {
            // 最大文字数以内で最後のスペースを探す
            var lastSpace = -1;
            for (var i = 0; i < maxChars && i < text.Length; i++)
            {
                if (text[i] == ' ')
                {
                    lastSpace = i;
                }
            }

            // スペースが見つかった場合はその位置で分割
            if (lastSpace > 0)
            {
                return lastSpace;
            }

            // スペースが見つからない場合は0を返す（強制分割）
            return 0;
        }
    }
}
