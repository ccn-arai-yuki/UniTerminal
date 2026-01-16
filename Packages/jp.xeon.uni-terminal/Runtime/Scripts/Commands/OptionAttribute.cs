using System;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// フィールドまたはプロパティをコマンドオプションとしてマークするための属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class OptionAttribute : Attribute
    {
        /// <summary>
        /// ロングオプション名（--なし）。
        /// </summary>
        public string LongName { get; }

        /// <summary>
        /// ショートオプション名（単一文字、-なし）。
        /// </summary>
        public string ShortName { get; }

        /// <summary>
        /// このオプションが必須かどうか。
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// ヘルプ表示用のオプション説明。
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 新しいOptionAttributeを作成します。
        /// </summary>
        /// <param name="longName">ロングオプション名（--なし）。</param>
        /// <param name="shortName">ショートオプション名（単一文字、-なし）。</param>
        /// <param name="isRequired">このオプションが必須かどうか。</param>
        public OptionAttribute(string longName, string shortName = "", bool isRequired = false)
        {
            LongName = longName ?? throw new ArgumentNullException(nameof(longName));
            ShortName = shortName ?? "";
            IsRequired = isRequired;
        }
    }
}
