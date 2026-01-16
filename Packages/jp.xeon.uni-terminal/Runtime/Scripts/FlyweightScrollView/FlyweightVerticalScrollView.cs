using UnityEngine;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// 垂直方向のFlyweightスクロールビュー
    /// 上下にスクロールするリスト表示に使用します
    /// </summary>
    [ExecuteInEditMode]
    public class FlyweightVerticalScrollView : FlyweightScrollView
    {
        /// <summary>
        /// アイテムの水平方向の配置
        /// </summary>
        [SerializeField]
        private HorizontalAlignment alignment = HorizontalAlignment.Left;

        /// <inheritdoc/>
        public override void Setup(FlyweightScrollViewControllerBase controller)
        {
            base.Setup(controller);
            controller.Setup(scrollView, param, content, alignment);
        }

        /// <inheritdoc/>
        protected override void OnChangedScrollPosition(Vector2 position)
        {
            if (controller == null)
                return;

            var isNext = position.y - prevScrollPosition.y < 0;
            if (position.y < float.Epsilon)
                isNext = true;
            else if (position.y >= 1f)
                isNext = false;
            prevScrollPosition = position;

            var isPositionLast = position.y <= float.Epsilon;
            controller.Update(isNext, position.y, isPositionLast);
        }

        /// <inheritdoc/>
        protected override void SetReverseMode()
        {
            if (param.IsReverse)
            {
                content.anchorMin = Vector2.zero;
                content.anchorMax = Vector2.right;
                content.pivot = new Vector2(0.5f, 0f);
            }
            else
            {
                content.anchorMin = Vector2.up;
                content.anchorMax = Vector2.one;
                content.pivot = new Vector2(0.5f, 1f);
            }
            controller?.SetIsReverse(param.IsReverse);
        }
    }
}
