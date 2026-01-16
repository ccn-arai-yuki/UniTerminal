using UnityEngine;
using UnityEngine.UI;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// Flyweightパターンを使用した仮想スクロールビューの基底クラス
    /// 大量のデータを効率的に表示するため、表示領域に必要な最小限のUIアイテムのみを生成・再利用します
    /// </summary>
    public abstract class FlyweightScrollView : MonoBehaviour
    {
        /// <summary>
        /// Unity標準のScrollRectコンポーネント
        /// </summary>
        [SerializeField]
        protected ScrollRect scrollView;

        /// <summary>
        /// スクロールビューのビューポート。サイズ変更を監視します
        /// </summary>
        [SerializeField]
        protected FlyweightScrollViewport viewPort;

        /// <summary>
        /// スクロールコンテンツのRectTransform。アイテムの親となります
        /// </summary>
        [SerializeField]
        protected RectTransform content;

        /// <summary>
        /// スクロールビューのパラメータ設定
        /// </summary>
        [SerializeField]
        protected FlyweightScrollViewParam param = new();

        /// <summary>
        /// スクロールビューのコントローラー。アイテムの生成・配置・データバインディングを管理します
        /// </summary>
        protected FlyweightScrollViewControllerBase controller;

        /// <summary>
        /// 前回のスクロール位置。スクロール方向の判定に使用します
        /// </summary>
        protected Vector2 prevScrollPosition = Vector2.zero;

        /// <summary>
        /// アイテム間のスペース（ピクセル）
        /// </summary>
        public float Spacing
        {
            get => param.Spacing;
            set
            {
                param.Spacing = value;
                controller?.SetSpacing(param.Spacing);
            }
        }

        /// <summary>
        /// 正規化されたスクロール位置（0〜1）
        /// </summary>
        public Vector2 normalizedPosition
        {
            get => scrollView.normalizedPosition;
            set => scrollView.normalizedPosition = value;
        }

        /// <summary>
        /// スクロールビューを初期化します
        /// </summary>
        /// <param name="controller">スクロールビューを制御するコントローラー</param>
        public virtual void Setup(FlyweightScrollViewControllerBase controller)
        {
            scrollView.onValueChanged.RemoveListener(OnChangedScrollPosition);
            scrollView.onValueChanged.AddListener(OnChangedScrollPosition);
            viewPort.OnRectTransformDimensionsChanged -= controller.UpdateViewportSize;
            viewPort.OnRectTransformDimensionsChanged += controller.UpdateViewportSize;
            prevScrollPosition = scrollView.normalizedPosition;
            this.controller = controller;
            if (param.IsAtLastSticky)
                controller.SetIsPositionLast(true);
        }

        private void OnDestroy()
        {
            controller?.Dispose();
            controller = null;
        }

        private void Update()
        {
            if (controller == null)
                return;
            if (controller.IsDirty)
            {
                controller.UpdateView();
                controller.IsDirty = false;
            }
        }

        /// <summary>
        /// 逆順モードを設定します。派生クラスで実装します
        /// </summary>
        protected abstract void SetReverseMode();

        /// <summary>
        /// スクロール位置が変更されたときに呼び出されます。派生クラスで実装します
        /// </summary>
        /// <param name="position">新しいスクロール位置（正規化された値）</param>
        protected abstract void OnChangedScrollPosition(Vector2 position);
    }
}
