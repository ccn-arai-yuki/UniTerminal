using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// stdout/stderr出力用の非同期テキストライターインターフェース。
    /// </summary>
    public interface IAsyncTextWriter
    {
        /// <summary>
        /// テキスト行を非同期で書き込みます。
        /// </summary>
        Task WriteLineAsync(string line, CancellationToken ct = default);

        /// <summary>
        /// 改行なしでテキストを非同期で書き込みます。
        /// </summary>
        Task WriteAsync(string text, CancellationToken ct = default);

        void Clear();
    }
}
