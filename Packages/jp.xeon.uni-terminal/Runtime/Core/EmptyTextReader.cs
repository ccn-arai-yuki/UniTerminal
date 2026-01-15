using System.Collections.Generic;
using System.Threading;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// 行を返さない空のテキストリーダー。
    /// stdinが提供されない場合に使用されます。
    /// </summary>
    public class EmptyTextReader : IAsyncTextReader
    {
        public static readonly EmptyTextReader Instance = new EmptyTextReader();

        public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await System.Threading.Tasks.Task.CompletedTask;
            yield break;
        }
    }
}
