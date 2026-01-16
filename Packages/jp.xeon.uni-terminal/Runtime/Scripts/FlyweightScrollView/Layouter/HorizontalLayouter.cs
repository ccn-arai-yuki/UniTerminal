using UnityEngine;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// 垂直方向の配置設定
    /// </summary>
    public enum VerticalAlignment
    {
        /// <summary>上揃え</summary>
        Top,
        /// <summary>中央揃え</summary>
        Middle,
        /// <summary>下揃え</summary>
        Bottom,
    }

    /// <summary>
    /// 水平スクロール用のレイアウター
    /// アイテムを横方向に並べて配置します
    /// </summary>
    public class HorizontalLayouter : Layouter
    {
        private VerticalAlignment alignment;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="container">コンテンツコンテナのRectTransform</param>
        /// <param name="param">スクロールビューのパラメータ</param>
        /// <param name="itemSize">アイテムのサイズ</param>
        /// <param name="alignment">垂直方向の配置</param>
        public HorizontalLayouter(RectTransform container, FlyweightScrollViewParam param, Vector2 itemSize, VerticalAlignment alignment)
            : base(container, param, itemSize)
        {
            this.alignment = alignment;
        }

        /// <inheritdoc/>
        public override float ItemSize => itemSize.x + spacing;

        /// <summary>
        /// 垂直方向の配置を設定します
        /// </summary>
        /// <param name="alignment">配置方法</param>
        public void SetAlignment(VerticalAlignment alignment)
        {
            this.alignment = alignment;
        }

        /// <inheritdoc/>
        public override int GetTailIndex() => Mathf.CeilToInt(viewPort.rect.width / ItemSize);

        /// <inheritdoc/>
        public override float GetContentSize(int itemCount) => itemCount * ItemSize + padding.horizontal;

        /// <inheritdoc/>
        public override Vector3 GetPosition(int index)
        {
            var value = alignment switch
            {
                VerticalAlignment.Top => -padding.top,
                VerticalAlignment.Middle => 0,
                VerticalAlignment.Bottom => padding.bottom,
                _ => 0f
            };
            return new Vector3(index * ItemSize + padding.left, value, 0f);
        }

        /// <inheritdoc/>
        public override void SetItemSize(FlyweightScrollViewItemBase item)
        {
            item.SetFittingItemHeight(container.rect.height - padding.vertical);
        }

        /// <inheritdoc/>
        public override void UpdateContainerSize(int itemCount)
        {
            var size = container.sizeDelta;
            size.x = itemCount * ItemSize + padding.horizontal;
            container.sizeDelta = size;
        }

        /// <inheritdoc/>
        public override int CalculateIndex(int itemCount, float scrollPosition)
        {
            var totalContentWidth = itemCount * ItemSize + padding.horizontal;
            var viewPortWidth = viewPort.rect.width;
            var maxScroll = totalContentWidth - viewPortWidth;
            var contentOffset = Mathf.Clamp01(scrollPosition) * maxScroll;
            return Mathf.FloorToInt(contentOffset / ItemSize);
        }

        /// <inheritdoc/>
        public override float GetContentOffset(int itemCount, float scrollPosition)
        {
            var totalContentWidth = itemCount * ItemSize + padding.horizontal;
            var viewPortWidth = viewPort.rect.width;
            var maxScroll = Mathf.Max(0f, totalContentWidth - viewPortWidth);
            return Mathf.Clamp01(scrollPosition) * maxScroll;
        }

        /// <inheritdoc/>
        public override float GetScrollPositionFromOffset(int itemCount, float contentOffset)
        {
            var totalContentWidth = itemCount * ItemSize + padding.horizontal;
            var viewPortWidth = viewPort.rect.width;
            var maxScroll = Mathf.Max(0f, totalContentWidth - viewPortWidth);
            if (maxScroll <= 0f) return 0f;
            return Mathf.Clamp01(contentOffset / maxScroll);
        }
    }
}
