#if UNI_TERMINAL_UNI_TASK_SUPPORT
using System.Collections.Generic;
using System.Threading;

namespace Xeon.UniTerminal.UniTask
{
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// UniTask版のコマンドインターフェース。
    /// UniTaskを使用した非同期コマンド実行をサポートします。
    /// </summary>
    public interface IUniTaskCommand
    {
        /// <summary>
        /// このコマンドを呼び出すために使用されるコマンド名。
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// ヘルプ表示用のコマンド説明。
        /// </summary>
        string Description { get; }

        /// <summary>
        /// UniTaskを使用してコマンドを非同期で実行します。
        /// </summary>
        /// <param name="context">UniTask版のコマンド実行コンテキスト。</param>
        /// <param name="ct">キャンセルトークン。</param>
        /// <returns>終了コード。</returns>
        UniTask<ExitCode> ExecuteAsync(UniTaskCommandContext context, CancellationToken ct);

        /// <summary>
        /// タブ補完用の補完候補を取得します。
        /// </summary>
        /// <param name="context">補完コンテキスト。</param>
        /// <returns>補完候補。</returns>
        IEnumerable<string> GetCompletions(CompletionContext context);
    }
}
#endif
