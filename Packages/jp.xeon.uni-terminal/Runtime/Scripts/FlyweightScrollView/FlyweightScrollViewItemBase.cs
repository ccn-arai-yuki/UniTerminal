using System.ComponentModel;
using UnityEngine;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// Flyweightスクロールビューで使用するアイテムの基底クラス
    /// UIアイテムのラッパーとして、位置管理と表示判定を行います
    /// </summary>
    public abstract class FlyweightScrollViewItemBase
    {
        /// <summary>
        /// アイテムのGameObject
        /// </summary>
        public GameObject gameObject { get; protected set; }

        /// <summary>
        /// アイテムのRectTransform
        /// </summary>
        public RectTransform RectTransform { get; protected set; }

        /// <summary>
        /// アイテムがビューポート内に表示されているかどうか
        /// </summary>
        public bool IsInside { get; private set; }

        private readonly RectTransform viewPort;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="viewPort">ビューポートのRectTransform。表示判定に使用します</param>
        public FlyweightScrollViewItemBase(RectTransform viewPort)
        {
            this.viewPort = viewPort;
        }

        /// <summary>
        /// 水平方向の配置を設定します
        /// </summary>
        /// <param name="alignment">配置方法</param>
        public virtual void SetHorizontalAlignment(HorizontalAlignment alignment)
        {
            var vector = alignment switch
            {
                HorizontalAlignment.Left => Vector2.up,
                HorizontalAlignment.Center => new Vector2(0.5f, 1f),
                HorizontalAlignment.Right => Vector2.one,
                _ => throw new InvalidEnumArgumentException()
            };
            RectTransform.anchorMin = vector;
            RectTransform.anchorMax = vector;
            RectTransform.pivot = vector;
            var position = RectTransform.anchoredPosition3D;
            position.x = 0f;
            RectTransform.anchoredPosition3D = position;
        }

        /// <summary>
        /// 垂直方向の配置を設定します
        /// </summary>
        /// <param name="alignment">配置方法</param>
        public virtual void SetVerticalAlignment(VerticalAlignment alignment)
        {
            var vector = alignment switch
            {
                VerticalAlignment.Top => new Vector2(0f, 1f),
                VerticalAlignment.Middle => new Vector2(0f, 0.5f),
                VerticalAlignment.Bottom => new Vector2(0f, 0f),
                _ => throw new InvalidEnumArgumentException()
            };
            RectTransform.anchorMin = vector;
            RectTransform.anchorMax = vector;
            RectTransform.pivot = vector;
            var position = RectTransform.anchoredPosition3D;
            position.y = 0f;
            RectTransform.anchoredPosition3D = position;
        }

        /// <summary>
        /// アイテムの幅をコンテンツ幅に合わせて設定します
        /// </summary>
        /// <param name="contentWidth">コンテンツの幅</param>
        public void SetFittingItemWidth(float contentWidth)
        {
            var size = RectTransform.sizeDelta;
            size.x = contentWidth;
            RectTransform.sizeDelta = size;
        }

        /// <summary>
        /// アイテムの高さをコンテンツ高さに合わせて設定します
        /// </summary>
        /// <param name="contentHeight">コンテンツの高さ</param>
        public void SetFittingItemHeight(float contentHeight)
        {
            var size = RectTransform.sizeDelta;
            size.y = contentHeight;
            RectTransform.sizeDelta = size;
        }

        /// <summary>
        /// アイテムがビューポート内にあるかどうかを更新します
        /// </summary>
        public void UpdateIsInside()
        {
            var worldRect = GetWorldRect(RectTransform);
            var viewPortRect = GetWorldRect(viewPort);
            IsInside = viewPortRect.Overlaps(worldRect);
        }

        /// <summary>
        /// アイテムの位置を設定します
        /// </summary>
        /// <param name="position">新しい位置</param>
        public void SetPosition(Vector3 position)
        {
            RectTransform.anchoredPosition3D = position;
        }

        /// <summary>
        /// RectTransformのワールド座標でのRectを取得します
        /// </summary>
        /// <param name="rectTransform">対象のRectTransform</param>
        /// <returns>ワールド座標でのRect</returns>
        protected static Rect GetWorldRect(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return new Rect(
                corners[0].x,
                corners[0].y,
                corners[2].x - corners[0].x,
                corners[2].y - corners[0].y
            );
        }
    }
}
