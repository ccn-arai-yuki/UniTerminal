using System.Collections.Generic;
using System.Threading;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// stdin入力用の非同期テキストリーダーインターフェース。
    /// 行単位の非同期列挙を提供します。
    /// </summary>
    public interface IAsyncTextReader
    {
        /// <summary>
        /// 非同期列挙として行を読み取ります。
        /// </summary>
        IAsyncEnumerable<string> ReadLinesAsync(CancellationToken ct = default);
    }
}
