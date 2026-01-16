using System;
using UnityEngine;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// Flyweightスクロールビューのパラメータ設定
    /// Inspector上で設定可能なシリアライズ可能クラスです
    /// </summary>
    [Serializable]
    public class FlyweightScrollViewParam
    {
        /// <summary>
        /// ビューポートのRectTransform
        /// </summary>
        [SerializeField]
        private RectTransform viewPort;

        /// <summary>
        /// コンテンツのパディング
        /// </summary>
        [SerializeField]
        private RectOffset padding;

        /// <summary>
        /// アイテム間のスペース
        /// </summary>
        [SerializeField]
        private float spacing;

        /// <summary>
        /// 子アイテムのサイズを自動制御するかどうか
        /// </summary>
        [SerializeField]
        private bool isControlChildSize;

        /// <summary>
        /// 逆順表示モード
        /// </summary>
        [SerializeField]
        private bool isReverse;

        /// <summary>
        /// 末尾固定モード。新しいアイテムが追加されたときに末尾に自動スクロールします
        /// </summary>
        [SerializeField]
        private bool isAtLastSticky;

        /// <summary>
        /// ビューポートのRectTransform
        /// </summary>
        public RectTransform ViewPort => viewPort;

        /// <summary>
        /// コンテンツのパディング
        /// </summary>
        public RectOffset Padding => padding;

        /// <summary>
        /// アイテム間のスペース
        /// </summary>
        public float Spacing
        {
            get => spacing;
            set => spacing = value;
        }

        /// <summary>
        /// 子アイテムのサイズを自動制御するかどうか
        /// </summary>
        public bool IsControlChildSize => isControlChildSize;

        /// <summary>
        /// 逆順表示モード
        /// </summary>
        public bool IsReverse
        {
            get => isReverse;
            set => isReverse = value;
        }

        /// <summary>
        /// 末尾固定モード
        /// </summary>
        public bool IsAtLastSticky => isAtLastSticky;
    }
}
