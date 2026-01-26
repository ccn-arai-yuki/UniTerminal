using System.Collections.Generic;
using System.Threading;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// 行を返さない空のテキストリーダー
    /// stdinが提供されない場合に使用されます
    /// </summary>
    public class EmptyTextReader : IAsyncTextReader
    {
        /// <summary>
        /// 共有インスタンス
        /// </summary>
        public static readonly EmptyTextReader Instance = new EmptyTextReader();

        /// <summary>
        /// 空の行列を返します
        /// </summary>
        /// <param name="ct">キャンセルトークン</param>
        public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await System.Threading.Tasks.Task.CompletedTask;
            yield break;
        }
    }
}
