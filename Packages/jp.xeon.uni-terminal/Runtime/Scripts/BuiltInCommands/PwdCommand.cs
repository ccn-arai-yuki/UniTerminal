using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// 現在の作業ディレクトリを表示します。
    /// </summary>
    [Command("pwd", "Print current working directory")]
    public class PwdCommand : ICommand
    {
        /// <summary>
        /// 論理パスを表示するかどうか。
        /// </summary>
        [Option("logical", "L", Description = "Print logical path (default)")]
        public bool Logical;

        /// <summary>
        /// 物理パスを表示するかどうか。
        /// </summary>
        [Option("physical", "P", Description = "Print physical path with symlinks resolved")]
        public bool Physical;

        /// <summary>
        /// コマンド名。
        /// </summary>
        public string CommandName => "pwd";

        /// <summary>
        /// コマンドの説明。
        /// </summary>
        public string Description => "Print current working directory";

        /// <summary>
        /// コマンドを実行します。
        /// </summary>
        /// <param name="context">実行コンテキスト。</param>
        /// <param name="ct">キャンセルトークン。</param>
        /// <returns>終了コード。</returns>
        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            try
            {
                // シンボリックリンクを解決した物理パスを取得か設定されているパスそのままの論理パスを使用
                var path = PathUtility.NormalizeToSlash(Physical ? Path.GetFullPath(context.WorkingDirectory) : context.WorkingDirectory);

                // ディレクトリの存在確認
                if (!Directory.Exists(path))
                {
                    await context.Stderr.WriteLineAsync("pwd: current directory does not exist", ct);
                    return ExitCode.RuntimeError;
                }

                await context.Stdout.WriteLineAsync(path, ct);
                return ExitCode.Success;
            }
            catch (UnauthorizedAccessException)
            {
                await context.Stderr.WriteLineAsync("pwd: permission denied", ct);
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync($"pwd: {ex.Message}", ct);
            }
            return ExitCode.RuntimeError;
        }

        /// <summary>
        /// 補完候補を返します。
        /// </summary>
        /// <param name="context">補完コンテキスト。</param>
        /// <returns>補完候補。</returns>
        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // pwdは引数を取らないため補完なし
            yield break;
        }
    }
}
