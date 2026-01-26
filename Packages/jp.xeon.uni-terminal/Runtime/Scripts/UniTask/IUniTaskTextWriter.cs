#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Threading;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;
    /// <summary>
    /// UniTask版の非同期テキストライターインターフェース。
    /// stdout/stderr出力に使用します。
    /// </summary>
    public interface IUniTaskTextWriter
    {
        /// <summary>
        /// テキスト行を非同期で書き込みます。
        /// </summary>
        /// <param name="line">書き込む行。</param>
        /// <param name="ct">キャンセルトークン。</param>
        UniTask WriteLineAsync(string line, CancellationToken ct = default);

        /// <summary>
        /// 改行なしでテキストを非同期で書き込みます。
        /// </summary>
        /// <param name="text">書き込むテキスト。</param>
        /// <param name="ct">キャンセルトークン。</param>
        UniTask WriteAsync(string text, CancellationToken ct = default);

        void Clear();
    }
}
#endif
