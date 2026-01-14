# history コマンド実装計画書

## 1. 概要

`history` コマンドは、ターミナルで実行されたコマンドの履歴を表示・管理するコマンドです。bashの `history` ビルトインに準拠した動作を目指します。

### 1.1 基本仕様

- **コマンド名:** `history`
- **説明:** コマンド履歴を表示・管理
- **書式:** `history [オプション] [件数]`

### 1.2 参考

- Bash history: https://www.gnu.org/software/bash/manual/html_node/Bash-History-Builtins.html

---

## 2. オプション仕様

### 2.1 実装するオプション（Phase 1）

| オプション | 短縮形 | 型 | デフォルト | 説明 |
|-----------|--------|-----|-----------|------|
| `--clear` | `-c` | bool | false | 履歴をすべてクリア |
| `--delete` | `-d` | int | -1 | 指定番号のエントリを削除 |
| `--reverse` | `-r` | bool | false | 逆順で表示（新しい順） |

### 2.2 将来的に実装を検討するオプション（Phase 2）

| オプション | 短縮形 | 説明 |
|-----------|--------|------|
| `--write` | `-w` | 履歴をファイルに書き出し |
| `--read` | `-r` | ファイルから履歴を読み込み |
| `--append` | `-a` | 現在のセッションの履歴をファイルに追記 |
| `--search` | `-s` | パターンで履歴を検索 |

---

## 3. 出力形式

### 3.1 デフォルト出力

履歴番号とコマンドを表示します。

```
    1  echo hello
    2  ls -la
    3  cd ~/projects
    4  cat file.txt
    5  grep pattern file.txt
```

### 3.2 件数指定

直近N件の履歴を表示します。

```bash
# 直近5件を表示
history 5
```

```
   96  cd ..
   97  ls
   98  cat README.md
   99  git status
  100  history 5
```

### 3.3 逆順表示（`-r` オプション）

```
  100  history -r
   99  git status
   98  cat README.md
   97  ls
   96  cd ..
```

---

## 4. 履歴管理

### 4.1 Terminal クラスでの履歴保持

```csharp
public class Terminal
{
    private readonly List<string> commandHistory = new List<string>();
    private const int MaxHistorySize = 1000;

    public IReadOnlyList<string> CommandHistory => commandHistory;

    public void AddToHistory(string command)
    {
        // 空コマンドは追加しない
        if (string.IsNullOrWhiteSpace(command))
            return;

        // 直前と同じコマンドは追加しない（オプション）
        if (commandHistory.Count > 0 &&
            commandHistory[commandHistory.Count - 1] == command)
            return;

        commandHistory.Add(command);

        // 最大サイズを超えたら古いものを削除
        while (commandHistory.Count > MaxHistorySize)
            commandHistory.RemoveAt(0);
    }
}
```

### 4.2 履歴の永続化（Phase 2）

```csharp
// 保存先: ~/.uniterminal_history
private string GetHistoryFilePath()
{
    return Path.Combine(homeDirectory, ".uniterminal_history");
}

public void SaveHistory()
{
    var path = GetHistoryFilePath();
    File.WriteAllLines(path, commandHistory);
}

public void LoadHistory()
{
    var path = GetHistoryFilePath();
    if (File.Exists(path))
    {
        var lines = File.ReadAllLines(path);
        commandHistory.AddRange(lines.Take(MaxHistorySize));
    }
}
```

---

## 5. CommandContext の拡張

履歴にアクセスするため、CommandContext に履歴参照を追加します。

```csharp
public class CommandContext
{
    // 既存のプロパティ...

    /// <summary>
    /// コマンド履歴への参照
    /// </summary>
    public IReadOnlyList<string> CommandHistory { get; }

    /// <summary>
    /// 履歴をクリアするコールバック
    /// </summary>
    public Action ClearHistory { get; }

    /// <summary>
    /// 指定番号の履歴を削除するコールバック
    /// </summary>
    public Action<int> DeleteHistoryEntry { get; }
}
```

---

## 6. エラー処理

### 6.1 エラーケース

| エラー | メッセージ | 終了コード |
|--------|-----------|-----------|
| 無効な履歴番号 | `history: {n}: history position out of range` | UsageError |
| 無効な件数 | `history: {n}: invalid number` | UsageError |

---

## 7. 実装詳細

### 7.1 クラス構造

```csharp
namespace Xeon.UniTerminal.BuiltInCommands
{
    [Command("history", "Display command history")]
    public class HistoryCommand : ICommand
    {
        [Option("clear", "c", Description = "Clear the history list")]
        public bool Clear;

        [Option("delete", "d", Description = "Delete history entry at offset")]
        public int DeleteOffset = -1;

        [Option("reverse", "r", Description = "Display in reverse order")]
        public bool Reverse;

        public string CommandName => "history";
        public string Description => "Display command history";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            // クリア処理
            if (Clear)
            {
                context.ClearHistory?.Invoke();
                return ExitCode.Success;
            }

            // 削除処理
            if (DeleteOffset >= 0)
            {
                if (DeleteOffset >= context.CommandHistory.Count)
                {
                    await context.Stderr.WriteLineAsync(
                        $"history: {DeleteOffset}: history position out of range", ct);
                    return ExitCode.UsageError;
                }
                context.DeleteHistoryEntry?.Invoke(DeleteOffset);
                return ExitCode.Success;
            }

            // 表示処理
            return await DisplayHistory(context, ct);
        }

        private async Task<ExitCode> DisplayHistory(CommandContext context, CancellationToken ct)
        {
            var history = context.CommandHistory;
            int count = history.Count;

            // 件数指定がある場合（位置引数から取得）
            if (context.PositionalArguments.Count > 0)
            {
                if (!int.TryParse(context.PositionalArguments[0], out int n) || n < 0)
                {
                    await context.Stderr.WriteLineAsync(
                        $"history: {context.PositionalArguments[0]}: invalid number", ct);
                    return ExitCode.UsageError;
                }
                count = Math.Min(n, history.Count);
            }

            int startIndex = history.Count - count;
            var indices = Enumerable.Range(startIndex, count);

            if (Reverse)
                indices = indices.Reverse();

            foreach (int i in indices)
            {
                // 1-based の履歴番号で表示
                await context.Stdout.WriteLineAsync(
                    $"{i + 1,5}  {history[i]}", ct);
            }

            return ExitCode.Success;
        }
    }
}
```

---

## 8. テストケース

### 8.1 基本テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| HIST-001 | 全履歴表示 | `history` | 全履歴を番号付きで表示 |
| HIST-002 | 件数指定 | `history 5` | 直近5件を表示 |
| HIST-003 | 逆順表示 | `history -r` | 新しい順で表示 |

### 8.2 管理操作テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| HIST-010 | 履歴クリア | `history -c` | 履歴が空になる |
| HIST-011 | エントリ削除 | `history -d 5` | 5番目のエントリが削除 |
| HIST-012 | 無効な番号で削除 | `history -d 9999` | エラーメッセージ |

### 8.3 エッジケーステスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| HIST-020 | 空の履歴 | `history` (初回) | 何も表示しない |
| HIST-021 | 履歴超過件数 | `history 10000` | 全履歴を表示 |
| HIST-022 | 負の件数 | `history -5` | エラーメッセージ |

### 8.4 パイプテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| HIST-030 | grep連携 | `history \| grep cd` | cdを含む履歴のみ |
| HIST-031 | wc連携 | `history \| wc -l` | 履歴の行数 |

---

## 9. 補完対応

### 9.1 補完ターゲット

- **位置引数:** 数値（補完なし）

### 9.2 実装

```csharp
public IEnumerable<string> GetCompletions(CompletionContext context)
{
    // 特に補完なし
    yield break;
}
```

---

## 10. 将来の拡張: 履歴展開

### 10.1 履歴展開構文（Phase 2以降）

| 構文 | 説明 |
|------|------|
| `!!` | 直前のコマンド |
| `!n` | n番目のコマンド |
| `!-n` | n個前のコマンド |
| `!string` | stringで始まる最新のコマンド |
| `!?string` | stringを含む最新のコマンド |

### 10.2 実装例

```csharp
// Terminal.ExecuteAsync 内で履歴展開を処理
private string ExpandHistory(string input)
{
    if (input.StartsWith("!!"))
    {
        if (commandHistory.Count == 0)
            throw new InvalidOperationException("No previous command");
        return commandHistory[commandHistory.Count - 1] + input.Substring(2);
    }
    // ... その他のパターン
}
```

---

## 11. 実装スケジュール

### Phase 1（必須機能）
1. CommandContext への履歴参照追加
2. 履歴表示（件数指定、逆順）
3. 履歴クリア、エントリ削除

### Phase 2（拡張機能）
1. 履歴の永続化（ファイル保存/読み込み）
2. 履歴検索（`--search`）
3. 重複排除オプション

### Phase 3（将来拡張）
1. 履歴展開（`!!`, `!n` など）
2. インクリメンタル検索（Ctrl+R相当）
3. タイムスタンプ付き履歴
