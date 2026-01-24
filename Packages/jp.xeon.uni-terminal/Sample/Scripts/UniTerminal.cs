using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Xeon.Common.FlyweightScrollView;
using Xeon.Common.FlyweightScrollView.Model;
using Xeon.UniTerminal.Common;
#if UNI_TERMINAL_UNI_TASK_SUPPORT
using Xeon.UniTerminal.UniTask;
#endif

namespace Xeon.UniTerminal.Sample
{
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// UniTerminalのUIコントローラー
    /// ターミナルUIの入出力を管理し、コマンドの実行を行う
    /// </summary>
    public class UniTerminal : MonoBehaviour
    {
        private const int BufferSize = 1000;

        /// <summary>
        /// y/n確認を求める候補数のしきい値（bashに準拠）
        /// </summary>
        private const int CompletionConfirmThreshold = 100;

        [SerializeField] private TMP_InputField input;
        [SerializeField] private OutputItem messagePrefab;
        [SerializeField] private FlyweightVerticalScrollView scrollView;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TMP_Text currentDirectoryLabel;

        private Terminal terminal;
        private FlyweightScrollViewController<OutputData, OutputItem> scrollViewController;
        private CircularBuffer<OutputData> buffer = new(BufferSize);
#if UNI_TERMINAL_UNI_TASK_SUPPORT
        private OutputWriterUniTask normalOutput;
        private OutputWriterUniTask errorOutput;
#else
        private OutputWriter normalOutput;
        private OutputWriter errorOutput;
#endif

        /// <summary>
        /// 現在の履歴インデックス（-1は履歴を参照していない状態）
        /// </summary>
        private int historyIndex = -1;

        /// <summary>
        /// 履歴参照前の入力内容を保持
        /// </summary>
        private string currentInput = string.Empty;

        /// <summary>
        /// 最後に表示したディレクトリパス（更新検知用）
        /// </summary>
        private string lastDisplayedDirectory = string.Empty;

        /// <summary>
        /// y/n確認待ち状態かどうか
        /// </summary>
        private bool waitingForCompletionConfirmation;

        /// <summary>
        /// 確認待ち中の補完候補（表示テキストのリスト）
        /// </summary>
        private List<string> pendingCompletionCandidates;

        private int maxCharsPerLine = -1;

        private void Awake()
        {
            terminal = new Terminal(Application.persistentDataPath, Application.persistentDataPath, maxHistorySize: BufferSize);
            scrollViewController = new FlyweightScrollViewController<OutputData, OutputItem>(messagePrefab, buffer);
            scrollView.Setup(scrollViewController);
#if UNI_TERMINAL_UNI_TASK_SUPPORT
            normalOutput = new OutputWriterUniTask(buffer, false, () => maxCharsPerLine);
            errorOutput = new OutputWriterUniTask(buffer, true, () => maxCharsPerLine);
#else
            normalOutput = new OutputWriter(buffer, false, () => maxCharsPerLine);
            errorOutput = new OutputWriter(buffer, true, () => maxCharsPerLine);
#endif

            input.onEndEdit.AddListener(OnInputCommand);
        }

        private void Update()
        {
            if (lastDisplayedDirectory != terminal.WorkingDirectory)
            {
                lastDisplayedDirectory = terminal.WorkingDirectory;
                currentDirectoryLabel.text = terminal.WorkingDirectory.Replace(terminal.HomeDirectory, "~/");
            }

            if (maxCharsPerLine < 0)
            {
                var sample = scrollViewController.GetSample();
                maxCharsPerLine = GetMaxCharsPerLine(sample);
            }
            // y/n確認待ち状態の処理
            if (waitingForCompletionConfirmation)
            {
                if (InputHandler.IsPressedY())
                {
                    waitingForCompletionConfirmation = false;
                    DisplayCompletionCandidates(pendingCompletionCandidates);
                    pendingCompletionCandidates = null;
                    FocusInputFieldAsync().Forget();
                }
                else if (InputHandler.IsPressedN())
                {
                    waitingForCompletionConfirmation = false;
                    pendingCompletionCandidates = null;
                    FocusInputFieldAsync().Forget();
                }
                return;
            }

            // 入力フィールドがアクティブでない場合は無視
            if (!input.isFocused || !input.interactable)
            {
                return;
            }

            if (InputHandler.IsPressedTab())
                ProcessCompletions();

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

        private void ProcessCompletions()
        {
            var completions = terminal.GetCompletions(input.text);
            var candidates = completions.Candidates;
            if (candidates.Count <= 0)
                return;

            if (candidates.Count > 1)
            {
                var displayTexts = candidates.Select(c => c.DisplayText).ToList();

                // 候補数がしきい値を超える場合はy/n確認を求める
                if (candidates.Count >= CompletionConfirmThreshold)
                {
                    buffer.PushFront(new OutputData($"Display all {candidates.Count} possibilities? (y or n)", false));
                    ScrollToBottom();
                    pendingCompletionCandidates = displayTexts;
                    waitingForCompletionConfirmation = true;
                    input.DeactivateInputField();
                    return;
                }

                DisplayCompletionCandidates(displayTexts);
                return;
            }

            var candidate = completions.Candidates[0];
            var newText = input.text.Remove(completions.TokenStart, completions.TokenLength).Insert(completions.TokenStart, candidate.Text);
            SetInputText(newText);
        }

        /// <summary>
        /// 補完候補を適切な列数でフォーマットしてバッファに追加する
        /// </summary>
        /// <param name="displayTexts">表示する候補テキストのリスト</param>
        private void DisplayCompletionCandidates(List<string> displayTexts)
        {
            if (displayTexts == null || displayTexts.Count == 0)
                return;

            var sample = scrollViewController.GetSample();

            var displayText = string.Empty;
            for (var index = 0; index < displayTexts.Count; index++)
            {
                var tmp = displayText;
                if (index > 0) tmp += " ";
                tmp += displayTexts[index];
                if (tmp.Length >= maxCharsPerLine)
                {
                    buffer.PushFront(new OutputData(displayText, false));
                    displayText = displayTexts[index];
                    continue;
                }
                displayText = tmp;
            }

            ScrollToBottom();
        }

        /// <summary>
        /// サンプルのTMP_Textを使用して1行に表示可能な最大文字数を取得する
        /// </summary>
        /// <param name="sample">サンプル用のOutputItem</param>
        /// <returns>1行に表示可能な最大文字数</returns>
        private int GetMaxCharsPerLine(OutputItem sample)
        {
            var go = sample.gameObject;
            var wasActive = go.activeInHierarchy;

            // GameObjectが無効な場合は一時的に有効化
            if (!wasActive)
            {
                go.SetActive(true);
            }

            // TextMeshUtilityに計算を委譲
            const string sampleChars = "WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW";
            var maxChars = sample.Label.GetMaxCharacterCountInOneLine(sampleChars);

            // 元の状態に戻す
            if (!wasActive)
            {
                go.SetActive(false);
            }

            return Math.Max(1, maxChars);
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
                FocusInputFieldAsync().Forget();
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
#if UNI_TERMINAL_UNI_TASK_SUPPORT
                await terminal.ExecuteUniTaskAsync(command, normalOutput, errorOutput, ct: destroyCancellationToken);
#else
                await terminal.ExecuteAsync(command, normalOutput, errorOutput, ct: destroyCancellationToken);
#endif
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
                FocusInputFieldAsync().Forget();
            }
        }

        /// <summary>
        /// 次フレームで入力フィールドにフォーカスを設定する
        /// </summary>
        private async UniTaskVoid FocusInputFieldAsync()
        {
            await Cysharp.Threading.Tasks.UniTask.NextFrame(cancellationToken: destroyCancellationToken);
            if (input == null) return;
            input.ActivateInputField();
            input.Select();
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
