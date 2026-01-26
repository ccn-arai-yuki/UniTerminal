using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// StringBuilderに書き込むテキストライター。
    /// </summary>
    public class StringBuilderTextWriter : IAsyncTextWriter
    {
        private readonly StringBuilder builder;

        /// <summary>
        /// 内部にStringBuilderを生成して初期化します。
        /// </summary>
        public StringBuilderTextWriter()
        {
            builder = new StringBuilder();
        }

        /// <summary>
        /// 既存のStringBuilderを使用して初期化します。
        /// </summary>
        /// <param name="builder">書き込み対象のStringBuilder。</param>
        public StringBuilderTextWriter(StringBuilder builder)
        {
            this.builder = builder;
        }

        /// <summary>
        /// 行を追加します。
        /// </summary>
        /// <param name="line">書き込む行。</param>
        /// <param name="ct">キャンセルトークン。</param>
        public Task WriteLineAsync(string line, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            builder.AppendLine(line);
            return Task.CompletedTask;
        }

        /// <summary>
        /// テキストを追加します。
        /// </summary>
        /// <param name="text">書き込むテキスト。</param>
        /// <param name="ct">キャンセルトークン。</param>
        public Task WriteAsync(string text, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            builder.Append(text);
            return Task.CompletedTask;
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
    }
}
