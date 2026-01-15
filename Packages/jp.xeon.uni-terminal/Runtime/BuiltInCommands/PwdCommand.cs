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
        [Option("logical", "L", Description = "Print logical path (default)")]
        public bool Logical;

        [Option("physical", "P", Description = "Print physical path with symlinks resolved")]
        public bool Physical;

        public string CommandName => "pwd";
        public string Description => "Print current working directory";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            try
            {
                string path;

                if (Physical)
                {
                    // シンボリックリンクを解決した物理パスを取得
                    path = Path.GetFullPath(context.WorkingDirectory);
                }
                else
                {
                    // 論理パス（設定されているパスをそのまま使用）
                    path = context.WorkingDirectory;
                }

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
                return ExitCode.RuntimeError;
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync($"pwd: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // pwdは引数を取らないため補完なし
            yield break;
        }
    }
}
