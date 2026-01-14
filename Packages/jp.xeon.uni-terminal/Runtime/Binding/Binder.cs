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
        private readonly CommandRegistry _registry;

        public Binder(CommandRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// パースされたパイプラインをバインドされたコマンドにバインドします。
        /// </summary>
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
        public BoundCommand BindCommand(ParsedCommand parsedCmd)
        {
            // コマンドを検索
            if (!_registry.TryGetCommand(parsedCmd.CommandName, out var metadata))
            {
                throw new BindException(
                    $"command not found: {parsedCmd.CommandName}\n\n{_registry.GenerateGlobalHelp()}",
                    parsedCmd.CommandName);
            }

            // コマンドインスタンスを作成
            var command = metadata.CreateInstance();

            // 設定されたオプションを追跡（重複検出用）
            var setOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // オプションを処理
            for (int i = 0; i < parsedCmd.Options.Count; i++)
            {
                var opt = parsedCmd.Options[i];
                OptionMetadata optMeta;

                if (opt.IsLong)
                {
                    if (!metadata.TryGetOptionByLongName(opt.Name, out optMeta))
                    {
                        throw new BindException(
                            $"unknown option: --{opt.Name}\n\n{metadata.GenerateHelp()}",
                            parsedCmd.CommandName);
                    }
                }
                else
                {
                    if (!metadata.TryGetOptionByShortName(opt.Name, out optMeta))
                    {
                        throw new BindException(
                            $"unknown option: -{opt.Name}\n\n{metadata.GenerateHelp()}",
                            parsedCmd.CommandName);
                    }
                }

                // リストの重複をチェック
                if (optMeta.IsList && setOptions.Contains(optMeta.LongName))
                {
                    throw new BindException(
                        $"list option --{optMeta.LongName} cannot be specified multiple times\n\n{metadata.GenerateHelp()}",
                        parsedCmd.CommandName);
                }

                // boolオプションの処理
                if (optMeta.IsBool)
                {
                    if (opt.HasValue)
                    {
                        throw new BindException(
                            $"boolean option --{optMeta.LongName} does not accept a value\n\n{metadata.GenerateHelp()}",
                            parsedCmd.CommandName);
                    }
                    optMeta.SetValue(command, true);
                    setOptions.Add(optMeta.LongName);
                    continue;
                }

                // bool以外のオプションは値が必要
                if (!opt.HasValue)
                {
                    throw new BindException(
                        $"option --{optMeta.LongName} requires a value\n\n{metadata.GenerateHelp()}",
                        parsedCmd.CommandName);
                }

                string rawValue = opt.RawValue;

                // 値を変換して設定
                try
                {
                    var value = ConvertValue(rawValue, optMeta, opt.WasQuoted);
                    optMeta.SetValue(command, value);
                    setOptions.Add(optMeta.LongName);
                }
                catch (BindException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new BindException(
                        $"failed to convert value '{rawValue}' for option --{optMeta.LongName}: {ex.Message}\n\n{metadata.GenerateHelp()}",
                        parsedCmd.CommandName,
                        ex);
                }
            }

            // 必須オプションをチェック
            foreach (var optMeta in metadata.Options)
            {
                if (optMeta.IsRequired && !setOptions.Contains(optMeta.LongName))
                {
                    throw new BindException(
                        $"required option --{optMeta.LongName} is missing\n\n{metadata.GenerateHelp()}",
                        parsedCmd.CommandName);
                }
            }

            return new BoundCommand(
                command,
                metadata,
                parsedCmd.PositionalArguments,
                parsedCmd.Redirections);
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
