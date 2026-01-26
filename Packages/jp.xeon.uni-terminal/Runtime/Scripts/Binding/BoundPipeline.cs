using System.Collections.Generic;

namespace Xeon.UniTerminal.Binding
{
    /// <summary>
    /// 実行準備が完了したバインドされたパイプライン。
    /// </summary>
    public readonly struct BoundPipeline
    {
        /// <summary>
        /// 順序通りのバインドされたコマンド。
        /// </summary>
        public IReadOnlyList<BoundCommand> Commands { get; }

        /// <summary>
        /// バインド済みコマンドの一覧からパイプラインを作成します。
        /// </summary>
        /// <param name="commands">順序通りのバインド済みコマンド。</param>
        public BoundPipeline(List<BoundCommand> commands)
        {
            Commands = commands;
        }
    }
}
