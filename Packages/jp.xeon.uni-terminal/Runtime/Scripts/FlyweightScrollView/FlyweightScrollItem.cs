using UnityEngine;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// Flyweightスクロールビューで使用するジェネリックなアイテムクラス
    /// MonoBehaviourコンポーネントをラップし、位置管理と表示判定を行います
    /// </summary>
    /// <typeparam name="T">ラップするMonoBehaviourの型</typeparam>
    public class FlyweightScrollItem<T> : FlyweightScrollViewItemBase
        where T : MonoBehaviour
    {
        /// <summary>
        /// ラップしているMonoBehaviourコンポーネント
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="value">ラップするMonoBehaviourコンポーネント</param>
        /// <param name="viewPort">ビューポートのRectTransform</param>
        /// <param name="horizontalAlignment">水平方向の配置</param>
        public FlyweightScrollItem(T value, RectTransform viewPort, HorizontalAlignment horizontalAlignment)
            : base(viewPort)
        {
            Value = value;

            RectTransform = value.GetComponent<RectTransform>();
            gameObject = value.gameObject;

            SetHorizontalAlignment(horizontalAlignment);
            UpdateIsInside();
        }
    }
}
