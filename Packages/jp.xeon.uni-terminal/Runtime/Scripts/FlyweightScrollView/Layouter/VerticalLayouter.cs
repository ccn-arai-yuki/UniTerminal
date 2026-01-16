using UnityEngine;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// 水平方向の配置設定
    /// </summary>
    public enum HorizontalAlignment
    {
        /// <summary>左揃え</summary>
        Left,
        /// <summary>中央揃え</summary>
        Center,
        /// <summary>右揃え</summary>
        Right,
    }

    /// <summary>
    /// 垂直スクロール用のレイアウター
    /// アイテムを縦方向に並べて配置します
    /// </summary>
    public class VerticalLayouter : Layouter
    {
        private HorizontalAlignment alignment;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="container">コンテンツコンテナのRectTransform</param>
        /// <param name="param">スクロールビューのパラメータ</param>
        /// <param name="itemSize">アイテムのサイズ</param>
        /// <param name="alignment">水平方向の配置</param>
        public VerticalLayouter(RectTransform container, FlyweightScrollViewParam param, Vector2 itemSize, HorizontalAlignment alignment)
            : base(container, param, itemSize)
        {
            this.alignment = alignment;
        }

        /// <inheritdoc/>
        public override float ItemSize => itemSize.y + spacing;

        /// <summary>
        /// 水平方向の配置を設定します
        /// </summary>
        /// <param name="alignment">配置方法</param>
        public void SetAlignment(HorizontalAlignment alignment)
        {
            this.alignment = alignment;
        }

        /// <inheritdoc/>
        public override int GetTailIndex() => Mathf.CeilToInt(viewPort.rect.height / ItemSize);

        /// <inheritdoc/>
        public override float GetContentSize(int itemCount) => itemCount * ItemSize + padding.vertical;

        /// <inheritdoc/>
        public override Vector3 GetPosition(int index)
        {
            var value = alignment switch
            {
                HorizontalAlignment.Left => padding.left,
                HorizontalAlignment.Center => 0f,
                HorizontalAlignment.Right => -padding.right,
                _ => 0f
            };
            return new Vector3(value, -index * ItemSize - padding.top, 0f);
        }

        /// <inheritdoc/>
        public override void SetItemSize(FlyweightScrollViewItemBase item)
        {
            item.SetFittingItemWidth(container.rect.width - padding.horizontal);
        }

        /// <inheritdoc/>
        public override void UpdateContainerSize(int itemCount)
        {
            var size = container.sizeDelta;
            size.y = itemCount * ItemSize + padding.vertical;
            container.sizeDelta = size;
        }

        /// <inheritdoc/>
        public override int CalculateIndex(int itemCount, float scrollPosition)
        {
            var totalContentHeight = itemCount * ItemSize + padding.vertical;
            var viewPortHeight = viewPort.rect.height;
            var maxScroll = totalContentHeight - viewPortHeight;
            var contentOffset = (1f - Mathf.Clamp01(scrollPosition)) * maxScroll;
            return Mathf.FloorToInt(contentOffset / ItemSize);
        }

        /// <inheritdoc/>
        public override float GetContentOffset(int itemCount, float scrollPosition)
        {
            var totalContentHeight = itemCount * ItemSize + padding.vertical;
            var viewPortHeight = viewPort.rect.height;
            var maxScroll = Mathf.Max(0f, totalContentHeight - viewPortHeight);
            return (1f - Mathf.Clamp01(scrollPosition)) * maxScroll;
        }

        /// <inheritdoc/>
        public override float GetScrollPositionFromOffset(int itemCount, float contentOffset)
        {
            var totalContentHeight = itemCount * ItemSize + padding.vertical;
            var viewPortHeight = viewPort.rect.height;
            var maxScroll = Mathf.Max(0f, totalContentHeight - viewPortHeight);
            if (maxScroll <= 0f) return 1f;
            var t = Mathf.Clamp01(contentOffset / maxScroll);
            return 1f - t;
        }
    }
}
