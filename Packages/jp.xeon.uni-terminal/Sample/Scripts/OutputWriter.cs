using System;
using System.Threading;
using System.Threading.Tasks;
using Xeon.Common.FlyweightScrollView.Model;
using Xeon.UniTerminal.Common;

namespace Xeon.UniTerminal.Sample
{
    /// <summary>
    /// ターミナル出力をバッファーに書き込むライター
    /// </summary>
    public class OutputWriter : IAsyncTextWriter
    {
        private readonly CircularBuffer<OutputData> buffer;
        private readonly bool isError;
        private readonly Func<int> getMaxCharsPerLine;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="buffer">出力先のバッファ</param>
        /// <param name="isError">エラー出力かどうか</param>
        /// <param name="getMaxCharsPerLine">1行当たりの最大文字数を取得する関数</param>
        public OutputWriter(CircularBuffer<OutputData> buffer, bool isError = false, Func<int> getMaxCharsPerLine = null)
        {
            this.buffer = buffer;
            this.isError = isError;
            this.getMaxCharsPerLine = getMaxCharsPerLine;
        }

        /// <summary>
        /// テキストを非同期で書き込む(改行なし)
        /// </summary>
        /// <param name="text">書き込むテキスト</param>
        /// <param name="ct">キャンセルトークン</param>
        public Task WriteAsync(string text, CancellationToken ct = default)
        {
            var maxChars = getMaxCharsPerLine?.Invoke() ?? 0;
            WriteWrappedLine(text, maxChars, isNotify: true);
            return Task.CompletedTask;
        }

        /// <summary>
        /// テキスト行を非同期で書き込む
        /// </summary>
        /// <param name="line">書き込む行</param>
        /// <param name="ct">キャンセルトークン</param>
        public Task WriteLineAsync(string line, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(line))
            {
                buffer.PushFront(new OutputData(string.Empty, isError));
                return Task.CompletedTask;
            }

            var maxChars = getMaxCharsPerLine?.Invoke() ?? 0;
            var rawLines = line.Split('\n');

            if (rawLines.Length > 1)
            {
                for (var i = 0; i < rawLines.Length - 1; i++)
                    WriteWrappedLine(rawLines[i], maxChars, isNotify: false);

                WriteWrappedLine(rawLines[rawLines.Length - 1], maxChars, isNotify: true);
            }
            else
            {
                WriteWrappedLine(rawLines[0], maxChars, isNotify: true);
            }

            return Task.CompletedTask;
        }

        public void Clear()
        {
            buffer.Clear();
        }

        /// <summary>
        /// ワードラップを適用して行をを書き込む
        /// </summary>
        /// <param name="text">元のテキスト</param>
        /// <param name="maxChars">一行で表示可能な最大文字数</param>
        /// <param name="isNotify">変更通知を発火するかどうか</param>
        private void WriteWrappedLine(string text, int maxChars, bool isNotify)
        {
            if (maxChars <= 0 || string.IsNullOrEmpty(text) || text.Length <= maxChars)
            {
                buffer.PushFront(new OutputData(text ?? string.Empty, isError), isNotify);
                return;
            }

            var wrappedLines = TextMeshUtility.WrapText(text, maxChars);

            if (wrappedLines.Count > 1)
            {
                for (var i = 0; i < wrappedLines.Count - 1; i++)
                    buffer.PushFront(new OutputData(wrappedLines[i], isError), isNotify: false);

                buffer.PushFront(new OutputData(wrappedLines[wrappedLines.Count - 1], isError), isNotify);
            }
            else if (wrappedLines.Count == 1)
            {
                buffer.PushFront(new OutputData(wrappedLines[0], isError), isNotify);
            }
        }
    }
}
