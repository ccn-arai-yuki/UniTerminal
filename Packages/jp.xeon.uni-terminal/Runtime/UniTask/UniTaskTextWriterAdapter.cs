#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// UniTask版のIUniTaskTextWriterをTask版のIAsyncTextWriterに変換するアダプター。
    /// </summary>
    public class UniTaskTextWriterAdapter : IAsyncTextWriter
    {
        private readonly IUniTaskTextWriter inner;

        /// <summary>
        /// 内部のUniTask版ライター。
        /// </summary>
        public IUniTaskTextWriter Inner => inner;

        /// <summary>
        /// UniTask版ライターをラップしてTask版として使用できるようにします。
        /// </summary>
        /// <param name="inner">ラップするUniTask版ライター。</param>
        public UniTaskTextWriterAdapter(IUniTaskTextWriter inner)
        {
            this.inner = inner;
        }

        /// <inheritdoc/>
        public Task WriteLineAsync(string line, CancellationToken ct = default)
        {
            return inner.WriteLineAsync(line, ct).AsTask();
        }

        /// <inheritdoc/>
        public Task WriteAsync(string text, CancellationToken ct = default)
        {
            return inner.WriteAsync(text, ct).AsTask();
        }
    }
}
#endif
