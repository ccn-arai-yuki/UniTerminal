using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// 作業ディレクトリを変更します。
    /// </summary>
    [Command("cd", "Change working directory")]
    public class CdCommand : ICommand
    {
        [Option("logical", "L", Description = "Follow symbolic links (default)")]
        public bool Logical;

        [Option("physical", "P", Description = "Use physical directory structure")]
        public bool Physical;

        public string CommandName => "cd";
        public string Description => "Change working directory";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            // 引数チェック
            if (context.PositionalArguments.Count > 1)
            {
                await context.Stderr.WriteLineAsync("cd: too many arguments", ct);
                return ExitCode.UsageError;
            }

            // ChangeWorkingDirectoryコールバックが設定されているか確認
            if (context.ChangeWorkingDirectory == null)
            {
                await context.Stderr.WriteLineAsync("cd: cannot change directory in this context", ct);
                return ExitCode.RuntimeError;
            }

            string targetPath;
            bool showPath = false;

            if (context.PositionalArguments.Count == 0)
            {
                // 引数なし: ホームディレクトリに移動
                targetPath = context.HomeDirectory;
            }
            else
            {
                var arg = context.PositionalArguments[0];

                if (arg == "-")
                {
                    // 前のディレクトリに移動
                    if (string.IsNullOrEmpty(context.PreviousWorkingDirectory))
                    {
                        await context.Stderr.WriteLineAsync("cd: OLDPWD not set", ct);
                        return ExitCode.RuntimeError;
                    }
                    targetPath = context.PreviousWorkingDirectory;
                    showPath = true;
                }
                else
                {
                    // パスを解決
                    targetPath = PathUtility.ResolvePath(
                        arg, context.WorkingDirectory, context.HomeDirectory);
                }
            }

            // 物理パスに変換（-P オプション）
            if (Physical)
            {
                try
                {
                    targetPath = Path.GetFullPath(targetPath);
                }
                catch (Exception ex)
                {
                    await context.Stderr.WriteLineAsync($"cd: {ex.Message}", ct);
                    return ExitCode.RuntimeError;
                }
            }

            // ディレクトリの存在確認
            if (!Directory.Exists(targetPath))
            {
                string displayPath = context.PositionalArguments.Count > 0
                    ? context.PositionalArguments[0]
                    : targetPath;

                if (File.Exists(targetPath))
                {
                    await context.Stderr.WriteLineAsync($"cd: {displayPath}: Not a directory", ct);
                }
                else
                {
                    await context.Stderr.WriteLineAsync($"cd: {displayPath}: No such file or directory", ct);
                }
                return ExitCode.RuntimeError;
            }

            // アクセス権限の確認（ディレクトリ内のファイル一覧を取得できるか）
            try
            {
                Directory.GetFileSystemEntries(targetPath);
            }
            catch (UnauthorizedAccessException)
            {
                string displayPath = context.PositionalArguments.Count > 0
                    ? context.PositionalArguments[0]
                    : targetPath;
                await context.Stderr.WriteLineAsync($"cd: {displayPath}: Permission denied", ct);
                return ExitCode.RuntimeError;
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync($"cd: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }

            // ディレクトリを変更
            context.ChangeWorkingDirectory(targetPath);

            // cd - の場合、移動先を表示
            if (showPath)
            {
                await context.Stdout.WriteLineAsync(targetPath, ct);
            }

            return ExitCode.Success;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // パス補完はCompletionEngineで処理（ディレクトリのみ）
            yield break;
        }
    }
}
