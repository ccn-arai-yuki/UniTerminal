using System.Collections.Generic;
using System.Collections.Specialized;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// 変更通知機能を持つコレクションのインターフェース
    /// スクロールビューのデータソースとして使用します
    /// </summary>
    /// <typeparam name="T">コレクションの要素の型</typeparam>
    public interface IObservableCollection<T> : IEnumerable<T>, INotifyCollectionChanged
    {
        /// <summary>
        /// 指定したインデックスの要素を取得します
        /// </summary>
        /// <param name="index">要素のインデックス</param>
        /// <returns>指定したインデックスの要素</returns>
        T this[int index] { get; }

        /// <summary>
        /// コレクションをクリアします
        /// </summary>
        /// <param name="isNotify">変更通知を発火するかどうか</param>
        void Clear(bool isNotify = true);

        /// <summary>
        /// コレクションの要素数
        /// </summary>
        int Count { get; }
    }
}
