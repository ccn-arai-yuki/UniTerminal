using UnityEngine;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// 水平方向のFlyweightスクロールビュー
    /// 左右にスクロールするリスト表示に使用します
    /// </summary>
    [ExecuteInEditMode]
    public class FlyweightHorizontalScrollView : FlyweightScrollView
    {
        /// <summary>
        /// アイテムの垂直方向の配置
        /// </summary>
        [SerializeField]
        private VerticalAlignment alignment = VerticalAlignment.Top;

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
            var isNext = position.x - prevScrollPosition.x < 0;
            prevScrollPosition = position;
            var isPositionLast = position.x <= float.Epsilon;
            controller.Update(isNext, position.x, isPositionLast);
        }

        /// <inheritdoc/>
        protected override void SetReverseMode()
        {
            if (!param.IsReverse)
            {
                content.anchorMin = Vector2.zero;
                content.anchorMax = Vector2.up;
                content.pivot = Vector2.up;
            }
            else
            {
                content.anchorMin = Vector2.right;
                content.anchorMax = Vector2.one;
                content.pivot = Vector2.one;
            }
            controller?.SetIsReverse(param.IsReverse);
        }
    }
}
