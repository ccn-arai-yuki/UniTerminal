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

        public StringBuilderTextWriter()
        {
            builder = new StringBuilder();
        }

        public StringBuilderTextWriter(StringBuilder builder)
        {
            this.builder = builder;
        }

        public Task WriteLineAsync(string line, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            builder.AppendLine(line);
            return Task.CompletedTask;
        }

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
