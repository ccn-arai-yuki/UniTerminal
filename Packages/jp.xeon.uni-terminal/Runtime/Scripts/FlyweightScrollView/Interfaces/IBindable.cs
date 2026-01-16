using System;

namespace Xeon.Common
{
    /// <summary>
    /// データバインディング可能なUIアイテムのインターフェース
    /// スクロールビューのアイテムがデータをバインドするために実装します
    /// </summary>
    /// <typeparam name="TData">バインドするデータの型</typeparam>
    public interface IBindable<TData>
    {
        /// <summary>
        /// データをUIにバインドします
        /// </summary>
        /// <param name="data">バインドするデータ</param>
        void Bind(TData data);

        /// <summary>
        /// アイテムが選択されたときに発火するイベント
        /// </summary>
        event Action<TData> OnSelect;
    }
}
