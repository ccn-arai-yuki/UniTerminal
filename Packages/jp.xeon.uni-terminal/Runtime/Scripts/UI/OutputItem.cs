using System;
using TMPro;
using UnityEngine;
using Xeon.Common;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// ターミナル出力アイテムのUIコンポーネント
    /// 出力データをUIにバインドする
    /// </summary>
    public class OutputItem : MonoBehaviour, IBindable<OutputData>
    {
        [SerializeField] private TMP_Text label;

        /// <summary>
        /// 出力データをUIにバインドする
        /// </summary>
        /// <param name="data">バインドするデータ</param>
        public void Bind(OutputData data)
        {
            if (label == null) return;

            if (data.IsError)
            {
                label.text = $"<color=red>{data.Message}</color>";
            }
            else
            {
                label.text = data.Message;
            }
        }

        /// <summary>
        /// アイテムが選択されたときに発火するイベント
        /// </summary>
        public event Action<OutputData> OnSelect;
    }
}
