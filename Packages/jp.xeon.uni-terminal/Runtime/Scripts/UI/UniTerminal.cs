using System;
using TMPro;
using UnityEngine;
using Xeon.Common.FlyweightScrollView;
using Xeon.Common.FlyweightScrollView.Model;
using Xeon.UniTerminal.UniTask;

namespace Xeon.UniTerminal
{
    public class UniTerminal : MonoBehaviour
    {
        private const int BufferSize = 1000;
        
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Transform content;
        [SerializeField] private OutputItem messagePrefab;
        [SerializeField] private FlyweightVerticalScrollView scrollView;

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

            normalOutput = new OutputWriter(buffer);
            errorOutput = new OutputWriter(buffer);
            
            input.onEndEdit.AddListener(OnInputCommand);
        }

        private async void OnInputCommand(string command)
        {
            try
            {
                input.interactable = false;
                buffer.Add(new OutputData($"> {command}", false));
                input.DeactivateInputField();
                await terminal.ExecuteUniTaskAsync(command, normalOutput, errorOutput, ct: destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                input.text = string.Empty;
                input.interactable = true;
                input.ActivateInputField();
            }
        }
    }
}
