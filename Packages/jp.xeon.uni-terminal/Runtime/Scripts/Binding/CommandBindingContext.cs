using System;
using System.Collections.Generic;
using System.Linq;
using Xeon.UniTerminal.Parsing;

namespace Xeon.UniTerminal.Binding
{
    /// <summary>
    /// コマンドのオプションを解析し、バインド結果を構築するためのコンテキスト
    /// </summary>
    public class CommandBindingContext
    {
        /// <summary>
        /// オプション値を目的の型へ変換するデリゲート
        /// </summary>
        public delegate object ConvertValueDelegate(string rawValue, OptionMetadata metaData, bool wasQuoted);

        /// <summary>
        /// オプション解析中に追加された位置引数
        /// </summary>
        public List<string> ExtraPositionalArgs { get; }

        /// <summary>
        /// 対象コマンドのメタデータ
        /// </summary>
        public CommandMetadata MetaData { get; }

        /// <summary>
        /// パース済みコマンド
        /// </summary>
        public ParsedCommand ParsedCommand { get; }
        /// <summary>重複検出用の設定されたオプションのリスト</summary>
        public HashSet<string> SetOptions { get; }

        /// <summary>
        /// バインド対象のコマンドインスタンス
        /// </summary>
        public ICommand Command { get; }

        private ConvertValueDelegate convertValueFunc;

        /// <summary>
        /// バインド処理に必要な情報を初期化します
        /// </summary>
        /// <param name="parsedCommand">パース済みコマンド</param>
        /// <param name="metaData">コマンドのメタデータ</param>
        /// <param name="convertValueFunc">オプション値変換処理</param>
        public CommandBindingContext(ParsedCommand parsedCommand, CommandMetadata metaData, ConvertValueDelegate convertValueFunc)
        {
            ParsedCommand = parsedCommand;
            MetaData = metaData;
            Command = metaData.CreateInstance();
            this.convertValueFunc = convertValueFunc;
            SetOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ExtraPositionalArgs = new List<string>();
        }

        /// <summary>
        /// オプションを適用し、バインド済みコマンドを生成します
        /// </summary>
        /// <returns>バインド済みコマンド</returns>
        public BoundCommand Process()
        {
            foreach (var option in ParsedCommand.Options)
            {
                ValidateOptionValue(option, out var optionMeta);
                ValidateList(optionMeta);
                if (optionMeta.IsBool)
                {
                    ValidateBoolValue(option, optionMeta);
                    continue;
                }

                CheckHasOptionValue(option, optionMeta);
                var rawValue = option.RawValue;
                try
                {
                    var value = convertValueFunc(rawValue, optionMeta, option.WasQuoted);
                    AddValue(value, optionMeta);
                }
                catch (BindException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new BindException(
                        $"failed to convert value '{rawValue}' for option --{optionMeta.LongName}: {ex.Message}\n\n{MetaData.GenerateHelp()}",
                        ParsedCommand.CommandName,
                        ex);
                }
            }
            CheckRequiredOptions();

            return new BoundCommand(Command, MetaData, GetAllPositionalArgs(), ParsedCommand.Redirections);
        }

        private void ValidateOptionValue(ParsedOptionOccurrence option, out OptionMetadata optionMetaData)
        {
            if (option.IsLong)
            {
                if (!MetaData.TryGetOptionByLongName(option.Name, out optionMetaData))
                {
                    throw new BindException($"unknown option: --{option.Name}\n\n{MetaData.GenerateHelp()}",
                        ParsedCommand.CommandName);
                }
                return;
            }
            if (!MetaData.TryGetOptionByShortName(option.Name, out optionMetaData))
            {
                throw new BindException(
                    $"unknown option: -{option.Name}\n\n{MetaData.GenerateHelp()}",
                    ParsedCommand.CommandName);
            }
        }

        /// <summary>
        /// リストの重複チェック
        /// </summary>
        /// <param name="optionMetadata"></param>
        /// <exception cref="BindException"></exception>
        private void ValidateList(OptionMetadata optionMetadata)
        {
            if (!optionMetadata.IsList || !SetOptions.Contains(optionMetadata.LongName))
                return;
            throw new BindException(
                $"list option --{optionMetadata.LongName} cannot be specified multiple times\n\n{MetaData.GenerateHelp()}",
                        ParsedCommand.CommandName);
        }

        private void ValidateBoolValue(ParsedOptionOccurrence option, OptionMetadata optionMetadata)
        {
            if (option.HasValue && !option.IsValueSpaceSeparated)
            {
                // =構文で値が指定された場合はエラー
                throw new BindException(
                    $"boolean option --{optionMetadata.LongName} does not accept a value\n\n{MetaData.GenerateHelp()}",
                    ParsedCommand.CommandName);
            }

            // スペース区切りで解析された値は位置引数として扱う
            if (option.HasValue && option.IsValueSpaceSeparated)
            {
                ExtraPositionalArgs.Add(option.RawValue);
            }
            optionMetadata.SetValue(Command, true);
            SetOptions.Add(optionMetadata.LongName);
        }

        private void CheckHasOptionValue(ParsedOptionOccurrence option, OptionMetadata optionMetadata)
        {
            if (option.HasValue)
                return;

            throw new BindException(
                        $"option --{optionMetadata.LongName} requires a value\n\n{MetaData.GenerateHelp()}",
                        ParsedCommand.CommandName);
        }

        private void AddValue(object value, OptionMetadata optionMetadata)
        {
            optionMetadata.SetValue(Command, value);
            SetOptions.Add(optionMetadata.LongName);
        }

        private void CheckRequiredOptions()
        {
            foreach (var optionMeta in MetaData.Options)
            {
                if (optionMeta.IsRequired && !SetOptions.Contains(optionMeta.LongName))
                {
                    throw new BindException(
                        $"required option --{optionMeta.LongName} is missing\n\n{MetaData.GenerateHelp()}",
                        ParsedCommand.CommandName);
                }
            }
        }

        private List<string> GetAllPositionalArgs()
        {
            return ExtraPositionalArgs.Concat(ParsedCommand.PositionalArguments).ToList();
        }

    }
}
