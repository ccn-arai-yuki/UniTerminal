#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Text;
using System.Threading;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;
    /// <summary>
    /// UniTask版のStringBuilderテキストライター。
    /// アロケーションを最小限に抑えた実装です。
    /// </summary>
    public class UniTaskStringBuilderTextWriter : IUniTaskTextWriter
    {
        private readonly StringBuilder builder;

        /// <summary>
        /// 新しいStringBuilderを使用してインスタンスを作成します。
        /// </summary>
        public UniTaskStringBuilderTextWriter()
        {
            builder = new StringBuilder();
        }

        /// <summary>
        /// 既存のStringBuilderを使用してインスタンスを作成します。
        /// </summary>
        /// <param name="builder">使用するStringBuilder。</param>
        public UniTaskStringBuilderTextWriter(StringBuilder builder)
        {
            this.builder = builder;
        }

        /// <inheritdoc/>
        public UniTask WriteLineAsync(string line, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            builder.AppendLine(line);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask WriteAsync(string text, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            builder.Append(text);
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 書き込まれた内容を文字列として取得します。
        /// </summary>
        public override string ToString()
        {
            return builder.ToString();
        }

        /// <summary>
        /// バッファをクリアします。
        /// </summary>
        public void Clear()
        {
            builder.Clear();
        }

        /// <summary>
        /// 現在の長さを取得します。
        /// </summary>
        public int Length => builder.Length;
    }
}
#endif
