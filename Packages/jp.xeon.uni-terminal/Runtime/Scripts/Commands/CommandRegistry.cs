using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// コマンドの検出と検索のためのレジストリ。
    /// </summary>
    public class CommandRegistry
    {
        private readonly Dictionary<string, CommandMetadata> commands;

        public IReadOnlyDictionary<string, CommandMetadata> Commands => commands;

        public CommandRegistry()
        {
            commands = new Dictionary<string, CommandMetadata>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 指定されたアセンブリからコマンドをスキャンします。
        /// </summary>
        /// <param name="assemblies">スキャンするアセンブリ。</param>
        public void ScanAssemblies(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                ScanAssembly(assembly);
            }
        }

        /// <summary>
        /// 単一のアセンブリからコマンドをスキャンします。
        /// </summary>
        public void ScanAssembly(Assembly assembly)
        {
            if (assembly == null) return;

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    TryRegisterType(type);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                // ログを出力し、ロードできた型で続行
                Debug.LogWarning($"Failed to load some types from assembly {assembly.FullName}: {ex.Message}");
                foreach (var type in ex.Types)
                {
                    if (type != null)
                    {
                        TryRegisterType(type);
                    }
                }
            }
        }

        /// <summary>
        /// コマンド型を手動で登録します。
        /// </summary>
        public void RegisterCommand<T>() where T : ICommand
        {
            TryRegisterType(typeof(T));
        }

        /// <summary>
        /// コマンド型を手動で登録します。
        /// </summary>
        public void RegisterCommand(Type type)
        {
            TryRegisterType(type);
        }

        private void TryRegisterType(Type type)
        {
            if (!typeof(ICommand).IsAssignableFrom(type)) return;
            if (type.IsInterface || type.IsAbstract) return;

            var commandAttr = type.GetCustomAttribute<CommandAttribute>();
            if (commandAttr == null) return;

            var options = ExtractOptions(type);
            var metadata = new CommandMetadata(
                commandAttr.CommandName,
                commandAttr.Description,
                type,
                options
            );

            if (commands.ContainsKey(metadata.CommandName))
            {
                Debug.LogWarning($"Duplicate command name '{metadata.CommandName}' - using last registered ({type.FullName})");
            }

            commands[metadata.CommandName] = metadata;
        }

        private List<OptionMetadata> ExtractOptions(Type type)
        {
            var options = new List<OptionMetadata>();

            // OptionAttributeを持つフィールドを取得
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var optAttr = field.GetCustomAttribute<OptionAttribute>();
                if (optAttr != null)
                {
                    options.Add(new OptionMetadata(
                        optAttr.LongName,
                        optAttr.ShortName,
                        optAttr.IsRequired,
                        optAttr.Description,
                        field.FieldType,
                        field
                    ));
                }
            }

            // OptionAttributeを持つプロパティを取得
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var optAttr = prop.GetCustomAttribute<OptionAttribute>();
                if (optAttr != null && prop.CanWrite)
                {
                    options.Add(new OptionMetadata(
                        optAttr.LongName,
                        optAttr.ShortName,
                        optAttr.IsRequired,
                        optAttr.Description,
                        prop.PropertyType,
                        prop
                    ));
                }
            }

            return options;
        }

        /// <summary>
        /// 名前でコマンドを取得しようとします。
        /// </summary>
        public bool TryGetCommand(string commandName, out CommandMetadata metadata)
        {
            return commands.TryGetValue(commandName, out metadata);
        }

        /// <summary>
        /// 登録されているすべてのコマンド名を取得します。
        /// </summary>
        public IEnumerable<string> GetCommandNames()
        {
            return commands.Keys;
        }

        /// <summary>
        /// グローバルヘルプテキストを生成します（すべてのコマンドのリスト）。
        /// </summary>
        public string GenerateGlobalHelp()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Available commands:");
            sb.AppendLine();

            var sortedNames = new List<string>(commands.Keys);
            sortedNames.Sort(StringComparer.OrdinalIgnoreCase);

            foreach (var name in sortedNames)
            {
                var meta = commands[name];
                sb.AppendLine($"  {meta.CommandName,-20} {meta.Description}");
            }

            return sb.ToString();
        }
    }
}
