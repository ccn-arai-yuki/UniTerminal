#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Xeon.Common.FlyweightScrollView;
using Xeon.Common.FlyweightScrollView.Model;
using Xeon.UniTerminal.UniTask;

namespace Xeon.UniTerminal
{
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// UniTerminalのUIコントローラー
    /// ターミナルUIの入出力を管理し、コマンドの実行を行う
    /// </summary>
    public class UniTerminal : MonoBehaviour
    {
        private const int BufferSize = 1000;

        [SerializeField] private TMP_InputField input;
        [SerializeField] private OutputItem messagePrefab;
        [SerializeField] private FlyweightVerticalScrollView scrollView;
        [SerializeField] private ScrollRect scrollRect;

        private Terminal terminal;
        private FlyweightScrollViewController<OutputData, OutputItem> scrollViewController;
        private CircularBuffer<OutputData> buffer = new(BufferSize);
        private OutputWriter normalOutput;
        private OutputWriter errorOutput;

        private void Awake()
        {
            terminal = new Terminal(Application.persistentDataPath, Application.persistentDataPath, maxHistorySize: BufferSize);
            scrollViewController = new FlyweightScrollViewController<OutputData, OutputItem>(messagePrefab, buffer);
            scrollView.Setup(scrollViewController);

            normalOutput = new OutputWriter(buffer, false);
            errorOutput = new OutputWriter(buffer, true);

            input.onEndEdit.AddListener(OnInputCommand);
        }

        private async void OnInputCommand(string command)
        {
            // Enterキー以外での終了（フォーカス喪失など）は無視
            if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                return;
            }

            // 空コマンドは無視
            if (string.IsNullOrWhiteSpace(command))
            {
                input.text = string.Empty;
                input.ActivateInputField();
                return;
            }

            try
            {
                input.interactable = false;
                buffer.Add(new OutputData($"> {command}", false));
                ScrollToBottom();
                input.DeactivateInputField();
                await terminal.ExecuteUniTaskAsync(command, normalOutput, errorOutput, ct: destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // キャンセルは正常終了として扱う
            }
            finally
            {
                ScrollToBottom();
                input.text = string.Empty;
                input.interactable = true;
                input.ActivateInputField();
            }
        }

        /// <summary>
        /// スクロールビューを最下部にスクロールする
        /// </summary>
        private void ScrollToBottom()
        {
            if (scrollRect != null)
            {
                scrollRect.normalizedPosition = Vector2.zero;
            }
            scrollViewController?.FixToLast();
        }
    }
}
#endif
