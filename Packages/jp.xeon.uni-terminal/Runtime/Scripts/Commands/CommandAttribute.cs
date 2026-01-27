using System;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// クラスをコマンドとしてマークするための属性
    /// クラスはICommandインターフェースを実装する必要があります
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        /// このコマンドを呼び出すために使用されるコマンド名
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// ヘルプ表示用のコマンド説明
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 新しいCommandAttributeを作成します
        /// </summary>
        /// <param name="commandName">このコマンドを呼び出すために使用される名前</param>
        /// <param name="description">ヘルプ表示用の説明</param>
        public CommandAttribute(string commandName, string description = "")
        {
            CommandName = commandName ?? throw new ArgumentNullException(nameof(commandName));
            Description = description ?? "";
        }
    }
}
