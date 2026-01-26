using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// 行をリストに収集するテキストライター
    /// パイプライン接続に便利です
    /// </summary>
    public class ListTextWriter : IAsyncTextWriter
    {
        private readonly List<string> lines = new List<string>();
        private string partial = "";

        /// <summary>
        /// 収集した行の一覧を取得します
        /// </summary>
        public IReadOnlyList<string> Lines => lines;

        /// <summary>
        /// 行を追加します
        /// </summary>
        /// <param name="line">書き込む行</param>
        /// <param name="ct">キャンセルトークン</param>
        public Task WriteLineAsync(string line, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            lines.Add(partial + line);
            partial = "";
            return Task.CompletedTask;
        }

        /// <summary>
        /// テキストを追加します
        /// </summary>
        /// <param name="text">書き込むテキスト</param>
        /// <param name="ct">キャンセルトークン</param>
        public Task WriteAsync(string text, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            partial += text;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 未完了の行を完全な行としてフラッシュします
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
        /// バッファと収集済みの行をクリアします
        /// </summary>
        public void Clear()
        {
            lines.Clear();
            partial = "";
        }
    }
}
