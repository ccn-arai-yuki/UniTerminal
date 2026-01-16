using System;
using TMPro;
using UnityEngine;
using Xeon.Common;

namespace Xeon.UniTerminal
{
    public class OutputItem : MonoBehaviour, IBindable<OutputData>
    {
        [SerializeField] private TMP_Text label;
        public void Bind(OutputData data)
        {
            if (data.IsError)
                label.text = $"<color=red>{data.Message}</color>";
            else
                label.text = data.Message;
        }

        public event Action<OutputData> OnSelect;
    }
}
