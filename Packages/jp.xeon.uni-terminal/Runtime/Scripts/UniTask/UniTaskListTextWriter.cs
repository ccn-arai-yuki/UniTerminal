#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Collections.Generic;
using System.Threading;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;
    
    /// <summary>
    /// UniTask版のリストテキストライター。
    /// 行をリストに収集し、パイプライン接続に使用します。
    /// </summary>
    public class UniTaskListTextWriter : IUniTaskTextWriter
    {
        private readonly List<string> lines = new List<string>();
        private string partial = "";

        /// <summary>
        /// 収集された行のリスト。
        /// </summary>
        public IReadOnlyList<string> Lines => lines;

        /// <inheritdoc/>
        public UniTask WriteLineAsync(string line, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            lines.Add(partial + line);
            partial = "";
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask WriteAsync(string text, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            partial += text;
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 未完了の行を完全な行としてフラッシュします。
        /// </summary>
        public void Flush()
        {
            if (!string.IsNullOrEmpty(partial))
            {
                lines.Add(partial);
                partial = "";
            }
        }

        /// <summary>
        /// バッファをクリアします。
        /// </summary>
        public void Clear()
        {
            lines.Clear();
            partial = "";
        }

        /// <summary>
        /// 収集された行数を取得します。
        /// </summary>
        public int Count => lines.Count;
    }
}
#endif
