#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Threading;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;
    
    /// <summary>
    /// Task版のIAsyncTextWriterをUniTask版のIUniTaskTextWriterに変換するアダプター。
    /// </summary>
    public class TaskTextWriterAdapter : IUniTaskTextWriter
    {
        private readonly IAsyncTextWriter inner;

        /// <summary>
        /// 内部のTask版ライター。
        /// </summary>
        public IAsyncTextWriter Inner => inner;

        /// <summary>
        /// Task版ライターをラップしてUniTask版として使用できるようにします。
        /// </summary>
        /// <param name="inner">ラップするTask版ライター。</param>
        public TaskTextWriterAdapter(IAsyncTextWriter inner)
        {
            this.inner = inner;
        }

        /// <inheritdoc/>
        public UniTask WriteLineAsync(string line, CancellationToken ct = default)
        {
            return inner.WriteLineAsync(line, ct).AsUniTask();
        }

        /// <inheritdoc/>
        public UniTask WriteAsync(string text, CancellationToken ct = default)
        {
            return inner.WriteAsync(text, ct).AsUniTask();
        }
    }
}
#endif
