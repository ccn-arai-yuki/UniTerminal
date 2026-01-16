#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Threading;
using Xeon.Common.FlyweightScrollView.Model;
using Xeon.UniTerminal.UniTask;

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

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="buffer">出力先のバッファ</param>
        /// <param name="isError">エラー出力かどうか</param>
        public OutputWriter(CircularBuffer<OutputData> buffer, bool isError = false)
        {
            this.buffer = buffer;
            this.isError = isError;
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
                buffer.Add(new OutputData(string.Empty, isError));
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }

            var lines = line.Split('\n');
            foreach (var l in lines)
            {
                buffer.Add(new OutputData(l, isError));
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
            buffer.Add(new OutputData(text, isError));
            return Cysharp.Threading.Tasks.UniTask.CompletedTask;
        }
    }
}
#endif
