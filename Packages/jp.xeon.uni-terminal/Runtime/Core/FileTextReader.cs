using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// ファイルから行を読み取るテキストリーダー。
    /// </summary>
    public class FileTextReader : IAsyncTextReader
    {
        private readonly string _filePath;

        public FileTextReader(string filePath)
        {
            _filePath = filePath;
        }

        public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            using var reader = new StreamReader(_filePath, System.Text.Encoding.UTF8);
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                ct.ThrowIfCancellationRequested();
                yield return line;
            }
        }
    }
}
