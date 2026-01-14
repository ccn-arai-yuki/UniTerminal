using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// コマンド実装用インターフェース。
    /// すべてのコマンドはこのインターフェースを実装し、CommandAttributeで装飾される必要があります。
    /// </summary>
    public interface ICommand
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
        /// コマンドを非同期で実行します。
        /// </summary>
        /// <param name="context">stdin/stdout/stderrと引数を含むコマンド実行コンテキスト。</param>
        /// <param name="ct">非同期キャンセル用のキャンセルトークン。</param>
        /// <returns>終了コード（成功の場合はSuccess、失敗の場合はSuccess以外）。</returns>
        Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct);

        /// <summary>
        /// タブ補完用の補完候補を取得します。
        /// 補完候補がない場合は空のenumerableを返します。
        /// </summary>
        /// <param name="context">現在の入力状態を持つ補完コンテキスト。</param>
        /// <returns>補完候補。</returns>
        IEnumerable<string> GetCompletions(CompletionContext context);
    }
}
