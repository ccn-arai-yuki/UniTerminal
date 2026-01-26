#if !UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Xeon.UniTerminal.Sample
{
    /// <summary>
    /// CanvasGroupのフェードアニメーションを制御します。
    /// </summary>
    public class AnimationController
    {
        private const float AnimationDuration = 0.2f;
        private CanvasGroup canvasGroup;
        private MonoBehaviour taskRunner;

        /// <summary>
        /// アニメーション制御用のコンストラクタ。
        /// </summary>
        /// <param name="canvasGroup">対象のCanvasGroup。</param>
        /// <param name="taskRunner">コルーチン実行用のMonoBehaviour。</param>
        public AnimationController(CanvasGroup canvasGroup, MonoBehaviour taskRunner)
        {
            this.canvasGroup = canvasGroup;
            this.taskRunner = taskRunner;
        }

        /// <summary>
        /// フェードインを実行します。
        /// </summary>
        /// <param name="token">キャンセルトークン。</param>
        public async Task OpenAsync(CancellationToken token = default)
        {
            var completionSource = new TaskCompletionSource<bool>();
            taskRunner.StartCoroutine(OpenCoroutine(completionSource));
            await completionSource.Task;
        }

        /// <summary>
        /// フェードアウトを実行します。
        /// </summary>
        /// <param name="token">キャンセルトークン。</param>
        public async Task CloseAsync(CancellationToken token = default)
        {
            var completionSource = new TaskCompletionSource<bool>();
            taskRunner.StartCoroutine(CloseCoroutine(completionSource));
            await completionSource.Task;
        }

        private IEnumerator OpenCoroutine(TaskCompletionSource<bool> completionSource)
        {
            var elapsed = 0f;
            canvasGroup.gameObject.SetActive(true);
            while (elapsed < AnimationDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / AnimationDuration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            canvasGroup.alpha = 1f;
            completionSource.SetResult(true);
        }

        private IEnumerator CloseCoroutine(TaskCompletionSource<bool> completionSource)
        {
            var elapsed = 0f;
            while (elapsed < AnimationDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / AnimationDuration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
            completionSource.SetResult(true);
        }
    }
}
#endif
