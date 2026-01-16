#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// UniTask版のIUniTaskTextReaderをTask版のIAsyncTextReaderに変換するアダプター。
    /// </summary>
    public class UniTaskTextReaderAdapter : IAsyncTextReader
    {
        private readonly IUniTaskTextReader inner;

        /// <summary>
        /// 内部のUniTask版リーダー。
        /// </summary>
        public IUniTaskTextReader Inner => inner;

        /// <summary>
        /// UniTask版リーダーをラップしてTask版として使用できるようにします。
        /// </summary>
        /// <param name="inner">ラップするUniTask版リーダー。</param>
        public UniTaskTextReaderAdapter(IUniTaskTextReader inner)
        {
            this.inner = inner;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var line in inner.ReadLinesAsync(ct).WithCancellation(ct))
            {
                yield return line;
            }
        }
    }
}
#endif
