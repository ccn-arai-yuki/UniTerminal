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
        private readonly IReadOnlyList<string> lines;

        /// <summary>
        /// 読み取り対象の行リストを受け取ります。
        /// </summary>
        /// <param name="lines">読み取り対象の行リスト。</param>
        public ListTextReader(IReadOnlyList<string> lines)
        {
            this.lines = lines;
        }

        /// <summary>
        /// 行を順に返します。
        /// </summary>
        /// <param name="ct">キャンセルトークン。</param>
        public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var line in lines)
            {
                ct.ThrowIfCancellationRequested();
                yield return line;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
