using UnityEngine;

namespace Xeon.Common.FlyweightScrollView
{
    /// <summary>
    /// スクロールビューのレイアウト計算を行う基底クラス
    /// アイテムの配置位置、コンテナサイズ、表示インデックスの計算を担当します
    /// </summary>
    public abstract class Layouter
    {
        /// <summary>
        /// コンテンツコンテナのRectTransform
        /// </summary>
        protected RectTransform container;

        /// <summary>
        /// ビューポートのRectTransform
        /// </summary>
        protected RectTransform viewPort;

        /// <summary>
        /// アイテムのサイズ
        /// </summary>
        protected Vector2 itemSize;

        /// <summary>
        /// アイテム間のスペース
        /// </summary>
        protected float spacing;

        /// <summary>
        /// パディング設定
        /// </summary>
        protected RectOffset padding;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="container">コンテンツコンテナのRectTransform</param>
        /// <param name="param">スクロールビューのパラメータ</param>
        /// <param name="itemSize">アイテムのサイズ</param>
        public Layouter(RectTransform container, FlyweightScrollViewParam param, Vector2 itemSize)
        {
            this.container = container;
            this.itemSize = itemSize;
            viewPort = param.ViewPort;
            padding = param.Padding;
            spacing = param.Spacing;
        }

        /// <summary>
        /// アイテム間のスペースを設定します
        /// </summary>
        public float Spacing
        {
            set => spacing = value;
        }

        /// <summary>
        /// スクロール方向のアイテムサイズ（スペース込み）
        /// </summary>
        public abstract float ItemSize { get; }

        /// <summary>
        /// 表示に必要なアイテム数を取得します
        /// </summary>
        /// <returns>アイテム数</returns>
        public int GetItemCount() => GetTailIndex() + 1;

        public Vector2 GetFitItemSize()
        {
            return new Vector2(container.rect.width - padding.horizontal, container.rect.height - padding.vertical);
        }

        /// <summary>
        /// 表示範囲の末尾インデックスを取得します
        /// </summary>
        /// <returns>末尾インデックス</returns>
        public abstract int GetTailIndex();

        /// <summary>
        /// アイテムのサイズを設定します
        /// </summary>
        /// <param name="item">対象のアイテム</param>
        public abstract void SetItemSize(FlyweightScrollViewItemBase item);

        /// <summary>
        /// コンテナのサイズを更新します
        /// </summary>
        /// <param name="itemCount">総アイテム数</param>
        public abstract void UpdateContainerSize(int itemCount);

        /// <summary>
        /// スクロール位置から表示開始インデックスを計算します
        /// </summary>
        /// <param name="itemCount">総アイテム数</param>
        /// <param name="scrollPosition">正規化されたスクロール位置（0〜1）</param>
        /// <returns>表示開始インデックス</returns>
        public abstract int CalculateIndex(int itemCount, float scrollPosition);

        /// <summary>
        /// 指定インデックスのアイテムの位置を取得します
        /// </summary>
        /// <param name="index">アイテムのインデックス</param>
        /// <returns>アイテムの位置</returns>
        public abstract Vector3 GetPosition(int index);

        /// <summary>
        /// 総コンテンツサイズを取得します
        /// </summary>
        /// <param name="itemCount">総アイテム数</param>
        /// <returns>コンテンツサイズ（ピクセル）</returns>
        public abstract float GetContentSize(int itemCount);

        /// <summary>
        /// 正規化されたスクロール位置からピクセルオフセットを計算します
        /// </summary>
        /// <param name="itemCount">総アイテム数</param>
        /// <param name="scrollPosition">正規化されたスクロール位置（0〜1）</param>
        /// <returns>ピクセルオフセット</returns>
        public abstract float GetContentOffset(int itemCount, float scrollPosition);

        /// <summary>
        /// ピクセルオフセットから正規化されたスクロール位置を計算します
        /// </summary>
        /// <param name="itemCount">総アイテム数</param>
        /// <param name="contentOffset">ピクセルオフセット</param>
        /// <returns>正規化されたスクロール位置（0〜1）</returns>
        public abstract float GetScrollPositionFromOffset(int itemCount, float contentOffset);
    }
}
