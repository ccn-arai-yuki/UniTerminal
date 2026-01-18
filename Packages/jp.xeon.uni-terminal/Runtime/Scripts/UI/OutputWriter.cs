#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System;
using System.Threading;
using Xeon.Common.FlyweightScrollView.Model;
using Xeon.UniTerminal.UniTask;
using Xeon.UniTerminal.Common;

namespace Xeon.UniTerminal
{
    using Cysharp.Threading.Tasks;
    /// <summary>
    /// ターミナル出力をCircularBufferに書き込むライター
    /// </summary>
    public class OutputWriter : IUniTaskTextWriter
    {
        private readonly CircularBuffer<OutputData> buffer;
        private readonly bool isError;
        private readonly Func<int> getMaxCharsPerLine;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="buffer">出力先のバッファ</param>
        /// <param name="isError">エラー出力かどうか</param>
        /// <param name="getMaxCharsPerLine">1行あたりの最大文字数を取得する関数</param>
        public OutputWriter(CircularBuffer<OutputData> buffer, bool isError = false, Func<int> getMaxCharsPerLine = null)
        {
            this.buffer = buffer;
            this.isError = isError;
            this.getMaxCharsPerLine = getMaxCharsPerLine;
        }

        /// <summary>
        /// テキスト行を非同期で書き込む
        /// </summary>
        /// <param name="line">書き込む行</param>
        /// <param name="ct">キャンセルトークン</param>
        public Cysharp.Threading.Tasks.UniTask WriteLineAsync(string line, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(line))
            {
                buffer.PushFront(new OutputData(string.Empty, isError));
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }

            var maxChars = getMaxCharsPerLine?.Invoke() ?? 0;
            var rawLines = line.Split('\n');

            foreach (var rawLine in rawLines)
            {
                WriteWrappedLine(rawLine, maxChars);
            }

            return Cysharp.Threading.Tasks.UniTask.CompletedTask;
        }

        /// <summary>
        /// テキストを非同期で書き込む（改行なし）
        /// </summary>
        /// <param name="text">書き込むテキスト</param>
        /// <param name="ct">キャンセルトークン</param>
        public Cysharp.Threading.Tasks.UniTask WriteAsync(string text, CancellationToken ct = default)
        {
            var maxChars = getMaxCharsPerLine?.Invoke() ?? 0;
            WriteWrappedLine(text, maxChars);
            return Cysharp.Threading.Tasks.UniTask.CompletedTask;
        }

        /// <summary>
        /// ワードラップを適用して行を書き込む
        /// </summary>
        private void WriteWrappedLine(string text, int maxChars)
        {
            if (maxChars <= 0 || string.IsNullOrEmpty(text) || text.Length <= maxChars)
            {
                buffer.PushFront(new OutputData(text ?? string.Empty, isError));
                return;
            }

            var wrappedLines = TextMeshUtility.WrapText(text, maxChars);
            // 逆順でPushFrontして正しい順序で表示
            for (var i = wrappedLines.Count - 1; i >= 0; i--)
            {
                buffer.PushFront(new OutputData(wrappedLines[i], isError));
            }
        }
    }
}
#endif
