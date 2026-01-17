#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Xeon.Common.FlyweightScrollView;
using Xeon.Common.FlyweightScrollView.Model;
using Xeon.UniTerminal.UniTask;
using Xeon.UniTerminal.Common;

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

        /// <summary>
        /// 現在の履歴インデックス（-1は履歴を参照していない状態）
        /// </summary>
        private int historyIndex = -1;

        /// <summary>
        /// 履歴参照前の入力内容を保持
        /// </summary>
        private string currentInput = string.Empty;

        private void Awake()
        {
            terminal = new Terminal(Application.persistentDataPath, Application.persistentDataPath, maxHistorySize: BufferSize);
            scrollViewController = new FlyweightScrollViewController<OutputData, OutputItem>(messagePrefab, buffer);
            scrollView.Setup(scrollViewController);

            normalOutput = new OutputWriter(buffer, false);
            errorOutput = new OutputWriter(buffer, true);

            input.onEndEdit.AddListener(OnInputCommand);
        }

        private void Update()
        {
            // 入力フィールドがアクティブでない場合は無視
            if (!input.isFocused || !input.interactable)
            {
                return;
            }

            if (InputHandler.IsPressedTab())
                ProcessComletions();

            // 上キーで履歴を遡る
            if (InputHandler.IsPressedUpArrow())
            {
                NavigateHistoryUp();
            }
            // 下キーで履歴を進む
            else if (InputHandler.IsPressedDownArrow())
            {
                NavigateHistoryDown();
            }
        }

        /// <summary>
        /// 履歴を遡る（古いコマンドへ）
        /// </summary>
        private void NavigateHistoryUp()
        {
            var history = terminal.CommandHistory;
            if (history.Count == 0)
            {
                return;
            }

            // 初めて履歴を参照する場合、現在の入力を保存
            if (historyIndex == -1)
            {
                currentInput = input.text;
                historyIndex = history.Count;
            }

            // 履歴の先頭（最も古い）に達していなければ遡る
            if (historyIndex > 0)
            {
                historyIndex--;
                SetInputText(history[historyIndex]);
            }
        }

        /// <summary>
        /// 履歴を進む（新しいコマンドへ）
        /// </summary>
        private void NavigateHistoryDown()
        {
            var history = terminal.CommandHistory;

            // 履歴を参照していない場合は何もしない
            if (historyIndex == -1)
            {
                return;
            }

            historyIndex++;

            // 履歴の末尾を超えたら現在の入力に戻る
            if (historyIndex >= history.Count)
            {
                historyIndex = -1;
                SetInputText(currentInput);
            }
            else
            {
                SetInputText(history[historyIndex]);
            }
        }

        private void ProcessComletions()
        {
            var completions = terminal.GetCompletions(input.text);
            var candidates = completions.Candidates;
            if (candidates.Count <= 0)
                return;

            if (candidates.Count > 1)
            {
                var text = string.Join("\t", candidates.Select(candidate => candidate.DisplayText));
                buffer.PushFront(new OutputData(text, false));
                return;
            }
            
            var candidate = completions.Candidates[0];
            var newText = input.text.Remove(completions.TokenStart, completions.TokenLength).Insert(completions.TokenStart, candidate.Text);
            SetInputText(newText);
        }

        /// <summary>
        /// 入力フィールドにテキストを設定し、キャレットを末尾に移動する
        /// </summary>
        /// <param name="text">設定するテキスト</param>
        private void SetInputText(string text)
        {
            input.text = text;
            // キャレットを末尾に移動
            input.caretPosition = text.Length;
            input.selectionAnchorPosition = text.Length;
            input.selectionFocusPosition = text.Length;
        }

        /// <summary>
        /// 履歴インデックスをリセットする
        /// </summary>
        private void ResetHistoryIndex()
        {
            historyIndex = -1;
            currentInput = string.Empty;
        }

        private async void OnInputCommand(string command)
        {
            // Enterキー以外での終了（フォーカス喪失など）は無視
            if (!InputHandler.IsPressedReturn())
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

            // 履歴インデックスをリセット
            ResetHistoryIndex();

            try
            {
                input.interactable = false;
                buffer.PushFront(new OutputData($"> {command}", false));
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
