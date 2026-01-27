using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Xeon.UniTerminal.Tests
{
    /// <summary>
    /// LogBufferのテスト。
    /// </summary>
    public class LogBufferTests
    {
        private LogBuffer logBuffer;

        [SetUp]
        public void SetUp()
        {
            logBuffer = new LogBuffer(100);
        }

        [TearDown]
        public void TearDown()
        {
            logBuffer?.Dispose();
        }

        #region 基本動作テスト

        // LOGBUF-001 ログ追加
        [Test]
        public async Task LogBuffer_AddLog_StoresEntry()
        {
            Debug.Log("Test Message");
            await Task.Yield();

            var entries = logBuffer.GetEntries();

            Assert.IsTrue(entries.Count > 0);
            Assert.IsTrue(entries.Exists(e => e.Message.Contains("Test Message")));
        }

        // LOGBUF-002 ログタイプの保持
        [Test]
        public async Task LogBuffer_LogTypes_PreservesType()
        {
            LogAssert.Expect(LogType.Warning, "Warning");
            LogAssert.Expect(LogType.Error, "Error");

            Debug.Log("Info");
            Debug.LogWarning("Warning");
            Debug.LogError("Error");
            await Task.Yield();

            var entries = logBuffer.GetEntries();

            Assert.IsTrue(entries.Exists(e => e.Type == LogType.Log));
            Assert.IsTrue(entries.Exists(e => e.Type == LogType.Warning));
            Assert.IsTrue(entries.Exists(e => e.Type == LogType.Error));
        }

        // LOGBUF-003 カウント
        [Test]
        public async Task LogBuffer_Count_ReturnsCorrectCount()
        {
            var initialCount = logBuffer.Count;

            Debug.Log("Message 1");
            Debug.Log("Message 2");
            await Task.Yield();

            Assert.AreEqual(initialCount + 2, logBuffer.Count);
        }

        // LOGBUF-004 容量
        [Test]
        public void LogBuffer_Capacity_ReturnsSpecifiedCapacity()
        {
            Assert.AreEqual(100, logBuffer.Capacity);
        }

        #endregion

        #region CircularBuffer動作テスト

        // LOGBUF-010 容量超過時の古いエントリ削除
        [Test]
        public async Task LogBuffer_ExceedsCapacity_RemovesOldEntries()
        {
            // 小さい容量のバッファを作成
            logBuffer.Dispose();
            logBuffer = new LogBuffer(5);

            for (int i = 0; i < 10; i++)
            {
                Debug.Log($"Message {i}");
            }
            await Task.Yield();

            var entries = logBuffer.GetEntries();

            Assert.LessOrEqual(entries.Count, 5);
            // 最初のメッセージは削除されている
            Assert.IsFalse(entries.Exists(e => e.Message.Contains("Message 0")));
        }

        #endregion

        #region クリアテスト

        // LOGBUF-020 クリア
        [Test]
        public async Task LogBuffer_Clear_RemovesAllEntries()
        {
            Debug.Log("Message");
            await Task.Yield();

            logBuffer.Clear();

            Assert.AreEqual(0, logBuffer.Count);
            Assert.IsEmpty(logBuffer.GetEntries());
        }

        #endregion

        #region イベントテスト

        // LOGBUF-030 OnLogReceivedイベント
        [Test]
        public async Task LogBuffer_OnLogReceived_FiresOnNewLog()
        {
            var received = false;
            LogEntry? receivedEntry = null;

            logBuffer.OnLogReceived += entry =>
            {
                received = true;
                receivedEntry = entry;
            };

            Debug.Log("Event Test");
            await Task.Yield();

            Assert.IsTrue(received);
            Assert.IsNotNull(receivedEntry);
            Assert.IsTrue(receivedEntry.Value.Message.Contains("Event Test"));
        }

        #endregion

        #region スレッドセーフティテスト

        // LOGBUF-040 GetEntriesはコピーを返す
        [Test]
        public async Task LogBuffer_GetEntries_ReturnsCopy()
        {
            Debug.Log("Initial Message");
            await Task.Yield();

            var entries1 = logBuffer.GetEntries();
            Debug.Log("Additional Message");
            await Task.Yield();

            var entries2 = logBuffer.GetEntries();

            // entries1は変更されない
            Assert.AreNotEqual(entries1.Count, entries2.Count);
        }

        #endregion
    }
}
