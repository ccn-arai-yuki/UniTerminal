using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// 行をリストに収集するテキストライター。
    /// パイプライン接続に便利です。
    /// </summary>
    public class ListTextWriter : IAsyncTextWriter
    {
        private readonly List<string> _lines = new List<string>();
        private string _partial = "";

        public IReadOnlyList<string> Lines => _lines;

        public Task WriteLineAsync(string line, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _lines.Add(_partial + line);
            _partial = "";
            return Task.CompletedTask;
        }

        public Task WriteAsync(string text, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _partial += text;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 未完了の行を完全な行としてフラッシュします。
        /// </summary>
        public void Flush()
        {
            if (!string.IsNullOrEmpty(_partial))
            {
                _lines.Add(_partial);
                _partial = "";
            }
        }

        public void Clear()
        {
            _lines.Clear();
            _partial = "";
        }
    }
}
