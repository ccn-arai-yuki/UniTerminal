using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NUnit.Framework;
using Xeon.Common.FlyweightScrollView.Model;

namespace Xeon.UniTerminal.Tests
{
    public class CircularBufferTests
    {
        #region Constructor Tests

        // CB-001 基本コンストラクタ
        [Test]
        public void Constructor_WithCapacity_CreatesEmptyBuffer()
        {
            var buffer = new CircularBuffer<int>(10);

            Assert.AreEqual(10, buffer.Capacity);
            Assert.AreEqual(0, buffer.Count);
            Assert.IsTrue(buffer.IsEmpty);
            Assert.IsFalse(buffer.IsFull);
            Assert.IsFalse(buffer.IsReadOnly);
        }

        // CB-002 容量1のバッファ
        [Test]
        public void Constructor_WithCapacityOne_CreatesValidBuffer()
        {
            var buffer = new CircularBuffer<int>(1);

            Assert.AreEqual(1, buffer.Capacity);
            Assert.AreEqual(0, buffer.Count);
        }

        // CB-003 容量0または負の値は例外
        [Test]
        public void Constructor_WithZeroCapacity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new CircularBuffer<int>(0));
        }

        [Test]
        public void Constructor_WithNegativeCapacity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new CircularBuffer<int>(-1));
        }

        // CB-004 fill=trueでデフォルト値で初期化
        [Test]
        public void Constructor_WithFillTrue_FillsWithDefault()
        {
            var buffer = new CircularBuffer<int>(5, fill: true);

            Assert.AreEqual(5, buffer.Capacity);
            Assert.AreEqual(5, buffer.Count);
            Assert.IsTrue(buffer.IsFull);

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(0, buffer[i]);
            }
        }

        // CB-005 初期配列付きコンストラクタ
        [Test]
        public void Constructor_WithItems_CopiesItems()
        {
            var items = new[] { 1, 2, 3 };
            var buffer = new CircularBuffer<int>(5, items);

            Assert.AreEqual(5, buffer.Capacity);
            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(3, buffer[2]);
        }

        // CB-006 初期配列が容量より大きい場合
        [Test]
        public void Constructor_WithItemsExceedingCapacity_TruncatesItems()
        {
            var items = new[] { 1, 2, 3, 4, 5 };
            var buffer = new CircularBuffer<int>(3, items);

            Assert.AreEqual(3, buffer.Capacity);
            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(3, buffer[2]);
        }

        // CB-007 nullの初期配列
        [Test]
        public void Constructor_WithNullItems_CreatesEmptyBuffer()
        {
            var buffer = new CircularBuffer<int>(5, null);

            Assert.AreEqual(5, buffer.Capacity);
            Assert.AreEqual(0, buffer.Count);
        }

        #endregion

        #region Add/PushBack Tests

        // CB-010 基本的なAdd
        [Test]
        public void Add_SingleItem_IncreasesCount()
        {
            var buffer = new CircularBuffer<int>(5);

            buffer.Add(42);

            Assert.AreEqual(1, buffer.Count);
            Assert.AreEqual(42, buffer[0]);
            Assert.IsFalse(buffer.IsEmpty);
        }

        // CB-011 複数のAdd
        [Test]
        public void Add_MultipleItems_MaintainsOrder()
        {
            var buffer = new CircularBuffer<int>(5);

            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(3, buffer[2]);
        }

        // CB-012 容量いっぱいまでAdd
        [Test]
        public void Add_UntilFull_FillsBuffer()
        {
            var buffer = new CircularBuffer<int>(3);

            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.AreEqual(3, buffer.Count);
            Assert.IsTrue(buffer.IsFull);
        }

        // CB-013 満杯時のAdd（循環動作）
        [Test]
        public void Add_WhenFull_OverwritesOldest()
        {
            var buffer = new CircularBuffer<int>(3);

            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // 1が上書きされる

            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(2, buffer[0]); // 旧1の位置に4が入り、startが進む
            Assert.AreEqual(3, buffer[1]);
            Assert.AreEqual(4, buffer[2]);
        }

        // CB-014 満杯後の連続Add
        [Test]
        public void Add_ContinuousOverwrite_MaintainsCircularBehavior()
        {
            var buffer = new CircularBuffer<int>(3);

            for (int i = 1; i <= 10; i++)
            {
                buffer.Add(i);
            }

            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(8, buffer[0]);
            Assert.AreEqual(9, buffer[1]);
            Assert.AreEqual(10, buffer[2]);
        }

        // CB-015 通知なしのAdd
        [Test]
        public void Add_WithNotifyFalse_DoesNotFireEvent()
        {
            var buffer = new CircularBuffer<int>(5);
            var eventFired = false;
            buffer.CollectionChanged += (s, e) => eventFired = true;

            buffer.Add(1, isNotify: false);

            Assert.IsFalse(eventFired);
            Assert.AreEqual(1, buffer.Count);
        }

        #endregion

        #region PushFront Tests

        // CB-020 基本的なPushFront
        [Test]
        public void PushFront_SingleItem_AddsAtFront()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);

            buffer.PushFront(0);

            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(0, buffer[0]);
            Assert.AreEqual(1, buffer[1]);
            Assert.AreEqual(2, buffer[2]);
        }

        // CB-021 空バッファへのPushFront
        [Test]
        public void PushFront_EmptyBuffer_AddsItem()
        {
            var buffer = new CircularBuffer<int>(5);

            buffer.PushFront(42);

            Assert.AreEqual(1, buffer.Count);
            Assert.AreEqual(42, buffer[0]);
        }

        // CB-022 満杯時のPushFront
        [Test]
        public void PushFront_WhenFull_OverwritesLast()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer.PushFront(0);

            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(0, buffer[0]);
            Assert.AreEqual(1, buffer[1]);
            Assert.AreEqual(2, buffer[2]); // 3が上書きされる
        }

        #endregion

        #region PopBack Tests

        // CB-030 基本的なPopBack
        [Test]
        public void PopBack_RemovesLastItem()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer.PopBack();

            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
        }

        // CB-031 空バッファでのPopBack
        [Test]
        public void PopBack_EmptyBuffer_ThrowsInvalidOperationException()
        {
            var buffer = new CircularBuffer<int>(5);

            Assert.Throws<InvalidOperationException>(() => buffer.PopBack());
        }

        // CB-032 要素を全て削除
        [Test]
        public void PopBack_AllItems_EmptiesBuffer()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);

            buffer.PopBack();
            buffer.PopBack();

            Assert.AreEqual(0, buffer.Count);
            Assert.IsTrue(buffer.IsEmpty);
        }

        #endregion

        #region PopFront Tests

        // CB-040 基本的なPopFront
        [Test]
        public void PopFront_RemovesFirstItem()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer.PopFront();

            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(3, buffer[1]);
        }

        // CB-041 空バッファでのPopFront
        [Test]
        public void PopFront_EmptyBuffer_ThrowsInvalidOperationException()
        {
            var buffer = new CircularBuffer<int>(5);

            Assert.Throws<InvalidOperationException>(() => buffer.PopFront());
        }

        #endregion

        #region Front/Back Tests

        // CB-050 基本的なFront
        [Test]
        public void Front_ReturnsFirstItem()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.AreEqual(1, buffer.Front());
        }

        // CB-051 空バッファでのFront
        [Test]
        public void Front_EmptyBuffer_ThrowsInvalidOperationException()
        {
            var buffer = new CircularBuffer<int>(5);

            Assert.Throws<InvalidOperationException>(() => buffer.Front());
        }

        // CB-052 基本的なBack
        [Test]
        public void Back_ReturnsLastItem()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.AreEqual(3, buffer.Back());
        }

        // CB-053 空バッファでのBack
        [Test]
        public void Back_EmptyBuffer_ThrowsInvalidOperationException()
        {
            var buffer = new CircularBuffer<int>(5);

            Assert.Throws<InvalidOperationException>(() => buffer.Back());
        }

        // CB-054 単一要素でのFront/Back
        [Test]
        public void FrontAndBack_SingleItem_ReturnsSameItem()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(42);

            Assert.AreEqual(42, buffer.Front());
            Assert.AreEqual(42, buffer.Back());
        }

        // CB-055 循環後のFront/Back
        [Test]
        public void FrontAndBack_AfterWrapAround_ReturnsCorrectItems()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4); // 1が上書きされる

            Assert.AreEqual(2, buffer.Front());
            Assert.AreEqual(4, buffer.Back());
        }

        #endregion

        #region Indexer Tests

        // CB-060 正常なインデックスアクセス
        [Test]
        public void Indexer_ValidIndex_ReturnsCorrectItem()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(10);
            buffer.Add(20);
            buffer.Add(30);

            Assert.AreEqual(10, buffer[0]);
            Assert.AreEqual(20, buffer[1]);
            Assert.AreEqual(30, buffer[2]);
        }

        // CB-061 範囲外インデックス
        [Test]
        public void Indexer_IndexOutOfRange_ThrowsIndexOutOfRangeException()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);

            Assert.Throws<IndexOutOfRangeException>(() => { var _ = buffer[2]; });
            Assert.Throws<IndexOutOfRangeException>(() => { var _ = buffer[10]; });
        }

        // CB-062 空バッファでのインデックスアクセス
        [Test]
        public void Indexer_EmptyBuffer_ThrowsIndexOutOfRangeException()
        {
            var buffer = new CircularBuffer<int>(5);

            Assert.Throws<IndexOutOfRangeException>(() => { var _ = buffer[0]; });
        }

        // CB-063 インデクサーでのセット
        [Test]
        public void Indexer_Set_UpdatesValue()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer[1] = 100;

            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(100, buffer[1]);
            Assert.AreEqual(3, buffer[2]);
        }

        // CB-064 循環後のインデックスアクセス
        [Test]
        public void Indexer_AfterWrapAround_ReturnsCorrectItems()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5);

            Assert.AreEqual(3, buffer[0]);
            Assert.AreEqual(4, buffer[1]);
            Assert.AreEqual(5, buffer[2]);
        }

        #endregion

        #region Clear Tests

        // CB-070 基本的なClear
        [Test]
        public void Clear_RemovesAllItems()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer.Clear();

            Assert.AreEqual(0, buffer.Count);
            Assert.IsTrue(buffer.IsEmpty);
        }

        // CB-071 空バッファのClear
        [Test]
        public void Clear_EmptyBuffer_DoesNotThrow()
        {
            var buffer = new CircularBuffer<int>(5);

            Assert.DoesNotThrow(() => buffer.Clear());
            Assert.AreEqual(0, buffer.Count);
        }

        // CB-072 Clear後の再利用
        [Test]
        public void Clear_ThenAddItems_WorksCorrectly()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Clear();

            buffer.Add(10);
            buffer.Add(20);

            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(10, buffer[0]);
            Assert.AreEqual(20, buffer[1]);
        }

        // CB-073 通知なしのClear
        [Test]
        public void Clear_WithNotifyFalse_DoesNotFireEvent()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            var eventFired = false;
            buffer.CollectionChanged += (s, e) => eventFired = true;

            buffer.Clear(isNotify: false);

            Assert.IsFalse(eventFired);
        }

        #endregion

        #region CollectionChanged Event Tests

        // CB-080 Add時のイベント
        [Test]
        public void CollectionChanged_OnAdd_FiresWithCorrectArgs()
        {
            var buffer = new CircularBuffer<int>(5);
            NotifyCollectionChangedEventArgs receivedArgs = null;
            buffer.CollectionChanged += (s, e) => receivedArgs = e;

            buffer.Add(42);

            Assert.IsNotNull(receivedArgs);
            Assert.AreEqual(NotifyCollectionChangedAction.Add, receivedArgs.Action);
            Assert.AreEqual(42, receivedArgs.NewItems[0]);
            Assert.AreEqual(0, receivedArgs.NewStartingIndex);
        }

        // CB-081 満杯時のAdd（Replace）
        [Test]
        public void CollectionChanged_OnAddWhenFull_FiresReplaceAction()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            NotifyCollectionChangedEventArgs receivedArgs = null;
            buffer.CollectionChanged += (s, e) => receivedArgs = e;

            buffer.Add(4);

            Assert.IsNotNull(receivedArgs);
            Assert.AreEqual(NotifyCollectionChangedAction.Replace, receivedArgs.Action);
            Assert.AreEqual(4, receivedArgs.NewItems[0]);
            Assert.AreEqual(1, receivedArgs.OldItems[0]);
        }

        // CB-082 PopBack時のイベント
        [Test]
        public void CollectionChanged_OnPopBack_FiresWithCorrectArgs()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);

            NotifyCollectionChangedEventArgs receivedArgs = null;
            buffer.CollectionChanged += (s, e) => receivedArgs = e;

            buffer.PopBack();

            Assert.IsNotNull(receivedArgs);
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, receivedArgs.Action);
            Assert.AreEqual(2, receivedArgs.OldItems[0]);
        }

        // CB-083 PopFront時のイベント
        [Test]
        public void CollectionChanged_OnPopFront_FiresWithCorrectArgs()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);

            NotifyCollectionChangedEventArgs receivedArgs = null;
            buffer.CollectionChanged += (s, e) => receivedArgs = e;

            buffer.PopFront();

            Assert.IsNotNull(receivedArgs);
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, receivedArgs.Action);
            Assert.AreEqual(1, receivedArgs.OldItems[0]);
            Assert.AreEqual(0, receivedArgs.OldStartingIndex);
        }

        // CB-084 Clear時のイベント
        [Test]
        public void CollectionChanged_OnClear_FiresResetAction()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);

            NotifyCollectionChangedEventArgs receivedArgs = null;
            buffer.CollectionChanged += (s, e) => receivedArgs = e;

            buffer.Clear();

            Assert.IsNotNull(receivedArgs);
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, receivedArgs.Action);
        }

        // CB-085 インデクサーSet時のイベント
        [Test]
        public void CollectionChanged_OnIndexerSet_FiresReplaceAction()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);

            NotifyCollectionChangedEventArgs receivedArgs = null;
            buffer.CollectionChanged += (s, e) => receivedArgs = e;

            buffer[1] = 100;

            Assert.IsNotNull(receivedArgs);
            Assert.AreEqual(NotifyCollectionChangedAction.Replace, receivedArgs.Action);
            Assert.AreEqual(100, receivedArgs.NewItems[0]);
            Assert.AreEqual(2, receivedArgs.OldItems[0]);
            Assert.AreEqual(1, receivedArgs.NewStartingIndex);
        }

        // CB-086 PushFront時のイベント
        [Test]
        public void CollectionChanged_OnPushFront_FiresWithCorrectArgs()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);

            NotifyCollectionChangedEventArgs receivedArgs = null;
            buffer.CollectionChanged += (s, e) => receivedArgs = e;

            buffer.PushFront(0);

            Assert.IsNotNull(receivedArgs);
            Assert.AreEqual(NotifyCollectionChangedAction.Add, receivedArgs.Action);
            Assert.AreEqual(0, receivedArgs.NewItems[0]);
            Assert.AreEqual(0, receivedArgs.NewStartingIndex);
        }

        #endregion

        #region Enumeration Tests

        // CB-090 基本的な列挙
        [Test]
        public void GetEnumerator_ReturnsItemsInOrder()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            var items = buffer.ToList();

            Assert.AreEqual(3, items.Count);
            Assert.AreEqual(1, items[0]);
            Assert.AreEqual(2, items[1]);
            Assert.AreEqual(3, items[2]);
        }

        // CB-091 空バッファの列挙
        [Test]
        public void GetEnumerator_EmptyBuffer_ReturnsNoItems()
        {
            var buffer = new CircularBuffer<int>(5);

            var items = buffer.ToList();

            Assert.AreEqual(0, items.Count);
        }

        // CB-092 循環後の列挙
        [Test]
        public void GetEnumerator_AfterWrapAround_ReturnsCorrectOrder()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5);

            var items = buffer.ToList();

            Assert.AreEqual(3, items.Count);
            Assert.AreEqual(3, items[0]);
            Assert.AreEqual(4, items[1]);
            Assert.AreEqual(5, items[2]);
        }

        // CB-093 foreachでの列挙
        [Test]
        public void Foreach_EnumeratesCorrectly()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(10);
            buffer.Add(20);
            buffer.Add(30);

            var sum = 0;
            foreach (var item in buffer)
            {
                sum += item;
            }

            Assert.AreEqual(60, sum);
        }

        // CB-094 LINQとの互換性
        [Test]
        public void Linq_WorksCorrectly()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5);

            var evenSum = buffer.Where(x => x % 2 == 0).Sum();

            Assert.AreEqual(6, evenSum); // 2 + 4
        }

        #endregion

        #region Edge Cases

        // CB-100 容量1でのラップアラウンド
        [Test]
        public void CapacityOne_WrapAround_WorksCorrectly()
        {
            var buffer = new CircularBuffer<int>(1);

            buffer.Add(1);
            Assert.AreEqual(1, buffer[0]);

            buffer.Add(2);
            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(1, buffer.Count);
        }

        // CB-101 参照型の要素
        [Test]
        public void ReferenceType_WorksCorrectly()
        {
            var buffer = new CircularBuffer<string>(3);

            buffer.Add("one");
            buffer.Add("two");
            buffer.Add("three");

            Assert.AreEqual("one", buffer[0]);
            Assert.AreEqual("two", buffer[1]);
            Assert.AreEqual("three", buffer[2]);
        }

        // CB-102 null要素の追加（参照型）
        [Test]
        public void Add_NullElement_WorksCorrectly()
        {
            var buffer = new CircularBuffer<string>(5);

            buffer.Add(null);
            buffer.Add("test");
            buffer.Add(null);

            Assert.AreEqual(3, buffer.Count);
            Assert.IsNull(buffer[0]);
            Assert.AreEqual("test", buffer[1]);
            Assert.IsNull(buffer[2]);
        }

        // CB-103 大量データの追加
        [Test]
        public void Add_LargeAmountOfData_MaintainsCorrectState()
        {
            var buffer = new CircularBuffer<int>(100);

            for (int i = 0; i < 10000; i++)
            {
                buffer.Add(i);
            }

            Assert.AreEqual(100, buffer.Count);
            Assert.AreEqual(9900, buffer[0]);
            Assert.AreEqual(9999, buffer[99]);
        }

        // CB-104 PushFrontとPushBackの混合使用
        [Test]
        public void MixedPushFrontAndBack_MaintainsCorrectOrder()
        {
            var buffer = new CircularBuffer<int>(5);

            buffer.PushBack(3);
            buffer.PushFront(2);
            buffer.PushBack(4);
            buffer.PushFront(1);
            buffer.PushBack(5);

            Assert.AreEqual(5, buffer.Count);
            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(3, buffer[2]);
            Assert.AreEqual(4, buffer[3]);
            Assert.AreEqual(5, buffer[4]);
        }

        // CB-105 PopFrontとPopBackの混合使用
        [Test]
        public void MixedPopFrontAndBack_RemovesCorrectItems()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5);

            buffer.PopFront(); // 1を削除
            buffer.PopBack();  // 5を削除
            buffer.PopFront(); // 2を削除

            Assert.AreEqual(2, buffer.Count);
            Assert.AreEqual(3, buffer[0]);
            Assert.AreEqual(4, buffer[1]);
        }

        #endregion
    }
}
