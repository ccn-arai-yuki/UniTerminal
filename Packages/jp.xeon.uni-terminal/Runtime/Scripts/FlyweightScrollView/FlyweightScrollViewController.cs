using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// Flyweightスクロールビューコントローラーのジェネリック実装クラス
    /// データリストとUIアイテムのバインディングを管理します
    /// </summary>
    /// <typeparam name="TData">リストに表示するデータの型</typeparam>
    /// <typeparam name="TItem">表示に使用するUIアイテムのコンポーネントの型。IBindable&lt;TData&gt;を実装する必要があります</typeparam>
    public class FlyweightScrollViewController<TData, TItem> : FlyweightScrollViewControllerBase
        where TItem : MonoBehaviour, IBindable<TData>
    {
        // ====================================================================================================
        // Fields & Properties
        // ====================================================================================================

        /// <summary>
        /// 表示するデータのコレクション
        /// </summary>
        private IObservableCollection<TData> dataList;

        /// <summary>
        /// アイテムのプレハブ
        /// </summary>
        private readonly TItem prefab;

        private FlyweightScrollItem<TItem> sample;

        /// <summary>
        /// アイテムが生成されたときに発火するイベント
        /// </summary>
        private event Action<TItem> onItemCreated;

        /// <summary>
        /// アイテムが生成されたときに発火するイベント
        /// 重複登録を防ぐため、追加前に一度削除します
        /// </summary>
        public event Action<TItem> OnItemCreated
        {
            add
            {
                onItemCreated -= value;
                onItemCreated += value;
            }
            remove => onItemCreated -= value;
        }

        /// <inheritdoc/>
        public override int ItemCount => dataList == null ? 0 : dataList.Count;


        // ====================================================================================================
        // Constructor
        // ====================================================================================================

        /// <summary>
        /// ObservableCollectionを使用するコンストラクタ
        /// 内部でFlyweightScrollViewDataAdapterにラップされます
        /// </summary>
        /// <param name="prefab">アイテムのプレハブ</param>
        /// <param name="dataList">表示するデータのObservableCollection</param>
        /// <param name="onCreatedItem">アイテム生成時のコールバック</param>
        public FlyweightScrollViewController(TItem prefab, ObservableCollection<TData> dataList, Action<TItem> onCreatedItem) : this(prefab, new FlyweightScrollViewDataAdapter<TData>(dataList), onCreatedItem)
        {
        }

        /// <summary>
        /// IObservableCollectionを使用するコンストラクタ
        /// </summary>
        /// <param name="prefab">アイテムのプレハブ</param>
        /// <param name="dataList">表示するデータのコレクション</param>
        /// <param name="onItemCreated">アイテム生成時のコールバック</param>
        public FlyweightScrollViewController(TItem prefab, IObservableCollection<TData> dataList, Action<TItem> onItemCreated = null)
        {
            this.prefab = prefab;
            this.itemSize = prefab.GetComponent<RectTransform>().rect.size;
            this.onItemCreated += onItemCreated;
            this.dataList = dataList;
            this.dataList.CollectionChanged += OnChangedItemCount;
        }

        // ====================================================================================================
        // Public Methods
        // ====================================================================================================

        public override void Setup(ScrollRect scrollView, FlyweightScrollViewParam param, RectTransform container, HorizontalAlignment alignment)
        {
            base.Setup(scrollView, param, container, alignment);
            var sampleObject = GameObject.Instantiate(prefab, container);
            sample = new FlyweightScrollItem<TItem>(sampleObject, viewPort, horizontalAlignment);
            sample.gameObject.SetActive(false);
            layouter.SetItemSize(sample);
        }

        /// <summary>
        /// 表示するデータリストを差し替えます
        /// 既存のデータリストのイベント登録を解除し、新しいデータリストに登録します
        /// </summary>
        /// <param name="newDataList">新しいデータコレクション</param>
        public void SetDataList(IObservableCollection<TData> newDataList)
        {
            if (dataList != null)
            {
                dataList.CollectionChanged -= OnChangedItemCount;
            }

            dataList = newDataList;
            dataList.CollectionChanged += OnChangedItemCount;

            // データリスト全体が置き換わったことを通知
            OnChangedItemCount(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            UpdateContainerSize();
            if (dataList != null)
            {
                dataList.CollectionChanged -= OnChangedItemCount;
                dataList = null;
            }
            if (sample != null)
            {
                GameObject.Destroy(sample.gameObject);
                sample = null;
            }

            base.Dispose();
        }

        public TItem GetSample() => sample.Value;

        // ====================================================================================================
        // Protected Overrides (Base Class Implementation)
        // ====================================================================================================

        /// <summary>
        /// プレハブからアイテムのインスタンスを生成します
        /// </summary>
        /// <param name="index">アイテムのインデックス</param>
        /// <returns>生成されたFlyweightScrollItem</returns>
        protected override FlyweightScrollViewItemBase CreateItem(int index)
        {
            var instance = GameObject.Instantiate(prefab, container);
            instance.name = $"Item({index})";

            var scrollItem = new FlyweightScrollItem<TItem>(instance, viewPort, horizontalAlignment);
            scrollItem.SetPosition(CreatePosition(index));
            onItemCreated?.Invoke(instance);

            return scrollItem;
        }

        /// <summary>
        /// アイテムの表示を、指定したインデックスのデータで更新します
        /// 逆順モードの場合はインデックスを反転させてデータを取得します
        /// </summary>
        /// <param name="index">データのインデックス</param>
        /// <param name="target">更新対象のアイテム</param>
        protected override void OnChangedItemIndex(int index, FlyweightScrollViewItemBase target)
        {
            if (target is not FlyweightScrollItem<TItem> item) return;
            if (index < 0 || index >= dataList.Count) return;
            if (isReverse)
                index = dataList.Count - index - 1;

            var data = dataList[index];
            item.Value.Bind(data);
        }
    }
}