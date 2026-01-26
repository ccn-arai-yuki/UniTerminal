using System;
using System.Reflection;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// OptionAttributeから抽出されたコマンドオプションのメタデータ。
    /// </summary>
    public class OptionMetadata
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
        /// オプションの説明。
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// オプションの型。
        /// </summary>
        public Type OptionType { get; }

        /// <summary>
        /// このオプションがマップされるメンバー（フィールドまたはプロパティ）。
        /// </summary>
        public MemberInfo Member { get; }

        /// <summary>
        /// このオプションがboolフラグかどうか。
        /// </summary>
        public bool IsBool { get; }

        /// <summary>
        /// このオプションがリスト型かどうか。
        /// </summary>
        public bool IsList { get; }

        /// <summary>
        /// リストオプションの要素型。
        /// </summary>
        public Type ListElementType { get; }

        /// <summary>
        /// オプションメタデータを初期化します。
        /// </summary>
        /// <param name="longName">ロングオプション名。</param>
        /// <param name="shortName">ショートオプション名。</param>
        /// <param name="isRequired">必須オプションかどうか。</param>
        /// <param name="description">オプション説明。</param>
        /// <param name="optionType">オプション型。</param>
        /// <param name="member">対象のメンバー。</param>
        public OptionMetadata(
            string longName,
            string shortName,
            bool isRequired,
            string description,
            Type optionType,
            MemberInfo member)
        {
            LongName = longName;
            ShortName = shortName;
            IsRequired = isRequired;
            Description = description;
            OptionType = optionType;
            Member = member;

            // リフレクション結果をキャッシュ
            IsBool = optionType == typeof(bool);
            IsList = optionType.IsGenericType &&
                     optionType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>);
            ListElementType = IsList ? optionType.GetGenericArguments()[0] : null;
        }

        /// <summary>
        /// コマンドインスタンスからこのオプションの値を取得します。
        /// </summary>
        public object GetValue(object instance)
        {
            return Member switch
            {
                FieldInfo field => field.GetValue(instance),
                PropertyInfo prop => prop.GetValue(instance),
                _ => null
            };
        }

        /// <summary>
        /// コマンドインスタンスにこのオプションの値を設定します。
        /// </summary>
        public void SetValue(object instance, object value)
        {
            switch (Member)
            {
                case FieldInfo field:
                    field.SetValue(instance, value);
                    break;
                case PropertyInfo prop:
                    prop.SetValue(instance, value);
                    break;
            }
        }

        /// <summary>
        /// 表示用文字列を生成します。
        /// </summary>
        public override string ToString()
        {
            var shortPart = string.IsNullOrEmpty(ShortName) ? "" : $"-{ShortName}, ";
            var required = IsRequired ? " (required)" : "";
            return $"{shortPart}--{LongName}{required}: {OptionType.Name}";
        }
    }
}
