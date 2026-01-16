using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// ObservableCollectionをIObservableCollectionにラップするアダプター
    /// FlyweightScrollViewControllerでObservableCollectionを使用するために使います
    /// </summary>
    /// <typeparam name="T">コレクションの要素の型</typeparam>
    public class FlyweightScrollViewDataAdapter<T> : IObservableCollection<T>, IDisposable
    {
        /// <summary>
        /// ラップ対象のObservableCollection
        /// </summary>
        private ObservableCollection<T> source;

        /// <summary>
        /// Disposeが呼ばれたかどうか
        /// </summary>
        private bool disposed;

        /// <summary>
        /// コレクションが変更されたときに発火するイベント
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// 指定したインデックスの要素を取得または設定します
        /// </summary>
        /// <param name="index">要素のインデックス</param>
        /// <returns>指定したインデックスの要素</returns>
        public T this[int index]
        {
            get => source[index];
            set => source[index] = value;
        }

        /// <summary>
        /// コレクションの要素数
        /// </summary>
        public int Count => source?.Count ?? 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="source">ラップするObservableCollection</param>
        /// <exception cref="ArgumentNullException">sourceがnullの場合</exception>
        public FlyweightScrollViewDataAdapter(ObservableCollection<T> source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            source.CollectionChanged += OnChangedCollection;
        }

        /// <summary>
        /// コレクション変更イベントを転送します
        /// </summary>
        /// <param name="sender">イベントの送信元</param>
        /// <param name="e">変更の詳細情報</param>
        private void OnChangedCollection(object sender, NotifyCollectionChangedEventArgs e)
            => CollectionChanged?.Invoke(this, e);

        /// <summary>
        /// コレクションをクリアします
        /// </summary>
        /// <param name="isNotify">変更通知を発火するかどうか</param>
        public void Clear(bool isNotify = true)
        {
            if (source == null) return;

            if (!isNotify)
                source.CollectionChanged -= OnChangedCollection;
            source.Clear();
            if (!isNotify)
                source.CollectionChanged += OnChangedCollection;
        }

        /// <summary>
        /// リソースを解放します
        /// ソースコレクションへのイベント登録を解除します
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;

            if (source != null)
            {
                source.CollectionChanged -= OnChangedCollection;
                source = null;
            }

            disposed = true;
        }

        /// <summary>
        /// コレクションを反復処理する列挙子を返します
        /// </summary>
        /// <returns>コレクションの列挙子</returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (source == null) yield break;

            for (var index = 0; index < source.Count; index++)
            {
                yield return source[index];
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}