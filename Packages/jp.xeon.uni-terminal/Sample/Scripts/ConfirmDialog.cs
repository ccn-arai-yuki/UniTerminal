using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Xeon.UniTerminal.Common;

namespace Xeon.UniTerminal.Sample
{
#if UNI_TERMINAL_UNI_TASK_SUPPORT
    using Cysharp.Threading.Tasks;
#else
    using System.Threading.Tasks;
#endif
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField]
        private CanvasGroup canvasGroup;
        [SerializeField]
        private TMP_Text message;
        [SerializeField]
        private Button yesButton;
        [SerializeField]
        private Button noButton;

#if UNI_TERMINAL_UNI_TASK_SUPPORT
        UniTaskCompletionSource<bool> completionSource;
        AnimationControllerUniTask animationController;
#else
        TaskCompletionSource<bool> completionSource;
        AnimationController animationController;
#endif

        private void Awake()
        {
            yesButton.onClick.AddListener(OnClickYes);
            noButton.onClick.AddListener(OnClickNo);
#if UNI_TERMINAL_UNI_TASK_SUPPORT
            animationController = new AnimationControllerUniTask(canvasGroup);
#else
            animationController = new AnimationController(canvasGroup, this);
#endif
        }

#if UNI_TERMINAL_UNI_TASK_SUPPORT
        public async UniTask<bool> ShowAsync(string message, CancellationToken token = default)
        {
            completionSource = new UniTaskCompletionSource<bool>();
            this.message.text = message;
            await animationController.OpenAsync(token);
            var result = await completionSource.Task;
            await animationController.CloseAsync(token);
            return result;
        }
#else
        public async Task<bool> ShowAsync(string message, CancellationToken token = default)
        {
            completionSource = new TaskCompletionSource<bool>();
            this.message.text = message;
            await animationController.OpenAsync(token);
            var result = await completionSource.Task;
            await animationController.CloseAsync(token);
            return result;
        }
#endif

        private void Update()
        {
            if (completionSource == null)
                return;

            if (InputHandler.IsPressedY())
                OnClickYes();
            else if (InputHandler.IsPressedN())
                OnClickNo();
        }

        private void OnDestroy()
        {
            yesButton.onClick.RemoveListener(OnClickYes);
            noButton.onClick.RemoveListener(OnClickNo);
        }

        private void OnClickYes() => completionSource.TrySetResult(true);
        private void OnClickNo() => completionSource.TrySetResult(false);

    }
}
