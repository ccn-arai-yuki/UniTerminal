using System;
using UnityEngine;
using UnityEngine.UI;

namespace Xeon.Common
{
    /// <summary>
    /// Flyweightスクロールビューのビューポートコンポーネント
    /// RectTransformのサイズ変更を監視し、イベントを発火します
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(RectMask2D))]
    public class FlyweightScrollViewport : MonoBehaviour
    {
        /// <summary>
        /// ビューポートのRectTransform
        /// </summary>
        [SerializeField]
        private RectTransform rectTransform;

        private bool isDirty = false;

        /// <summary>
        /// ビューポートのRectTransform
        /// </summary>
        public RectTransform RectTransform => rectTransform;

        /// <summary>
        /// RectTransformのサイズが変更されたときに発火するイベント
        /// </summary>
        public event Action OnRectTransformDimensionsChanged;

        /// <summary>
        /// Unity組み込みコールバック。RectTransformのサイズ変更時に呼び出されます
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            isDirty = true;
        }

        private void Update()
        {
            if (!isDirty)
                return;
            OnRectTransformDimensionsChanged?.Invoke();
            isDirty = false;
        }
    }
}
