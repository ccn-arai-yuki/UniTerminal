using System.Threading;
using Xeon.Common.FlyweightScrollView.Model;
using Xeon.UniTerminal.UniTask;

namespace Xeon.UniTerminal
{
    public class OutputWriter : IUniTaskTextWriter
    {
        private CircularBuffer<OutputData> buffer;
        private bool isError = false;

        public OutputWriter(CircularBuffer<OutputData> buffer, bool isError = false)
        {
            this.buffer = buffer;
            this.isError = isError;
        }
        public async Cysharp.Threading.Tasks.UniTask WriteLineAsync(string line, CancellationToken ct = default)
        {
            var lines = line.Split("\n");
            foreach (var l in lines)
                buffer.PushFront(new OutputData(l, isError));
            await Cysharp.Threading.Tasks.UniTask.CompletedTask;
        }

        public async Cysharp.Threading.Tasks.UniTask WriteAsync(string text, CancellationToken ct = default)
        {
            buffer.PushFront(new OutputData(text, isError));
            await Cysharp.Threading.Tasks.UniTask.CompletedTask;
        }
    }
}
