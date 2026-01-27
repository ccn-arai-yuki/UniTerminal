using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xeon.UniTerminal.Binding;
using Xeon.UniTerminal.BuiltInCommands;
using Xeon.UniTerminal.Completion;
using Xeon.UniTerminal.Execution;
using Xeon.UniTerminal.Parsing;
using Xeon.UniTerminal.UnityCommands;
using UnityEngine;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// ターミナルCLIのメインエントリポイント。
    /// </summary>
    public class Terminal : IDisposable
    {
        private readonly CommandRegistry registry;
        private readonly Parser parser;
        private readonly Binder binder;
        private string workingDirectory;
        private string previousWorkingDirectory;
        private readonly string homeDirectory;
        private readonly List<string> commandHistory;
        private readonly int maxHistorySize;

        /// <summary>
        /// Unityログバッファ。
        /// </summary>
        public LogBuffer LogBuffer { get; }

        /// <summary>
        /// コマンドレジストリ。
        /// </summary>
        public CommandRegistry Registry => registry;

        /// <summary>
        /// 現在の作業ディレクトリ。
        /// </summary>
        public string WorkingDirectory
        {
            get => workingDirectory;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                var normalizedValue = PathUtility.NormalizeToSlash(value);
                if (workingDirectory != normalizedValue)
                {
                    previousWorkingDirectory = workingDirectory;
                    workingDirectory = normalizedValue;
                }
            }
        }

        /// <summary>
        /// 前の作業ディレクトリ（cd - で使用）。
        /// </summary>
        public string PreviousWorkingDirectory => previousWorkingDirectory;

        /// <summary>
        /// ホームディレクトリ。
        /// </summary>
        public string HomeDirectory => homeDirectory;

        /// <summary>
        /// コマンド履歴。
        /// </summary>
        public IReadOnlyList<string> CommandHistory => commandHistory;

        /// <summary>
        /// 新しいTerminalインスタンスを作成します。
        /// </summary>
        /// <param name="homeDirectory">ホームディレクトリ（デフォルトはApplication.persistentDataPath）。</param>
        /// <param name="workingDirectory">初期作業ディレクトリ（デフォルトはホームディレクトリ）。</param>
        /// <param name="registerBuiltInCommands">組み込みコマンドを登録するかどうか。</param>
        /// <param name="maxHistorySize">履歴の最大サイズ（デフォルトは1000）。</param>
        public Terminal(string homeDirectory = null, string workingDirectory = null, bool registerBuiltInCommands = true, int maxHistorySize = 1000)
        {
            this.homeDirectory = PathUtility.NormalizeToSlash(homeDirectory ?? Application.persistentDataPath);
            this.workingDirectory = PathUtility.NormalizeToSlash(workingDirectory ?? this.homeDirectory);
            this.maxHistorySize = maxHistorySize;
            commandHistory = new List<string>();
            registry = new CommandRegistry();
            parser = new Parser();
            binder = new Binder(registry);
            LogBuffer = new LogBuffer();

            if (registerBuiltInCommands)
            {
                RegisterBuiltInCommands();
            }
        }

        /// <summary>
        /// リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            LogBuffer?.Dispose();
        }

        /// <summary>
        /// 組み込みコマンドを登録します。
        /// </summary>
        public void RegisterBuiltInCommands()
        {
            // ファイルシステムコマンド
            registry.RegisterCommand<EchoCommand>();
            registry.RegisterCommand<CatCommand>();
            registry.RegisterCommand<GrepCommand>();
            registry.RegisterCommand<HelpCommand>();
            registry.RegisterCommand<PwdCommand>();
            registry.RegisterCommand<CdCommand>();
            registry.RegisterCommand<LsCommand>();
            registry.RegisterCommand<HistoryCommand>();
            registry.RegisterCommand<FindCommand>();
            registry.RegisterCommand<LessCommand>();
            registry.RegisterCommand<DiffCommand>();
            registry.RegisterCommand<HeadCommand>();
            registry.RegisterCommand<TailCommand>();
            registry.RegisterCommand<LogCommand>();

            registry.RegisterCommand<ClearCommand>();

            // Unity固有コマンド
            registry.RegisterCommand<HierarchyCommand>();
            registry.RegisterCommand<GoCommand>();
            registry.RegisterCommand<TransformCommand>();
            registry.RegisterCommand<ComponentCommand>();
            registry.RegisterCommand<PropertyCommand>();
            registry.RegisterCommand<SceneCommand>();

            // アセット管理コマンド
            registry.RegisterCommand<AssetCommand>();
            registry.RegisterCommand<ResourcesCommand>();
#if UNITY_ADDRESSABLES
            registry.RegisterCommand<AddressableCommand>();
#endif
#if UNITY_EDITOR
            registry.RegisterCommand<AssetDbCommand>();
#endif
        }

        /// <summary>
        /// コマンドを履歴に追加します。
        /// </summary>
        /// <param name="command">追加するコマンド。</param>
        public void AddHistory(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            // 直前と同じコマンドは追加しない
            if (commandHistory.Count > 0 && commandHistory[commandHistory.Count - 1] == command)
                return;

            commandHistory.Add(command);

            // 最大サイズを超えた場合、古いものを削除
            while (commandHistory.Count > maxHistorySize)
            {
                commandHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// 履歴をクリアします。
        /// </summary>
        public void ClearHistory()
        {
            commandHistory.Clear();
        }

        /// <summary>
        /// 指定番号の履歴を削除します。
        /// </summary>
        /// <param name="index">削除する履歴のインデックス（1ベース）。</param>
        public void DeleteHistoryEntry(int index)
        {
            // 1ベースから0ベースに変換
            int actualIndex = index - 1;
            if (actualIndex >= 0 && actualIndex < commandHistory.Count)
            {
                commandHistory.RemoveAt(actualIndex);
            }
        }

        /// <summary>
        /// 指定された入力に対する補完候補を取得します。
        /// </summary>
        /// <param name="input">現在の入力行。</param>
        /// <returns>候補を含む補完結果。</returns>
        public CompletionResult GetCompletions(string input)
        {
            var engine = new CompletionEngine(registry, workingDirectory, homeDirectory);
            return engine.GetCompletions(input);
        }

        /// <summary>
        /// コマンドラインを実行します。
        /// </summary>
        /// <param name="input">コマンドライン入力。</param>
        /// <param name="stdout">標準出力ライター。</param>
        /// <param name="stderr">標準エラー出力ライター。</param>
        /// <param name="stdin">標準入力リーダー（オプション）。</param>
        /// <param name="ct">キャンセルトークン。</param>
        /// <returns>終了コード（成功の場合はSuccess）。</returns>
        public async Task<ExitCode> ExecuteAsync(
            string input,
            IAsyncTextWriter stdout,
            IAsyncTextWriter stderr,
            IAsyncTextReader stdin = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return ExitCode.Success;
            }

            // 履歴に追加
            AddHistory(input);

            try
            {
                // パース
                var parsed = parser.Parse(input);
                if (parsed.IsEmpty)
                {
                    return ExitCode.Success;
                }

                // バインド
                var bound = binder.Bind(parsed.Pipeline);

                // 実行
                var executor = new PipelineExecutor(
                    workingDirectory,
                    homeDirectory,
                    registry,
                    previousWorkingDirectory,
                    path => WorkingDirectory = path,
                    commandHistory,
                    ClearHistory,
                    DeleteHistoryEntry,
                    LogBuffer);
                var result = await executor.ExecuteAsync(bound, stdin, stdout, stderr, ct);

                return result.ExitCode;
            }
            catch (ParseException ex)
            {
                await stderr.WriteLineAsync($"Parse error: {ex.Message}", ct);
                return ExitCode.UsageError;
            }
            catch (BindException ex)
            {
                await stderr.WriteLineAsync(ex.Message, ct);
                return ExitCode.UsageError;
            }
            catch (RuntimeException ex)
            {
                await stderr.WriteLineAsync($"Runtime error: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
            catch (OperationCanceledException)
            {
                return ExitCode.RuntimeError;
            }
            catch (Exception ex)
            {
                await stderr.WriteLineAsync($"Unexpected error: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
        }
    }
}
