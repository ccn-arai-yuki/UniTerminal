using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xeon.Common.FlyweightScrollView.Model;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// Unityログのエントリ
    /// </summary>
    public readonly struct LogEntry
    {
        public readonly string Message;
        public readonly string StackTrace;
        public readonly LogType Type;
        public readonly DateTime Timestamp;

        public LogEntry(string message, string stackTrace, LogType type)
        {
            Message = message;
            StackTrace = stackTrace;
            Type = type;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Unityログを蓄積するバッファ
    /// CircularBufferを使用して固定サイズで効率的に管理
    /// </summary>
    public class LogBuffer : IDisposable
    {
        private readonly CircularBuffer<LogEntry> entries;
        private readonly object lockObject = new object();

        /// <summary>
        /// 新しいログを受信した際に発火するイベント。
        /// </summary>
        public event Action<LogEntry> OnLogReceived;

        /// <summary>
        /// 現在のログ件数
        /// </summary>
        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return entries.Count;
                }
            }
        }

        /// <summary>
        /// バッファの最大容量
        /// </summary>
        public int Capacity => entries.Capacity;

        /// <summary>
        /// LogBufferを初期化する。
        /// </summary>
        /// <param name="capacity">バッファの最大容量。</param>
        public LogBuffer(int capacity = 10000)
        {
            entries = new CircularBuffer<LogEntry>(capacity);
            Application.logMessageReceived += HandleLogMessage;
        }

        /// <summary>
        /// リソースを解放する。
        /// </summary>
        public void Dispose()
        {
            Application.logMessageReceived -= HandleLogMessage;
        }

        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            var entry = new LogEntry(condition, stackTrace, type);

            lock (lockObject)
            {
                // CircularBufferは自動的に古いエントリを上書き
                entries.Add(entry, isNotify: false);
            }

            OnLogReceived?.Invoke(entry);
        }

        /// <summary>
        /// 蓄積されたログのコピーを取得
        /// </summary>
        public List<LogEntry> GetEntries()
        {
            lock (lockObject)
            {
                return entries.ToList();
            }
        }

        /// <summary>
        /// 蓄積されたログをクリア
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                entries.Clear(isNotify: false);
            }
        }
    }
}
