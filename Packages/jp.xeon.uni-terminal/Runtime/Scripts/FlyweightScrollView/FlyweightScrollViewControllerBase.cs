using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// Flyweightスクロールビューコントローラーの基底クラス
    /// アイテムの生成、再配置、スクロール位置の管理などのコア機能を提供する
    /// </summary>
    public abstract class FlyweightScrollViewControllerBase : IDisposable
    {
        // ====================================================================================================
        // Fields & Properties
        // ====================================================================================================

        /// <summary>
        /// 表示中のアイテムを管理するリンクリスト
        /// 先頭・末尾からの追加・削除がO(1)で行えるため、スクロール時の再配置に適しています
        /// </summary>
        private LinkedList<FlyweightScrollViewItemBase> itemList = new();

        /// <summary>
        /// アイテム数が変更されたときに呼び出されるコールバック
        /// </summary>
        private Action<int> onChangedItemCount;

        /// <summary>
        /// 現在のスクロール位置（0.0〜1.0の正規化された値）
        /// </summary>
        protected float scrollPosition = 0f;

        /// <summary>
        /// 表示中のアイテムの先頭インデックス
        /// </summary>
        protected int headIndex = 0;

        /// <summary>
        /// 表示中のアイテムの末尾インデックス
        /// </summary>
        protected int tailIndex = 0;

        /// <summary>
        /// アイテムのサイズ。Setupで初期化されます
        /// </summary>
        protected Vector2 itemSize;

        /// <summary>
        /// スクロールビューコンポーネント
        /// </summary>
        protected ScrollRect scrollView;

        /// <summary>
        /// スクロールビューのパラメータ設定
        /// </summary>
        protected FlyweightScrollViewParam param;

        /// <summary>
        /// ビューポートのRectTransform
        /// </summary>
        protected RectTransform viewPort => param.ViewPort;

        /// <summary>
        /// コンテンツコンテナのRectTransform
        /// </summary>
        protected RectTransform container;

        /// <summary>
        /// コンテンツのパディング
        /// </summary>
        protected RectOffset padding => param.Padding;

        /// <summary>
        /// アイテム間のスペース
        /// </summary>
        protected float spacing => param.Spacing;

        /// <summary>
        /// 子アイテムのサイズを自動制御するかどうか
        /// </summary>
        protected bool isControlChildSize => param.IsControlChildSize;

        /// <summary>
        /// 逆順表示モードかどうか
        /// </summary>
        protected bool isReverse => param.IsReverse;

        /// <summary>
        /// 末尾固定モードかどうか
        /// </summary>
        protected bool isAtLastSticky => param.IsAtLastSticky;

        /// <summary>
        /// 垂直方向の配置設定
        /// </summary>
        protected VerticalAlignment verticalAlignment;

        /// <summary>
        /// 水平方向の配置設定
        /// </summary>
        protected HorizontalAlignment horizontalAlignment;

        /// <summary>
        /// スクロール位置が末尾かどうか
        /// </summary>
        protected bool isPositionLast = false;

        /// <summary>
        /// アイテム数変更処理中かどうか
        /// </summary>
        protected bool isItemCountChanging = false;

        /// <summary>
        /// レイアウト計算を行うレイアウター
        /// </summary>
        protected Layouter layouter;

        /// <summary>
        /// データソースの総アイテム数
        /// </summary>
        public abstract int ItemCount { get; }

        /// <summary>
        /// 再描画が必要かどうかのフラグ
        /// </summary>
        public bool IsDirty { get; set; }


        // ====================================================================================================
        // Constructor
        // ====================================================================================================

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public FlyweightScrollViewControllerBase() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="onChangedItemCount">アイテム数が変更されたときに呼び出されるコールバック</param>
        public FlyweightScrollViewControllerBase(Action<int> onChangedItemCount = null)
        {
            this.onChangedItemCount = onChangedItemCount;
        }


        // ====================================================================================================
        // Public Methods (API)
        // ====================================================================================================

        /// <summary>
        /// 垂直スクロール用にスクロールビューを初期化します
        /// </summary>
        /// <param name="scrollView">ScrollRectコンポーネント</param>
        /// <param name="param">スクロールビューのパラメータ</param>
        /// <param name="container">コンテンツコンテナのRectTransform</param>
        /// <param name="alignment">水平方向の配置設定</param>
        public virtual void Setup(ScrollRect scrollView, FlyweightScrollViewParam param, RectTransform container, HorizontalAlignment alignment)
        {
            this.scrollView = scrollView;
            this.container = container;
            this.param = param;
            horizontalAlignment = alignment;
            layouter = new VerticalLayouter(container, param, itemSize, alignment);

            UpdateViewportSize();
        }

        /// <summary>
        /// 水平スクロール用にスクロールビューを初期化します
        /// </summary>
        /// <param name="scrollView">ScrollRectコンポーネント</param>
        /// <param name="param">スクロールビューのパラメータ</param>
        /// <param name="container">コンテンツコンテナのRectTransform</param>
        /// <param name="alignment">垂直方向の配置設定</param>
        public void Setup(ScrollRect scrollView, FlyweightScrollViewParam param, RectTransform container, VerticalAlignment alignment)
        {
            this.scrollView = scrollView;
            this.container = container;
            this.param = param;
            verticalAlignment = alignment;
            layouter = new HorizontalLayouter(container, param, itemSize, alignment);

            UpdateViewportSize();
        }

        /// <summary>
        /// ビューポートサイズが変更された際の更新処理
        /// 必要なアイテム数を再計算し、アイテムを再生成します
        /// </summary>
        public void UpdateViewportSize()
        {
            tailIndex = layouter.GetTailIndex();
            tailIndex += headIndex;
            CreateItems();
            IsDirty = true;
        }

        /// <summary>
        /// スクロール位置に応じてビューを更新します
        /// </summary>
        /// <param name="isNext">次方向（下/右）にスクロールしているかどうか</param>
        /// <param name="normalizedPosition">正規化されたスクロール位置（0.0〜1.0）</param>
        /// <param name="isPositionLast">スクロール位置が末尾かどうか</param>
        public void Update(bool isNext, float normalizedPosition, bool isPositionLast)
        {
            scrollPosition = normalizedPosition;
            if (!isItemCountChanging)
            {
                this.isPositionLast = isPositionLast;
            }
            isItemCountChanging = false;
            UpdateInternal();

            if (isNext)
                RepositionForNext();
            else
                RepositionForPrev();
        }

        /// <summary>
        /// 現在のインデックスに基づいてビュー全体を再描画します
        /// </summary>
        public void UpdateView()
        {
            if (ItemCount < itemList.Count)
            {
                UpdateNotEnoughData();
                return;
            }
            (headIndex, tailIndex) = CalculateIndex();
            var index = headIndex;
            foreach (var item in itemList)
            {
                item.SetPosition(CreatePosition(index));
                OnChangedItemIndex(index, item);
                item.UpdateIsInside();
                item.gameObject.SetActive(true);
                index++;
            }
        }

        /// <summary>
        /// スクロール位置を先頭に固定します
        /// </summary>
        public virtual void FixToHead()
        {
            headIndex = 0;
            tailIndex = itemList.Count - 1;
            IsDirty = true;
        }

        /// <summary>
        /// スクロール位置を末尾に固定します
        /// </summary>
        /// <param name="setDirty">IsDirtyフラグを設定するかどうか</param>
        public virtual void FixToLast(bool setDirty = true)
        {
            tailIndex = ItemCount;
            headIndex = Mathf.Max(0, tailIndex - itemList.Count);
            if (setDirty)
                IsDirty = true;
        }

        /// <summary>
        /// アイテム数変更時のコールバックを設定します
        /// </summary>
        /// <param name="onChangedItemCount">アイテム数が変更されたときに呼び出されるコールバック</param>
        public void SetOnChangedItemCount(Action<int> onChangedItemCount)
            => this.onChangedItemCount = onChangedItemCount;

        /// <summary>
        /// アイテム間のスペースを設定し、ビューを更新します
        /// </summary>
        /// <param name="spacing">アイテム間のスペース（ピクセル）</param>
        public void SetSpacing(float spacing)
        {
            layouter.Spacing = spacing;
            UpdateContainerSize();
            CreateItems();
            IsDirty = true;
        }

        /// <summary>
        /// 水平方向の配置を設定します
        /// </summary>
        /// <param name="horizontalAlignment">配置方法</param>
        public void SetHorizontalAlignment(HorizontalAlignment horizontalAlignment)
        {
            this.horizontalAlignment = horizontalAlignment;
            if (layouter is VerticalLayouter verticalLayouter)
                verticalLayouter.SetAlignment(horizontalAlignment);
            foreach (var item in itemList)
                item.SetHorizontalAlignment(horizontalAlignment);
        }

        /// <summary>
        /// 垂直方向の配置を設定します
        /// </summary>
        /// <param name="verticalAlignment">配置方法</param>
        public void SetVerticalAlignment(VerticalAlignment verticalAlignment)
        {
            this.verticalAlignment = verticalAlignment;
            if (layouter is HorizontalLayouter horizontalLayouter)
                horizontalLayouter.SetAlignment(verticalAlignment);
            foreach(var item in itemList)
                item.SetVerticalAlignment(verticalAlignment);
        }

        /// <summary>
        /// 逆順表示モードを設定します
        /// </summary>
        /// <param name="isReverse">逆順表示を有効にするかどうか</param>
        public void SetIsReverse(bool isReverse)
        {
            param.IsReverse = isReverse;
            IsDirty = true;
        }

        /// <summary>
        /// 末尾位置フラグを設定します
        /// </summary>
        /// <param name="flag">末尾に固定するかどうか</param>
        public void SetIsPositionLast(bool flag)
        {
            isPositionLast = flag;
            if (isPositionLast)
                FixToLast();
        }

        /// <summary>
        /// リソースを解放します
        /// 管理しているすべてのアイテムオブジェクトを破棄します
        /// </summary>
        public virtual void Dispose()
        {
            if (itemList == null) return;
            foreach (var item in itemList)
            {
                if (Application.isPlaying)
                    GameObject.Destroy(item.gameObject);
                else
                    GameObject.DestroyImmediate(item.gameObject);
            }
            itemList.Clear();
        }

        /// <summary>
        /// ビューポートに収まるアイテムサイズを取得します
        /// </summary>
        /// <returns>表示に適したアイテムサイズ</returns>
        public Vector2 GetFitItemSize() => layouter.GetFitItemSize();


        // ====================================================================================================
        // Protected Methods (For Derived Classes & Core Logic)
        // ====================================================================================================

        /// <summary>
        /// 下方向にスクロールした際のアイテム再配置処理
        /// </summary>
        protected void RepositionForNext()
        {
            var item = itemList.First;
            var (newHeadIndex, newTailIndex) = CalculateIndex();
            if (Mathf.Abs(newHeadIndex - headIndex) >= itemList.Count)
            {
                headIndex = newHeadIndex;
                tailIndex = newTailIndex;
                IsDirty = true;
                return;
            }

            while (!item.Value.IsInside)
            {
                if (tailIndex >= ItemCount - 1)
                    break;
                headIndex++;
                tailIndex++;
                OnChangedItemIndex(tailIndex, item.Value);
                item.Value.SetPosition(CreatePosition(tailIndex));
                var tmp = item.Value;
                item = item.Next;
                itemList.RemoveFirst();
                itemList.AddLast(tmp);
            }
        }

        /// <summary>
        /// 上方向にスクロールした際のアイテム再配置処理
        /// </summary>
        protected void RepositionForPrev()
        {
            var item = itemList.Last;
            var (newHeadIndex, newTailIndex) = CalculateIndex();
            if (Mathf.Abs(newHeadIndex - headIndex) >= itemList.Count)
            {
                headIndex = newHeadIndex;
                tailIndex = newTailIndex;
                IsDirty = true;
                return;
            }

            while (!item.Value.IsInside)
            {
                if (headIndex <= 0)
                    break;
                headIndex--;
                tailIndex--;
                item.Value.SetPosition(CreatePosition(headIndex));
                OnChangedItemIndex(headIndex, item.Value);
                var tmp = item.Value;
                item = item.Previous;
                itemList.RemoveLast();
                itemList.AddFirst(tmp);
            }
        }

        /// <summary>
        /// 総アイテム数が表示可能数に満たない場合の表示更新処理
        /// </summary>
        protected void UpdateNotEnoughData()
        {
            if (ItemCount > itemList.Count)
                return;

            foreach (var (item, index) in itemList.Select((item, index) => (item, index)))
            {
                if (index >= ItemCount)
                {
                    item.gameObject.SetActive(false);
                    continue;
                }
                var dataIndex = headIndex + index;
                item.gameObject.SetActive(true);
                item.SetPosition(CreatePosition(dataIndex));
                OnChangedItemIndex(dataIndex, item);
            }
        }

        /// <summary>
        /// アイテムが追加された際の更新処理
        /// コンテナサイズを更新し、必要に応じて末尾にスクロールします
        /// </summary>
        private void UpdateForAddItem()
        {
            UpdateContainerSize();
            if (isAtLastSticky && isPositionLast)
            {
                scrollView.normalizedPosition = Vector2.zero;
                FixToLast(setDirty: false);
                return;
            }
            if (tailIndex >= ItemCount - 1)
                IsDirty = true;
        }

        /// <summary>
        /// アイテムが削除された際の更新処理
        /// コンテナサイズを更新し、表示範囲を調整します
        /// </summary>
        private void UpdateForRemove()
        {
            // アイテム削除時はサイズ更新
            UpdateContainerSize();
            // 表示範囲より後ろが消えた場合は再描画不要
            // 表示中の範囲に影響がある場合のみ再描画
            if (headIndex >= ItemCount)
            {
                headIndex = Mathf.Max(0, ItemCount - itemList.Count);
                tailIndex = headIndex + itemList.Count - 1;
            }

            IsDirty = true;
        }

        /// <summary>
        /// データソースのアイテム数が変更されたときに呼び出されるイベントハンドラ
        /// 不要な再計算を避け、差分のみを更新します
        /// </summary>
        /// <param name="sender">イベントの送信元</param>
        /// <param name="e">変更の詳細情報</param>
        protected void OnChangedItemCount(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    isItemCountChanging = true;
                    UpdateForAddItem();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    isItemCountChanging = true;
                    UpdateForRemove();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    // 全リセット時は完全再構築
                    isItemCountChanging = true;
                    UpdateContainerSize();
                    IsDirty = true;
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                default:
                    // 要素数に変化がない場合は再描画だけ
                    IsDirty = true;
                    break;
            }

            // 逆順モードのときのみ再計算を強制
            if (isReverse)
                IsDirty = true;

            // ItemCount変更通知
            onChangedItemCount?.Invoke(ItemCount);
        }


        /// <summary>
        /// スクロールコンテンツ全体のサイズを更新します
        /// </summary>
        public void UpdateContainerSize()
        {
            layouter.UpdateContainerSize(ItemCount);
        }

        /// <summary>
        /// スクロール位置から、表示すべきアイテムの先頭と末尾のインデックスを計算します
        /// </summary>
        protected (int headIndex, int tailIndex) CalculateIndex()
        {
            var desiredHead = layouter.CalculateIndex(ItemCount, scrollPosition);

            var maxHead = Mathf.Max(0, ItemCount - itemList.Count);
            desiredHead = Mathf.Clamp(desiredHead, 0, maxHead);
            return (desiredHead, desiredHead + itemList.Count - 1);
        }

        /// <summary>
        /// 指定されたインデックスのアイテムが配置されるべきローカル座標を計算します
        /// </summary>
        protected Vector3 CreatePosition(int index)
        {
            return layouter.GetPosition(index);
        }


        // ====================================================================================================
        // Private Methods (Implementation Details)
        // ====================================================================================================

        /// <summary>
        /// 内部的な更新処理
        /// </summary>
        private void UpdateInternal()
        {
            UpdateNotEnoughData();
            foreach (var item in itemList)
                item.UpdateIsInside();
        }

        /// <summary>
        /// 表示に必要なアイテムオブジェクトを生成または破棄します
        /// </summary>
        private void CreateItems()
        {
            var itemCount = layouter.GetItemCount();

            if (itemList.Count == itemCount)
                return;

            foreach (var item in itemList)
            {
                if (Application.isPlaying)
                    GameObject.Destroy(item.gameObject);
                else
                    GameObject.DestroyImmediate(item.gameObject);
            }
            itemList.Clear();

            for (var index = 0; index < itemCount; index++)
            {
                var item = CreateItem(headIndex + index);
                if (isControlChildSize)
                {
                    layouter.SetItemSize(item);
                }
                item.gameObject.SetActive(false);
                itemList.AddLast(item);
            }
        }


        // ====================================================================================================
        // Abstract Methods (Must be implemented by Derived Classes)
        // ====================================================================================================

        /// <summary>
        /// 指定したインデックスに対応するアイテムのインスタンスを生成します
        /// 派生クラスで実装し、具体的なアイテムオブジェクトを生成します
        /// </summary>
        /// <param name="index">アイテムのインデックス</param>
        /// <returns>生成されたアイテム</returns>
        protected abstract FlyweightScrollViewItemBase CreateItem(int index);

        /// <summary>
        /// アイテムの表示内容を、指定したインデックスのデータで更新します
        /// 派生クラスで実装し、データのバインディングを行います
        /// </summary>
        /// <param name="index">データのインデックス</param>
        /// <param name="target">更新対象のアイテム</param>
        protected abstract void OnChangedItemIndex(int index, FlyweightScrollViewItemBase target);
    }
}
