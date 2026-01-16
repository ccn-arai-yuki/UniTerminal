#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Threading;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;
    using Cysharp.Threading.Tasks.Linq;

    /// <summary>
    /// UniTask版の空テキストリーダー。
    /// 何も返さないstdinとして使用します。
    /// </summary>
    public class UniTaskEmptyTextReader : IUniTaskTextReader
    {
        /// <summary>
        /// シングルトンインスタンス。
        /// </summary>
        public static readonly UniTaskEmptyTextReader Instance = new UniTaskEmptyTextReader();

        private UniTaskEmptyTextReader()
        {
        }

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<string> ReadLinesAsync(CancellationToken ct = default)
        {
            return UniTaskAsyncEnumerable.Empty<string>();
        }
    }
}
#endif
