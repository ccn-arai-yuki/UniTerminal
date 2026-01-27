#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Collections.Generic;
using System.Threading;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;
    using Cysharp.Threading.Tasks.Linq;

    /// <summary>
    /// UniTask版のリストテキストリーダー
    /// 文字列のリストから行を読み取ります
    /// </summary>
    public class UniTaskListTextReader : IUniTaskTextReader
    {
        private readonly IReadOnlyList<string> lines;

        /// <summary>
        /// 読み取り元の行リスト
        /// </summary>
        public IReadOnlyList<string> Lines => lines;

        /// <summary>
        /// 文字列のリストからリーダーを作成します
        /// </summary>
        /// <param name="lines">読み取り元の行リスト</param>
        public UniTaskListTextReader(IReadOnlyList<string> lines)
        {
            this.lines = lines ?? new List<string>();
        }

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<string> ReadLinesAsync(CancellationToken ct = default)
        {
            return UniTaskAsyncEnumerable.Create<string>(async (writer, token) =>
            {
                foreach (var line in lines)
                {
                    token.ThrowIfCancellationRequested();
                    await writer.YieldAsync(line);
                }
            });
        }

        /// <summary>
        /// 行数を取得します
        /// </summary>
        public int Count => lines.Count;
    }
}
#endif
