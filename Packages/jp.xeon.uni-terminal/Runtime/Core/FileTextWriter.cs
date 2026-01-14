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
        private readonly StreamWriter _writer;

        public FileTextWriter(string filePath, bool append = false)
        {
            var stream = new FileStream(
                filePath,
                append ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.Read);
            _writer = new StreamWriter(stream, Encoding.UTF8);
        }

        public async Task WriteLineAsync(string line, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await _writer.WriteLineAsync(line);
            await _writer.FlushAsync();
        }

        public async Task WriteAsync(string text, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await _writer.WriteAsync(text);
            await _writer.FlushAsync();
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}
