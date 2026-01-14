using System.Collections.Generic;
using System.Threading;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// 行のリストから読み取るテキストリーダー。
    /// パイプライン接続に使用されます。
    /// </summary>
    public class ListTextReader : IAsyncTextReader
    {
        private readonly IReadOnlyList<string> _lines;

        public ListTextReader(IReadOnlyList<string> lines)
        {
            _lines = lines;
        }

        public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var line in _lines)
            {
                ct.ThrowIfCancellationRequested();
                yield return line;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
