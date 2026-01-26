#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Threading;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// UniTask版の非同期テキストリーダーインターフェース
    /// stdin入力に使用します
    /// </summary>
    public interface IUniTaskTextReader
    {
        /// <summary>
        /// 非同期列挙として行を読み取ります
        /// </summary>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>行の非同期列挙</returns>
        IUniTaskAsyncEnumerable<string> ReadLinesAsync(CancellationToken ct = default);
    }
}
#endif
