# less コマンド実装計画書

## 1. 概要

`less` コマンドは、ファイルやパイプ入力の内容をページ単位で表示するページャーコマンドです。Linux/Unix の `less` コマンドに準拠した動作を目指しますが、Unity CLI環境の制約を考慮した実装とします。

### 1.1 基本仕様

- **コマンド名:** `less`
- **説明:** ファイルの内容をページ単位で表示
- **書式:** `less [オプション] [ファイル...]`

### 1.2 参考

- GNU less: https://www.gnu.org/software/less/
- POSIX more: https://pubs.opengroup.org/onlinepubs/9699919799/utilities/more.html

### 1.3 Unity CLI環境での制約

インタラクティブなターミナル操作（キー入力によるスクロール）はUnity CLI環境では制限があるため、以下のアプローチを採用します：

1. **バッチモード:** 指定行数ずつ出力し、続行確認を行う
2. **行数指定:** 一度に表示する行数をオプションで指定
3. **オフセット指定:** 開始位置を指定可能

---

## 2. オプション仕様

### 2.1 実装するオプション（Phase 1）

| オプション | 短縮形 | 型 | デフォルト | 説明 |
|-----------|--------|-----|-----------|------|
| `--lines` | `-n` | int | 20 | 一度に表示する行数 |
| `--from-line` | `-f` | int | 1 | 表示開始行（1-based） |
| `--line-numbers` | `-N` | bool | false | 行番号を表示 |
| `--quit-at-eof` | `-e` | bool | false | ファイル末尾で自動終了 |
| `--chop-long-lines` | `-S` | bool | false | 長い行を折り返さずに切り詰め |

### 2.2 将来的に実装を検討するオプション（Phase 2）

| オプション | 短縮形 | 説明 |
|-----------|--------|------|
| `--pattern` | `-p` | 指定パターンを強調表示 |
| `--ignore-case` | `-i` | 検索時に大文字小文字を無視 |
| `--raw-control-chars` | `-r` | 制御文字をそのまま表示 |
| `--squeeze-blank-lines` | `-s` | 連続する空行を1行に圧縮 |

---

## 3. 出力形式

### 3.1 基本出力

```
File: example.txt (lines 1-20 of 150)
----------------------------------------
This is line 1
This is line 2
This is line 3
...
This is line 20
----------------------------------------
[Press Enter for next page, 'q' to quit, or enter line number]
```

### 3.2 行番号付き出力（`-N` オプション）

```
File: example.txt (lines 1-20 of 150)
----------------------------------------
     1	This is line 1
     2	This is line 2
     3	This is line 3
...
    20	This is line 20
----------------------------------------
[Press Enter for next page, 'q' to quit, or enter line number]
```

### 3.3 パイプ入力時

```
(stdin) (lines 1-20)
----------------------------------------
Line 1 from pipe
Line 2 from pipe
...
----------------------------------------
[Press Enter for next page, 'q' to quit]
```

---

## 4. インタラクション

### 4.1 対話的操作

Unity CLI環境での制約を考慮し、シンプルな対話モデルを採用します。

| 入力 | 動作 |
|------|------|
| Enter | 次のページを表示 |
| `q` | 終了 |
| 数字 | 指定行へジャンプ |
| `b` | 前のページに戻る |
| `/pattern` | パターンで検索（Phase 2） |

### 4.2 非対話モード

パイプラインの中間で使用される場合は、非対話モードで動作します。

```bash
# 非対話モード（すべて出力）
cat largefile.txt | less --quit-at-eof | head -100
```

---

## 5. ファイル処理

### 5.1 入力ソース

| ソース | 説明 |
|--------|------|
| ファイル | 指定されたファイルを読み込み |
| 標準入力 | パイプからの入力を処理 |
| 複数ファイル | 順番に表示（`:n` で次、`:p` で前） |

### 5.2 大きなファイルの処理

メモリ効率のため、必要な範囲のみを読み込みます。

```csharp
// ファイルを行単位でストリーム処理
private IEnumerable<string> ReadLines(string path, int startLine, int count)
{
    using var reader = new StreamReader(path);
    int lineNum = 0;
    string line;

    while ((line = reader.ReadLine()) != null)
    {
        lineNum++;
        if (lineNum < startLine)
            continue;
        if (lineNum >= startLine + count)
            break;

        yield return line;
    }
}
```

---

## 6. パス解決

### 6.1 対応するパス形式

| パス形式 | 例 | 説明 |
|---------|-----|------|
| 相対パス | `./file.txt`, `../file.txt` | 作業ディレクトリからの相対 |
| 絶対パス | `/path/to/file.txt` | ルートからの絶対パス |
| ホームディレクトリ | `~/file.txt` | ホームディレクトリを展開 |
| 標準入力 | `-` | パイプ入力として処理 |

---

## 7. エラー処理

### 7.1 エラーケース

| エラー | メッセージ | 終了コード |
|--------|-----------|-----------|
| ファイルが存在しない | `less: {path}: No such file or directory` | RuntimeError |
| 読み取り権限なし | `less: {path}: Permission denied` | RuntimeError |
| 無効な行番号 | `less: invalid line number: {n}` | UsageError |
| ディレクトリを指定 | `less: {path}: Is a directory` | RuntimeError |

---

## 8. 実装詳細

### 8.1 クラス構造

```csharp
namespace Xeon.UniTerminal.BuiltInCommands
{
    [Command("less", "View file contents page by page")]
    public class LessCommand : ICommand
    {
        [Option("lines", "n", Description = "Number of lines to display at once")]
        public int LinesPerPage = 20;

        [Option("from-line", "f", Description = "Start from specified line")]
        public int FromLine = 1;

        [Option("line-numbers", "N", Description = "Show line numbers")]
        public bool ShowLineNumbers;

        [Option("quit-at-eof", "e", Description = "Quit at end of file")]
        public bool QuitAtEof;

        [Option("chop-long-lines", "S", Description = "Chop long lines")]
        public bool ChopLongLines;

        // 実装...
    }
}
```

### 8.2 ページ表示ロジック

```csharp
private async Task<ExitCode> DisplayPage(
    CommandContext context,
    List<string> allLines,
    int currentLine,
    string fileName,
    CancellationToken ct)
{
    int totalLines = allLines.Count;
    int endLine = Math.Min(currentLine + LinesPerPage - 1, totalLines);

    // ヘッダー
    var header = string.IsNullOrEmpty(fileName)
        ? $"(stdin) (lines {currentLine}-{endLine})"
        : $"File: {fileName} (lines {currentLine}-{endLine} of {totalLines})";

    await context.Stdout.WriteLineAsync(header, ct);
    await context.Stdout.WriteLineAsync(new string('-', 40), ct);

    // 内容
    for (int i = currentLine - 1; i < endLine; i++)
    {
        string line = allLines[i];

        if (ChopLongLines && line.Length > 80)
            line = line.Substring(0, 77) + "...";

        if (ShowLineNumbers)
            await context.Stdout.WriteLineAsync($"{i + 1,6}\t{line}", ct);
        else
            await context.Stdout.WriteLineAsync(line, ct);
    }

    await context.Stdout.WriteLineAsync(new string('-', 40), ct);

    return ExitCode.Success;
}
```

### 8.3 対話ループ

```csharp
private async Task<(bool continueLoop, int nextLine)> HandleInput(
    CommandContext context,
    int currentLine,
    int totalLines,
    CancellationToken ct)
{
    // プロンプト表示
    if (currentLine + LinesPerPage > totalLines)
    {
        await context.Stdout.WriteAsync("(END) Press 'q' to quit: ", ct);
    }
    else
    {
        await context.Stdout.WriteAsync(
            "[Enter=next, b=back, q=quit, number=goto]: ", ct);
    }

    // ユーザー入力を待機
    var input = await context.Stdin.ReadLineAsync(ct);

    if (string.IsNullOrEmpty(input))
    {
        // Enter: 次のページ
        return (true, currentLine + LinesPerPage);
    }

    input = input.Trim().ToLower();

    if (input == "q")
        return (false, 0);

    if (input == "b")
    {
        // 前のページ
        int prev = Math.Max(1, currentLine - LinesPerPage);
        return (true, prev);
    }

    if (int.TryParse(input, out int lineNum))
    {
        // 指定行へジャンプ
        lineNum = Math.Max(1, Math.Min(lineNum, totalLines));
        return (true, lineNum);
    }

    // 不明な入力は無視して継続
    return (true, currentLine);
}
```

---

## 9. テストケース

### 9.1 基本テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| LESS-001 | ファイル表示 | `less file.txt` | 最初の20行を表示 |
| LESS-002 | 行数指定 | `less -n 10 file.txt` | 10行ずつ表示 |
| LESS-003 | 開始行指定 | `less -f 50 file.txt` | 50行目から表示 |
| LESS-004 | 行番号表示 | `less -N file.txt` | 行番号付きで表示 |

### 9.2 対話テスト

| ID | テスト内容 | 操作 | 期待結果 |
|----|-----------|------|----------|
| LESS-010 | 次ページ | Enter | 次の20行を表示 |
| LESS-011 | 前ページ | `b` | 前の20行を表示 |
| LESS-012 | 行ジャンプ | `100` | 100行目から表示 |
| LESS-013 | 終了 | `q` | コマンド終了 |

### 9.3 オプションテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| LESS-020 | 長い行の切り詰め | `less -S file.txt` | 80文字で切り詰め |
| LESS-021 | EOF自動終了 | `less -e file.txt` | ファイル末尾で終了 |

### 9.4 パイプテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| LESS-030 | パイプ入力 | `cat file \| less` | stdinをページ表示 |
| LESS-031 | 複数コマンド | `find . \| less` | findの出力をページ表示 |

### 9.5 エラーテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| LESS-040 | 存在しないファイル | `less noexist` | エラーメッセージ |
| LESS-041 | ディレクトリ指定 | `less dirname` | エラーメッセージ |

---

## 10. 補完対応

### 10.1 補完ターゲット

- **位置引数:** ファイルパス補完

### 10.2 実装

```csharp
public IEnumerable<string> GetCompletions(CompletionContext context)
{
    // パス補完はCompletionEngineで処理
    yield break;
}
```

---

## 11. 代替アプローチ: head/tail との連携

lessの完全な対話機能が困難な場合、head/tailコマンドとの組み合わせで同等の機能を提供できます。

```bash
# 最初の20行
head -20 file.txt

# 50行目から20行
head -70 file.txt | tail -20

# 行番号付き
cat -n file.txt | head -20
```

---

## 12. 実装スケジュール

### Phase 1（必須機能）
1. 基本的なページ表示
2. 行数指定（`-n`）
3. 開始行指定（`-f`）
4. 行番号表示（`-N`）

### Phase 2（拡張機能）
1. パイプ入力対応
2. 対話的ナビゲーション（Enter, b, q, 行番号）
3. 長い行の切り詰め（`-S`）

### Phase 3（将来拡張）
1. パターン検索（`/pattern`）
2. 検索結果のハイライト
3. 複数ファイル切り替え
