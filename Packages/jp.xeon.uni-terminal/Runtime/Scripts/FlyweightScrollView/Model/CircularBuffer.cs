using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Xeon.Common.FlyweightScrollView.Model
{
    /// <summary>
    /// 固定サイズの循環バッファ
    /// 容量を超えた場合、最も古い要素が自動的に上書きされます
    /// ログの保存などに適したデータ構造です
    /// </summary>
    /// <typeparam name="T">バッファに格納する要素の型</typeparam>
    public class CircularBuffer<T> : IObservableCollection<T>, IReadOnlyList<T>
    {
        /// <summary>
        /// バッファ本体の配列
        /// </summary>
        protected T[] buffer;

        /// <summary>
        /// 論理的な先頭を指すインデックス
        /// </summary>
        protected int start;

        /// <summary>
        /// 次に追加される位置を指すインデックス
        /// </summary>
        protected int end;

        /// <summary>
        /// コレクションが変更されたときに発火するイベント
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// バッファの最大容量
        /// </summary>
        public int Capacity => buffer.Length;

        /// <summary>
        /// バッファが満杯かどうか
        /// </summary>
        public bool IsFull => Count == Capacity;

        /// <summary>
        /// バッファが空かどうか
        /// </summary>
        public bool IsEmpty => Count == 0;

        /// <summary>
        /// 現在の要素数
        /// </summary>
        public int Count { get; protected set; }

        /// <summary>
        /// 読み取り専用かどうか。常にfalseを返します
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// 指定したインデックスの要素を取得または設定します
        /// </summary>
        /// <param name="index">論理インデックス（0から始まる）</param>
        /// <returns>指定したインデックスの要素</returns>
        /// <exception cref="IndexOutOfRangeException">インデックスが範囲外の場合</exception>
        public virtual T this[int index]
        {
            get
            {
                if (IsEmpty)
                    throw new IndexOutOfRangeException($"インデックス {index} にアクセスできません。バッファが空です。");
                if (index >= Count)
                    throw new IndexOutOfRangeException($"インデックス {index} にアクセスできません。バッファの要素数は {Count} です。");

                var actualIndex = (start + index) % Capacity;
                return buffer[actualIndex];
            }

            set
            {
                if (IsEmpty)
                    throw new IndexOutOfRangeException($"インデックス {index} にアクセスできません。バッファが空です。");
                if (index >= Count)
                    throw new IndexOutOfRangeException($"インデックス {index} にアクセスできません。バッファの要素数は {Count} です。");

                var actualIndex = (start + index) % Capacity;
                var oldItem = buffer[actualIndex];
                buffer[actualIndex] = value;

                // 要素の置換を通知
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Replace, value, oldItem, index));
            }
        }

        /// <summary>
        /// 初期データを指定してバッファを初期化します
        /// </summary>
        /// <param name="capacity">バッファの最大容量</param>
        /// <param name="items">初期データの配列。容量を超える場合は先頭から容量分のみコピーされます</param>
        /// <exception cref="ArgumentException">容量が1未満の場合</exception>
        public CircularBuffer(int capacity, T[] items)
        {
            if (capacity < 1)
                throw new ArgumentException("容量は1以上でなければなりません。", nameof(capacity));

            buffer = new T[capacity];
            if (items == null)
                return;

            var copyCount = Math.Min(items.Length, capacity);
            Array.Copy(items, buffer, copyCount);

            Count = copyCount;
            start = 0;
            end = copyCount % Capacity;
        }

        /// <summary>
        /// 指定した容量でバッファを初期化します
        /// </summary>
        /// <param name="capacity">バッファの最大容量</param>
        /// <param name="fill">trueの場合、デフォルト値で容量いっぱいまで初期化します</param>
        /// <exception cref="ArgumentException">容量が1未満の場合</exception>
        public CircularBuffer(int capacity, bool fill = false)
        {
            if (capacity < 1)
                throw new ArgumentException("容量は1以上でなければなりません。", nameof(capacity));

            buffer = new T[capacity];
            start = 0;
            end = 0;

            if (fill)
            {
                for (var i = 0; i < capacity; i++)
                    buffer[i] = default;
                Count = Capacity;
            }
            else
            {
                Count = 0;
            }
        }

        /// <summary>
        /// バッファをクリアし、すべての要素を削除します
        /// </summary>
        /// <param name="isNotify">変更通知を発火するかどうか</param>
        public virtual void Clear(bool isNotify = true)
        {
            // バッファ全体をクリア
            for (var i = 0; i < Capacity; i++)
                buffer[i] = default;

            Count = start = end = 0;

            // コレクション全体がリセットされたことを通知
            if (isNotify)
            {
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// 先頭の要素を取得します
        /// </summary>
        /// <returns>先頭の要素</returns>
        /// <exception cref="InvalidOperationException">バッファが空の場合</exception>
        public T Front()
        {
            ThrowIfEmpty();
            return buffer[start];
        }

        /// <summary>
        /// 末尾の要素を取得します
        /// </summary>
        /// <returns>末尾の要素</returns>
        /// <exception cref="InvalidOperationException">バッファが空の場合</exception>
        public T Back()
        {
            ThrowIfEmpty();
            var lastIndex = (end == 0) ? Capacity - 1 : end - 1;
            return buffer[lastIndex];
        }

        /// <summary>
        /// 末尾に要素を追加します。PushBackのエイリアスです
        /// </summary>
        /// <param name="item">追加する要素</param>
        /// <param name="isNotify">変更通知を発火するかどうか</param>
        public virtual void Add(T item, bool isNotify = true)
        {
            PushBack(item, isNotify);
        }

        /// <summary>
        /// 末尾に要素を追加します
        /// バッファが満杯の場合は先頭の要素が上書きされます
        /// </summary>
        /// <param name="item">追加する要素</param>
        /// <param name="isNotify">変更通知を発火するかどうか</param>
        public virtual void PushBack(T item, bool isNotify = true)
        {
            if (IsFull)
            {
                // 満杯のときは古い要素を上書きする
                var oldItem = buffer[end];
                buffer[end] = item;

                // end を進め、start も追従させる
                Increment(ref end);
                start = end;

                // 古い要素が置き換わったことを通知（論理インデックス0が上書きされた扱い）
                if (isNotify)
                {
                    CollectionChanged?.Invoke(this,
                        new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Replace, item, oldItem, 0));
                }
                return;
            }

            // 通常の追加処理
            buffer[end] = item;
            var logicalIndex = Count;
            Increment(ref end);
            Count++;

            // 要素が追加されたことを通知
            if (isNotify)
            {
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add, item, logicalIndex));
            }
        }

        /// <summary>
        /// 先頭に要素を追加します
        /// バッファが満杯の場合は末尾の要素が上書きされます
        /// </summary>
        /// <param name="item">追加する要素</param>
        /// <param name="isNotify">変更通知を発火するかどうか</param>
        public virtual void PushFront(T item, bool isNotify = true)
        {
            Decrement(ref start);

            if (IsFull)
            {
                // 満杯時は最後尾の要素を上書き
                var oldItem = buffer[start];
                buffer[start] = item;
                end = start;

                // 最後の要素が置き換わったことを通知
                if (isNotify)
                {
                    CollectionChanged?.Invoke(this,
                        new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Replace, item, oldItem, Count - 1));
                }
                return;
            }

            buffer[start] = item;
            Count++;

            // 要素が先頭に追加されたことを通知
            if (isNotify)
            {
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add, item, 0));
            }
        }

        /// <summary>
        /// 末尾の要素を削除します
        /// </summary>
        /// <param name="isNotify">変更通知を発火するかどうか</param>
        /// <exception cref="InvalidOperationException">バッファが空の場合</exception>
        public virtual void PopBack(bool isNotify = true)
        {
            ThrowIfEmpty("バッファが空のため、要素を削除できません。");

            var lastIndex = (end == 0) ? Capacity - 1 : end - 1;
            var removedItem = buffer[lastIndex];

            Decrement(ref end);
            buffer[end] = default;
            Count--;

            // 要素削除を通知（論理インデックス Count は削除前の最後の要素位置）
            if (isNotify)
            {
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove, removedItem, Count));
            }
        }

        /// <summary>
        /// 先頭の要素を削除します
        /// </summary>
        /// <param name="isNotify">変更通知を発火するかどうか</param>
        /// <exception cref="InvalidOperationException">バッファが空の場合</exception>
        public virtual void PopFront(bool isNotify = true)
        {
            ThrowIfEmpty("バッファが空のため、要素を削除できません。");

            var removedItem = buffer[start];
            buffer[start] = default;
            Increment(ref start);
            Count--;

            // 先頭要素削除を通知
            if (isNotify)
            {
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove, removedItem, 0));
            }
        }

        /// <summary>
        /// バッファが空の場合に例外を送出します
        /// </summary>
        /// <param name="message">例外メッセージ</param>
        /// <exception cref="InvalidOperationException">バッファが空の場合</exception>
        private void ThrowIfEmpty(string message = "バッファが空のためアクセスできません。")
        {
            if (!IsEmpty) return;
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// インデックスを1進めます
        /// 末尾に達した場合は0に戻ります（循環）
        /// </summary>
        /// <param name="index">進めるインデックスの参照</param>
        private void Increment(ref int index)
        {
            index++;
            if (index == Capacity)
                index = 0;
        }

        /// <summary>
        /// インデックスを1戻します
        /// 0の場合は末尾に戻ります（循環）
        /// </summary>
        /// <param name="index">戻すインデックスの参照</param>
        private void Decrement(ref int index)
        {
            if (index == 0)
                index = Capacity;
            index--;
        }

        /// <summary>
        /// コレクションを反復処理する列挙子を返します
        /// </summary>
        /// <returns>コレクションの列挙子</returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (var index = 0; index < Count; index++)
            {
                var actualIndex = (start + index) % Capacity;
                yield return buffer[actualIndex];
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}