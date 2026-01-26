using System;
using System.Collections.Generic;
using System.Reflection;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// CommandAttributeとOptionAttributesから抽出されたコマンドのメタデータ。
    /// </summary>
    public class CommandMetadata
    {
        /// <summary>
        /// コマンド名。
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// コマンドの説明。
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// ICommandを実装する型。
        /// </summary>
        public Type CommandType { get; }

        /// <summary>
        /// このコマンドのオプションメタデータ。
        /// </summary>
        public IReadOnlyList<OptionMetadata> Options { get; }

        private readonly Dictionary<string, OptionMetadata> longNameMap;
        private readonly Dictionary<string, OptionMetadata> shortNameMap;

        /// <summary>
        /// コマンドメタデータを初期化します。
        /// </summary>
        /// <param name="commandName">コマンド名。</param>
        /// <param name="description">コマンド説明。</param>
        /// <param name="commandType">コマンド型。</param>
        /// <param name="options">オプションメタデータ一覧。</param>
        public CommandMetadata(
            string commandName,
            string description,
            Type commandType,
            List<OptionMetadata> options)
        {
            CommandName = commandName;
            Description = description;
            CommandType = commandType;
            Options = options;

            longNameMap = new Dictionary<string, OptionMetadata>(StringComparer.OrdinalIgnoreCase);
            shortNameMap = new Dictionary<string, OptionMetadata>(StringComparer.OrdinalIgnoreCase);

            foreach (var opt in options)
            {
                longNameMap[opt.LongName] = opt;
                if (!string.IsNullOrEmpty(opt.ShortName))
                {
                    shortNameMap[opt.ShortName] = opt;
                }
            }
        }

        /// <summary>
        /// ロング名でオプションを検索します。
        /// </summary>
        public bool TryGetOptionByLongName(string longName, out OptionMetadata option)
        {
            return longNameMap.TryGetValue(longName, out option);
        }

        /// <summary>
        /// ショート名でオプションを検索します。
        /// </summary>
        public bool TryGetOptionByShortName(string shortName, out OptionMetadata option)
        {
            return shortNameMap.TryGetValue(shortName, out option);
        }

        /// <summary>
        /// コマンドの新しいインスタンスを作成します。
        /// </summary>
        public ICommand CreateInstance()
        {
            return (ICommand)Activator.CreateInstance(CommandType);
        }

        /// <summary>
        /// このコマンドのヘルプテキストを生成します。
        /// </summary>
        public string GenerateHelp()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{CommandName} - {Description}");
            sb.AppendLine();

            if (Options.Count > 0)
            {
                sb.AppendLine("Options:");
                foreach (var opt in Options)
                {
                    var shortPart = string.IsNullOrEmpty(opt.ShortName) ? "    " : $"-{opt.ShortName}, ";
                    var required = opt.IsRequired ? " (required)" : "";
                    var typeName = GetFriendlyTypeName(opt.OptionType);

                    sb.Append($"  {shortPart}--{opt.LongName}");
                    if (!opt.IsBool)
                    {
                        sb.Append($" <{typeName}>");
                    }
                    sb.Append(required);
                    if (!string.IsNullOrEmpty(opt.Description))
                    {
                        sb.Append($"  {opt.Description}");
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type.IsEnum) return "enum";
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = type.GetGenericArguments()[0];
                return $"list<{GetFriendlyTypeName(elementType)}>";
            }
            return type.Name;
        }
    }
}
