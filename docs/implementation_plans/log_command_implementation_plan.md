# ログコマンド実装計画書

## 概要

Unityのログを監視・出力するコマンドの実装計画書です。

**前提**: この実装は[Ctrl+Cキャンセル機能](ctrl_c_cancel_implementation_plan.md)が実装されていることを前提とします。

## 主要機能

ターミナルのインスタンスを生成した時点から、Unityのログメッセージ受け取りイベント（`Application.logMessageReceived`）にコールバックを登録し、すべてのログを取得します。

コマンド名は`log`で、原則として履歴に残っているログを出力する機能となります。

### オプション

| オプション | 説明 |
|-----------|------|
| `-i, --info` | Infoレベルログのみを出力 |
| `-w, --warn` | Warningレベルログのみを出力 |
| `-e, --error` | Errorレベルログのみを出力 |
| `-f, --follow` | ログの追加ごとに新しいログをリアルタイム出力（Ctrl+Cで終了） |
| `-t, --tail <N>` | 最新からN行出力（デフォルト: 全件） |
| `-h, --head <N>` | 最古からN行出力 |
| `-s, --stack-trace` | スタックトレースも表示する |

**注意**: `-i`, `-w`, `-e`のいずれも指定しない場合は、すべてのログレベルを表示します。

### 出力フォーマット

リッチテキストをサポートし、各レベルごとに文字色を変えます：

| レベル | 色 | プレフィックス |
|--------|-----|---------------|
| Info | 白 | `[INFO]` |
| Warning | 黄 | `[WARN]` |
| Error | 赤 | `[ERROR]` |
| Exception | 赤 | `[EXCEPTION]` |
| Assert | 赤 | `[ASSERT]` |

**出力例:**
```
[INFO] Player initialized
[WARN] Texture not found, using default
[ERROR] Failed to load asset
[EXCEPTION] NullReferenceException: Object reference not set
```

## 実装内容

### 新規作成ファイル

#### LogBuffer.cs

**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Core/LogBuffer.cs`

Unityログを蓄積するバッファクラス。既存の`CircularBuffer<T>`を使用して効率的なログ管理を行います。

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xeon.Common.FlyweightScrollView.Model;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// Unityログのエントリ
    /// </summary>
    public readonly struct LogEntry
    {
        public readonly string Message;
        public readonly string StackTrace;
        public readonly LogType Type;
        public readonly DateTime Timestamp;

        public LogEntry(string message, string stackTrace, LogType type)
        {
            Message = message;
            StackTrace = stackTrace;
            Type = type;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Unityログを蓄積するバッファ
    /// CircularBufferを使用して固定サイズで効率的に管理
    /// </summary>
    public class LogBuffer : IDisposable
    {
        private readonly CircularBuffer<LogEntry> entries;
        private readonly object lockObject = new object();

        public event Action<LogEntry> OnLogReceived;

        /// <summary>
        /// 現在のログ件数
        /// </summary>
        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return entries.Count;
                }
            }
        }

        /// <summary>
        /// バッファの最大容量
        /// </summary>
        public int Capacity => entries.Capacity;

        public LogBuffer(int capacity = 10000)
        {
            entries = new CircularBuffer<LogEntry>(capacity);
            Application.logMessageReceived += HandleLogMessage;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= HandleLogMessage;
        }

        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            var entry = new LogEntry(condition, stackTrace, type);

            lock (lockObject)
            {
                // CircularBufferは自動的に古いエントリを上書き
                entries.Add(entry, isNotify: false);
            }

            OnLogReceived?.Invoke(entry);
        }

        /// <summary>
        /// 蓄積されたログのコピーを取得
        /// </summary>
        public List<LogEntry> GetEntries()
        {
            lock (lockObject)
            {
                return entries.ToList();
            }
        }

        /// <summary>
        /// 蓄積されたログをクリア
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                entries.Clear(isNotify: false);
            }
        }
    }
}
```

#### LogCommand.cs

**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/BuiltInCommands/LogCommand.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// Unityログを表示するコマンド
    /// </summary>
    [Command("log", "Display Unity log messages")]
    public class LogCommand : ICommand
    {
        [Option("info", "i", Description = "Show only Info level logs")]
        public bool Info;

        [Option("warn", "w", Description = "Show only Warning level logs")]
        public bool Warn;

        [Option("error", "e", Description = "Show only Error level logs")]
        public bool Error;

        [Option("follow", "f", Description = "Output appended logs in real-time (Ctrl+C to stop)")]
        public bool Follow;

        [Option("tail", "t", Description = "Output the last N log entries")]
        public int? Tail;

        [Option("head", "h", Description = "Output the first N log entries")]
        public int? Head;

        [Option("stack-trace", "s", Description = "Show stack traces")]
        public bool StackTrace;

        public string CommandName => "log";
        public string Description => "Display Unity log messages";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            // LogBufferが設定されていない場合
            if (context.LogBuffer == null)
            {
                await context.Stderr.WriteLineAsync("log: LogBuffer is not available", ct);
                return ExitCode.RuntimeError;
            }

            // --head と --tail は排他
            if (Head.HasValue && Tail.HasValue)
            {
                await context.Stderr.WriteLineAsync("log: cannot specify both --head and --tail", ct);
                return ExitCode.UsageError;
            }

            // フィルタ条件を取得
            var filter = GetLogTypeFilter();

            // 履歴ログを出力
            var entries = context.LogBuffer.GetEntries();
            var filteredEntries = FilterEntries(entries, filter);
            var outputEntries = ApplyHeadTail(filteredEntries);

            foreach (var entry in outputEntries)
            {
                await OutputLogEntryAsync(context, entry, ct);
            }

            // -f オプション: リアルタイム監視
            if (Follow)
                return await FollowLogsAsync(context, filter, ct);

            return ExitCode.Success;
        }

        private HashSet<LogType> GetLogTypeFilter()
        {
            var filter = new HashSet<LogType>();

            // フィルタ指定がない場合は全て表示
            if (!Info && !Warn && !Error)
            {
                filter.Add(LogType.Log);
                filter.Add(LogType.Warning);
                filter.Add(LogType.Error);
                filter.Add(LogType.Exception);
                filter.Add(LogType.Assert);
                return filter;
            }

            if (Info)
                filter.Add(LogType.Log);

            if (Warn)
                filter.Add(LogType.Warning);

            if (Error)
            {
                filter.Add(LogType.Error);
                filter.Add(LogType.Exception);
                filter.Add(LogType.Assert);
            }

            return filter;
        }

        private IEnumerable<LogEntry> FilterEntries(List<LogEntry> entries, HashSet<LogType> filter)
        {
            return entries.Where(e => filter.Contains(e.Type));
        }

        private IEnumerable<LogEntry> ApplyHeadTail(IEnumerable<LogEntry> entries)
        {
            if (Head.HasValue)
                return entries.Take(Head.Value);

            if (Tail.HasValue)
                return entries.TakeLast(Tail.Value);

            return entries;
        }

        private async Task OutputLogEntryAsync(
            CommandContext context,
            LogEntry entry,
            CancellationToken ct)
        {
            var formattedMessage = FormatLogEntry(entry);
            await context.Stdout.WriteLineAsync(formattedMessage, ct);

            if (StackTrace && !string.IsNullOrEmpty(entry.StackTrace))
            {
                var stackTraceLines = entry.StackTrace.TrimEnd().Split('\n');
                foreach (var line in stackTraceLines)
                {
                    await context.Stdout.WriteLineAsync($"    {line}", ct);
                }
            }
        }

        private string FormatLogEntry(LogEntry entry)
        {
            var (prefix, color) = GetPrefixAndColor(entry.Type);
            return $"<color={color}>{prefix} {entry.Message}</color>";
        }

        private (string prefix, string color) GetPrefixAndColor(LogType type)
        {
            return type switch
            {
                LogType.Log => ("[INFO]", "white"),
                LogType.Warning => ("[WARN]", "yellow"),
                LogType.Error => ("[ERROR]", "red"),
                LogType.Exception => ("[EXCEPTION]", "red"),
                LogType.Assert => ("[ASSERT]", "red"),
                _ => ("[INFO]", "white")
            };
        }

        /// <summary>
        /// リアルタイムでログを監視
        /// Ctrl+CでCancellationTokenがキャンセルされると終了
        /// </summary>
        private async Task<ExitCode> FollowLogsAsync(
            CommandContext context,
            HashSet<LogType> filter,
            CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();

            void OnLogReceived(LogEntry entry)
            {
                if (!filter.Contains(entry.Type))
                    return;

                try
                {
                    // 同期的に出力（イベントハンドラ内なので）
                    var formattedMessage = FormatLogEntry(entry);
                    context.Stdout.WriteLineAsync(formattedMessage, ct).Wait(ct);

                    if (StackTrace && !string.IsNullOrEmpty(entry.StackTrace))
                    {
                        var stackTraceLines = entry.StackTrace.TrimEnd().Split('\n');
                        foreach (var line in stackTraceLines)
                        {
                            context.Stdout.WriteLineAsync($"    {line}", ct).Wait(ct);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetResult(true);
                }
            }

            context.LogBuffer.OnLogReceived += OnLogReceived;

            try
            {
                // Ctrl+C（CancellationToken）でキャンセルされるまで待機
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                // Ctrl+Cによる正常終了
            }
            finally
            {
                context.LogBuffer.OnLogReceived -= OnLogReceived;
            }

            return ExitCode.Success;
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            return Enumerable.Empty<string>();
        }
    }
}
```

### 修正ファイル

#### CommandContext.cs

**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Core/CommandContext.cs`

**変更内容**: `LogBuffer`プロパティを追加

```csharp
// 追加するプロパティ
public LogBuffer LogBuffer { get; }

// コンストラクタに追加
public CommandContext(
    // ... 既存のパラメータ ...
    LogBuffer logBuffer = null)
{
    // ... 既存の初期化 ...
    LogBuffer = logBuffer;
}
```

#### Terminal.cs

**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Terminal.cs`

**変更内容**:
1. `LogBuffer`フィールドを追加
2. コンストラクタで初期化
3. `Dispose()`で破棄
4. `RegisterBuiltInCommands()`に`LogCommand`を追加
5. `PipelineExecutor`に`LogBuffer`を渡す

```csharp
// フィールド追加
public LogBuffer LogBuffer { get; }

// コンストラクタ
public Terminal()
{
    LogBuffer = new LogBuffer();
    // ...
}

// Dispose
public void Dispose()
{
    LogBuffer?.Dispose();
    // ...
}

// RegisterBuiltInCommands
private void RegisterBuiltInCommands()
{
    // 既存のコマンド...
    Register<LogCommand>();
}
```

#### PipelineExecutor.cs

**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Execution/PipelineExecutor.cs`

**変更内容**: `CommandContext`生成時に`LogBuffer`を渡す

```csharp
var context = new CommandContext(
    // ... 既存のパラメータ ...
    logBuffer: terminal.LogBuffer
);
```

## 技術詳細

### ログバッファの設計

```csharp
Application.logMessageReceived += HandleLogMessage;
```

- `Application.logMessageReceived`イベントでUnityの全ログを取得
- `CircularBuffer<LogEntry>`を使用して固定サイズで効率的に管理
- 容量（デフォルト10000）を超えると自動的に古いエントリを上書き
- スレッドセーフ（`lock`で保護）
- `OnLogReceived`イベントで`-f`オプション用のリアルタイム通知

**CircularBufferを使用する利点:**
- 固定サイズのため、メモリ使用量が予測可能
- 古いエントリの削除が O(1) で効率的
- 既存のプロジェクトコードを再利用

### リッチテキストフォーマット

```csharp
$"<color={color}>{prefix} {entry.Message}</color>"
```

TextMeshProとUnity UIの両方でサポートされる形式を使用。

**出力例:**
```
<color=white>[INFO] Player initialized</color>
<color=yellow>[WARN] Texture not found</color>
<color=red>[ERROR] Failed to load asset</color>
<color=red>[EXCEPTION] NullReferenceException: Object reference not set</color>
<color=red>[ASSERT] Assertion failed</color>
```

### フィルタ動作

- **フィルタ未指定**: 全レベルを表示（Info, Warning, Error, Exception, Assert）
- `-i`: Infoのみ
- `-w`: Warningのみ
- `-e`: Error, Exception, Assertを表示
- 複数指定可能: `-i -w`でInfoとWarningを表示

### -fオプションの終了フロー

```
ユーザーがCtrl+Cを押す
    ↓
UniTerminal.Update()でIsPressedCtrlC()を検出
    ↓
commandCancellationTokenSource.Cancel()
    ↓
Task.Delay(Timeout.Infinite, ct)がOperationCanceledExceptionをスロー
    ↓
FollowLogsAsync()が正常終了（ExitCode.Success）
    ↓
UniTerminal.OnInputCommand()で"^C"を表示
```

## 実装順序

**前提**: [Ctrl+Cキャンセル機能](ctrl_c_cancel_implementation_plan.md)を先に実装

1. `LogBuffer.cs`を新規作成
2. `CommandContext.cs`に`LogBuffer`プロパティを追加
3. `Terminal.cs`で`LogBuffer`を初期化・破棄
4. `PipelineExecutor.cs`で`LogBuffer`を渡す
5. `LogCommand.cs`を新規作成
6. `Terminal.cs`でコマンドを登録
7. 動作確認

## 検証方法

### 基本機能テスト

```bash
# 全ログを表示
log

# 最新10件を表示
log -t 10

# 最古10件を表示
log -h 10

# Infoのみ
log -i

# Warningのみ
log -w

# Errorのみ
log -e

# InfoとWarning
log -i -w

# スタックトレースも表示
log -s

# 最新のエラー5件をスタックトレース付きで表示
log -e -t 5 -s

# リアルタイム監視
log -f

# リアルタイム監視（Errorのみ）
log -f -e
```

### エッジケーステスト

1. **ログが空の場合**: 何も出力せず正常終了
2. **-h と -t の同時指定**: エラー
3. **-f 中にCtrl+C**: 正常終了、"^C"表示
4. **LogBufferが未設定**: エラーメッセージ
5. **大量のログ**: CircularBufferの容量超過時に古いログが自動的に上書き

## ファイル一覧

### 新規作成
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Core/LogBuffer.cs`
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/BuiltInCommands/LogCommand.cs`

### 修正
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Core/CommandContext.cs`
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Terminal.cs`
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Execution/PipelineExecutor.cs`

## 依存関係

- [Ctrl+Cキャンセル機能](ctrl_c_cancel_implementation_plan.md) - 必須（-fオプション用）

## CLAUDE.md準拠

- **ネスト制限**: 最大3レベル（namespace除く）
- **switch式**: 使用（switch文ではないため許可）
- **可読性優先**: メソッドを分割し、意図が明確な命名
- **制御文スタイル**: 2行形式を使用
