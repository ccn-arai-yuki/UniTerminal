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
        private readonly StringBuilder _builder;

        public StringBuilderTextWriter()
        {
            _builder = new StringBuilder();
        }

        public StringBuilderTextWriter(StringBuilder builder)
        {
            _builder = builder;
        }

        public Task WriteLineAsync(string line, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _builder.AppendLine(line);
            return Task.CompletedTask;
        }

        public Task WriteAsync(string text, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _builder.Append(text);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 書き込まれた内容を文字列として取得します。
        /// </summary>
        public override string ToString()
        {
            return _builder.ToString();
        }

        /// <summary>
        /// バッファをクリアします。
        /// </summary>
        public void Clear()
        {
            _builder.Clear();
        }
    }
}
