#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Xeon.UniTerminal;
using Xeon.UniTerminal.UniTask;

namespace Xeon.UniTerminal.Tests
{
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// UniTaskアダプターのテスト。
    /// </summary>
    [TestFixture]
    public class UniTaskAdapterTests
    {
        #region IUniTaskTextWriter Tests

        [Test]
        public async Task UniTaskStringBuilderTextWriter_WriteLine_AppendsLine()
        {
            var writer = new UniTaskStringBuilderTextWriter();

            await writer.WriteLineAsync("Hello");
            await writer.WriteLineAsync("World");

            var result = writer.ToString();
            Assert.IsTrue(result.Contains("Hello"));
            Assert.IsTrue(result.Contains("World"));
        }

        [Test]
        public async Task UniTaskStringBuilderTextWriter_Write_AppendsWithoutNewline()
        {
            var writer = new UniTaskStringBuilderTextWriter();

            await writer.WriteAsync("Hello");
            await writer.WriteAsync(" ");
            await writer.WriteAsync("World");

            Assert.AreEqual("Hello World", writer.ToString());
        }

        [Test]
        public void UniTaskStringBuilderTextWriter_Clear_ClearsBuffer()
        {
            var writer = new UniTaskStringBuilderTextWriter();
            writer.WriteLineAsync("Test").Forget();

            writer.Clear();

            Assert.AreEqual(0, writer.Length);
        }

        [Test]
        public async Task UniTaskListTextWriter_WriteLine_CollectsLines()
        {
            var writer = new UniTaskListTextWriter();

            await writer.WriteLineAsync("Line1");
            await writer.WriteLineAsync("Line2");
            await writer.WriteLineAsync("Line3");

            Assert.AreEqual(3, writer.Count);
            Assert.AreEqual("Line1", writer.Lines[0]);
            Assert.AreEqual("Line2", writer.Lines[1]);
            Assert.AreEqual("Line3", writer.Lines[2]);
        }

        [Test]
        public async Task UniTaskListTextWriter_Flush_CompletesPartialLine()
        {
            var writer = new UniTaskListTextWriter();

            await writer.WriteAsync("Partial");
            writer.Flush();

            Assert.AreEqual(1, writer.Count);
            Assert.AreEqual("Partial", writer.Lines[0]);
        }

        #endregion

        #region IUniTaskTextReader Tests

        [Test]
        public async Task UniTaskListTextReader_ReadLinesAsync_ReturnsAllLines()
        {
            var lines = new List<string> { "Line1", "Line2", "Line3" };
            var reader = new UniTaskListTextReader(lines);

            var result = new List<string>();
            await foreach (var line in reader.ReadLinesAsync())
            {
                result.Add(line);
            }

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("Line1", result[0]);
            Assert.AreEqual("Line2", result[1]);
            Assert.AreEqual("Line3", result[2]);
        }

        [Test]
        public async Task UniTaskEmptyTextReader_ReadLinesAsync_ReturnsEmpty()
        {
            var reader = UniTaskEmptyTextReader.Instance;

            var result = new List<string>();
            await foreach (var line in reader.ReadLinesAsync())
            {
                result.Add(line);
            }

            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region Adapter Tests

        [Test]
        public async Task TaskTextWriterAdapter_WrapsTaskWriter()
        {
            var taskWriter = new StringBuilderTextWriter();
            var adapter = new TaskTextWriterAdapter(taskWriter);

            await adapter.WriteLineAsync("Test");

            Assert.IsTrue(taskWriter.ToString().Contains("Test"));
        }

        [Test]
        public async Task UniTaskTextWriterAdapter_WrapsUniTaskWriter()
        {
            var uniTaskWriter = new UniTaskStringBuilderTextWriter();
            var adapter = new UniTaskTextWriterAdapter(uniTaskWriter);

            await adapter.WriteLineAsync("Test");

            Assert.IsTrue(uniTaskWriter.ToString().Contains("Test"));
        }

        [Test]
        public async Task TaskTextReaderAdapter_WrapsTaskReader()
        {
            var lines = new List<string> { "Line1", "Line2" };
            var taskReader = new ListTextReader(lines);
            var adapter = new TaskTextReaderAdapter(taskReader);

            var result = new List<string>();
            await foreach (var line in adapter.ReadLinesAsync())
            {
                result.Add(line);
            }

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public async Task UniTaskTextReaderAdapter_WrapsUniTaskReader()
        {
            var lines = new List<string> { "Line1", "Line2" };
            var uniTaskReader = new UniTaskListTextReader(lines);
            var adapter = new UniTaskTextReaderAdapter(uniTaskReader);

            var result = new List<string>();
            await foreach (var line in adapter.ReadLinesAsync())
            {
                result.Add(line);
            }

            Assert.AreEqual(2, result.Count);
        }

        #endregion

        #region Cancellation Tests

        [Test]
        public void UniTaskStringBuilderTextWriter_ThrowsOnCancellation()
        {
            var writer = new UniTaskStringBuilderTextWriter();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await writer.WriteLineAsync("Test", cts.Token);
            });
        }

        [Test]
        public void UniTaskListTextWriter_ThrowsOnCancellation()
        {
            var writer = new UniTaskListTextWriter();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await writer.WriteLineAsync("Test", cts.Token);
            });
        }

        #endregion
    }
}
#endif
