using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// ファイルに書き込むテキストライター。
    /// </summary>
    public class FileTextWriter : IAsyncTextWriter, System.IDisposable
    {
        private readonly StreamWriter writer;

        /// <summary>
        /// ファイルライターを初期化します。
        /// </summary>
        /// <param name="filePath">出力先ファイルパス。</param>
        /// <param name="append">追記する場合はtrue。</param>
        public FileTextWriter(string filePath, bool append = false)
        {
            var stream = new FileStream(
                filePath,
                append ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.Read);
            writer = new StreamWriter(stream, Encoding.UTF8);
        }

        /// <summary>
        /// 行を書き込みます。
        /// </summary>
        /// <param name="line">書き込む行。</param>
        /// <param name="ct">キャンセルトークン。</param>
        public async Task WriteLineAsync(string line, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(line);
            await writer.FlushAsync();
        }

        /// <summary>
        /// テキストを書き込みます。
        /// </summary>
        /// <param name="text">書き込むテキスト。</param>
        /// <param name="ct">キャンセルトークン。</param>
        public async Task WriteAsync(string text, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteAsync(text);
            await writer.FlushAsync();
        }

        /// <summary>
        /// ファイルライターではクリアをサポートしません。
        /// </summary>
        public void Clear()
        {
            // ファイル出力ではクリアはサポートしない
            throw new System.NotSupportedException("Clear operation is not supported for FileTextWriter.");
        }

        /// <summary>
        /// ライターを破棄します。
        /// </summary>
        public void Dispose()
        {
            writer?.Dispose();
        }
    }
}
