#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Threading;
using UnityEngine;

namespace Xeon.UniTerminal.Sample
{
    using Cysharp.Threading.Tasks;
    /// <summary>
    /// アニメーションコントローラー(UniTask版)
    /// </summary>
    public class AnimationControllerUniTask
    {
        private const float AnimationDuration = 0.2f;
        private CanvasGroup canvasGroup;

        /// <summary>
        /// アニメーション制御用のコンストラクタ
        /// </summary>
        /// <param name="canvasGroup">対象のCanvasGroup</param>
        public AnimationControllerUniTask(CanvasGroup canvasGroup)
        {
            this.canvasGroup = canvasGroup;
        }

        /// <summary>
        /// フェードインを実行します
        /// </summary>
        /// <param name="token">キャンセルトークン</param>
        public async UniTask OpenAsync(CancellationToken token = default)
        {
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            var elapsed = 0f;
            while (elapsed < AnimationDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / AnimationDuration);
                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(token);
            }
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// フェードアウトを実行します
        /// </summary>
        /// <param name="token">キャンセルトークン</param>
        public async UniTask CloseAsync(CancellationToken token = default)
        {
            var elapsed = 0f;
            while (elapsed < AnimationDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / AnimationDuration);
                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(token);
            }
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }
}
#endif
