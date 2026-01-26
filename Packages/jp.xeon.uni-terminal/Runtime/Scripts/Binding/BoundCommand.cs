using System.Collections.Generic;
using Xeon.UniTerminal.Parsing;

namespace Xeon.UniTerminal.Binding
{
    /// <summary>
    /// 型変換されたオプションでバインドされたコマンド
    /// </summary>
    public readonly struct BoundCommand
    {
        /// <summary>
        /// オプションが設定されたコマンドインスタンス
        /// </summary>
        public ICommand Command { get; }

        /// <summary>
        /// コマンドのメタデータ
        /// </summary>
        public CommandMetadata Metadata { get; }

        /// <summary>
        /// 位置引数
        /// </summary>
        public IReadOnlyList<string> PositionalArguments { get; }

        /// <summary>
        /// このコマンドのリダイレクション
        /// </summary>
        public ParsedRedirections Redirections { get; }

        /// <summary>
        /// バインド済みコマンドを生成します
        /// </summary>
        /// <param name="command">オプションが設定されたコマンドインスタンス</param>
        /// <param name="metadata">コマンドのメタデータ</param>
        /// <param name="positionalArguments">位置引数</param>
        /// <param name="redirections">リダイレクション情報</param>
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
