#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Threading;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;
    using Cysharp.Threading.Tasks.Linq;

    /// <summary>
    /// Task版のIAsyncTextReaderをUniTask版のIUniTaskTextReaderに変換するアダプター
    /// </summary>
    public class TaskTextReaderAdapter : IUniTaskTextReader
    {
        private readonly IAsyncTextReader inner;

        /// <summary>
        /// 内部のTask版リーダー
        /// </summary>
        public IAsyncTextReader Inner => inner;

        /// <summary>
        /// Task版リーダーをラップしてUniTask版として使用できるようにします
        /// </summary>
        /// <param name="inner">ラップするTask版リーダー</param>
        public TaskTextReaderAdapter(IAsyncTextReader inner)
        {
            this.inner = inner;
        }

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<string> ReadLinesAsync(CancellationToken ct = default)
        {
            var source = inner.ReadLinesAsync(ct);
            return UniTaskAsyncEnumerable.Create<string>(async (writer, token) =>
            {
                var enumerator = source.GetAsyncEnumerator(token);
                try
                {
                    while (await enumerator.MoveNextAsync())
                    {
                        token.ThrowIfCancellationRequested();
                        await writer.YieldAsync(enumerator.Current);
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync();
                }
            });
        }
    }
}
#endif
