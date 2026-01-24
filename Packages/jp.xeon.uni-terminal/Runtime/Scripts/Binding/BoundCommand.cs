using System.Collections.Generic;
using Xeon.UniTerminal.Parsing;

namespace Xeon.UniTerminal.Binding
{
    /// <summary>
    /// 型変換されたオプションでバインドされたコマンド。
    /// </summary>
    public readonly struct BoundCommand
    {
        /// <summary>
        /// オプションが設定されたコマンドインスタンス。
        /// </summary>
        public ICommand Command { get; }

        /// <summary>
        /// コマンドのメタデータ。
        /// </summary>
        public CommandMetadata Metadata { get; }

        /// <summary>
        /// 位置引数。
        /// </summary>
        public IReadOnlyList<string> PositionalArguments { get; }

        /// <summary>
        /// このコマンドのリダイレクション。
        /// </summary>
        public ParsedRedirections Redirections { get; }

        public BoundCommand(
            ICommand command,
            CommandMetadata metadata,
            IReadOnlyList<string> positionalArguments,
            ParsedRedirections redirections)
        {
            Command = command;
            Metadata = metadata;
            PositionalArguments = positionalArguments;
            Redirections = redirections;
        }
    }
}
