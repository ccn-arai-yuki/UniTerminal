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

        public AnimationControllerUniTask(CanvasGroup canvasGroup)
        {
            this.canvasGroup = canvasGroup;
        }

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
