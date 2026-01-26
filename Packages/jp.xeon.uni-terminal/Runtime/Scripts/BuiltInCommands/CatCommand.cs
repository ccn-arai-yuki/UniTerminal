using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// ファイルの内容またはstdinを連結して表示します
    /// </summary>
    [Command("cat", "Concatenate and display file contents")]
    public class CatCommand : ICommand
    {
        /// <summary>
        /// コマンド名
        /// </summary>
        public string CommandName => "cat";

        /// <summary>
        /// コマンドの説明
        /// </summary>
        public string Description => "Concatenate and display file contents";

        /// <summary>
        /// コマンドを実行します
        /// </summary>
        /// <param name="context">実行コンテキスト</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns>終了コード</returns>
        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            // ファイルが指定されていない場合、stdinから読み取る
            if (context.PositionalArguments.Count == 0)
            {
                await foreach (var line in context.Stdin.ReadLinesAsync(ct))
                {
                    await context.Stdout.WriteLineAsync(line, ct);
                }
                return ExitCode.Success;
            }

            // 指定されたファイルを読み取る
            foreach (var path in context.PositionalArguments)
            {
                var resolvedPath = PathUtility.ResolvePath(path, context.WorkingDirectory, context.HomeDirectory);

                if (!File.Exists(resolvedPath))
                {
                    await context.Stderr.WriteLineAsync($"cat: {path}: No such file or directory", ct);
                    return ExitCode.RuntimeError;
                }

                try
                {
                    using var reader = new StreamReader(resolvedPath, System.Text.Encoding.UTF8);
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        ct.ThrowIfCancellationRequested();
                        await context.Stdout.WriteLineAsync(line, ct);
                    }
                }
                catch (System.Exception ex)
                {
                    await context.Stderr.WriteLineAsync($"cat: {path}: {ex.Message}", ct);
                    return ExitCode.RuntimeError;
                }
            }

            return ExitCode.Success;
        }

        /// <summary>
        /// 補完候補を返します
        /// </summary>
        /// <param name="context">補完コンテキスト</param>
        /// <returns>補完候補</returns>
        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // パス補完はCompletionEngineで処理される
            yield break;
        }
    }
}
