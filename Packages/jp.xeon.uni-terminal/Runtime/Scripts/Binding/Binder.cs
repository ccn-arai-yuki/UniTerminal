using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Xeon.UniTerminal.Parsing;

namespace Xeon.UniTerminal.Binding
{
    /// <summary>
    /// パースされたコマンドを型変換されたオプションを持つコマンドインスタンスにバインドします。
    /// </summary>
    public class Binder
    {
        private readonly CommandRegistry registry;

        /// <summary>
        /// コマンドレジストリを受け取りバインダーを初期化します。
        /// </summary>
        /// <param name="registry">コマンドレジストリ。</param>
        public Binder(CommandRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// パースされたパイプラインをバインド済みコマンドに変換します。
        /// </summary>
        /// <param name="pipeline">パース済みパイプライン。</param>
        /// <exception cref="BindException">バインドエラー時にスローされます。</exception>
        public BoundPipeline Bind(ParsedPipeline pipeline)
        {
            var boundCommands = new List<BoundCommand>();

            foreach (var parsedCmd in pipeline.Commands)
            {
                var boundCmd = BindCommand(parsedCmd);
                boundCommands.Add(boundCmd);
            }

            return new BoundPipeline(boundCommands);
        }

        /// <summary>
        /// 単一のパースされたコマンドをバインドします。
        /// </summary>
        /// <param name="parsedCmd">パース済みコマンド。</param>
        public BoundCommand BindCommand(ParsedCommand parsedCmd)
        {
            // コマンドを検索
            if (!registry.TryGetCommand(parsedCmd.CommandName, out var metadata))
            {
                throw new BindException(
                    $"command not found: {parsedCmd.CommandName}\n\n{registry.GenerateGlobalHelp()}",
                    parsedCmd.CommandName);
            }

            var context = new CommandBindingContext(parsedCmd, metadata, ConvertValue);
            return context.Process();
        }

        private object ConvertValue(string rawValue, OptionMetadata optMeta, bool wasQuoted)
        {
            var type = optMeta.OptionType;

            // リスト型の処理
            if (optMeta.IsList)
            {
                return ConvertList(rawValue, optMeta.ListElementType, wasQuoted);
            }

            // スカラー型の処理
            return ConvertScalar(rawValue, type);
        }

        private object ConvertList(string rawValue, Type elementType, bool wasQuoted)
        {
            // クォートされている場合は単一要素として扱う
            if (wasQuoted)
            {
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                list.Add(ConvertScalar(rawValue, elementType));
                return list;
            }

            // カンマで分割
            var parts = rawValue.Split(',');
            var result = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

            foreach (var part in parts)
            {
                result.Add(ConvertScalar(part, elementType));
            }

            return result;
        }

        private object ConvertScalar(string rawValue, Type type)
        {
            if (type == typeof(string))
            {
                return rawValue;
            }

            if (type == typeof(int))
            {
                if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
                {
                    throw new FormatException($"Cannot convert '{rawValue}' to int");
                }
                return intVal;
            }

            if (type == typeof(float))
            {
                if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatVal))
                {
                    throw new FormatException($"Cannot convert '{rawValue}' to float");
                }
                return floatVal;
            }

            if (type == typeof(double))
            {
                if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleVal))
                {
                    throw new FormatException($"Cannot convert '{rawValue}' to double");
                }
                return doubleVal;
            }

            if (type.IsEnum)
            {
                // 大文字小文字を区別しないenumパース
                try
                {
                    return Enum.Parse(type, rawValue, ignoreCase: true);
                }
                catch (ArgumentException)
                {
                    var validValues = string.Join(", ", Enum.GetNames(type));
                    throw new FormatException($"Cannot convert '{rawValue}' to {type.Name}. Valid values: {validValues}");
                }
            }

            throw new NotSupportedException($"Unsupported option type: {type.Name}");
        }
    }
}
