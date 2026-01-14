# diff コマンド実装計画書

## 1. 概要

`diff` コマンドは、2つのファイルまたはディレクトリの差分を比較・表示するコマンドです。Linux/Unix の `diff` コマンドに準拠した動作を目指します。

### 1.1 基本仕様

- **コマンド名:** `diff`
- **説明:** 2つのファイルを比較して差分を表示
- **書式:** `diff [オプション] ファイル1 ファイル2`

### 1.2 参考

- GNU diffutils: https://www.gnu.org/software/diffutils/manual/diffutils.html
- POSIX diff: https://pubs.opengroup.org/onlinepubs/9699919799/utilities/diff.html

---

## 2. オプション仕様

### 2.1 実装するオプション（Phase 1）

| オプション | 短縮形 | 型 | デフォルト | 説明 |
|-----------|--------|-----|-----------|------|
| `--unified` | `-u` | int | 3 | unified形式で出力（コンテキスト行数指定） |
| `--context` | `-c` | int | 3 | context形式で出力（コンテキスト行数指定） |
| `--ignore-case` | `-i` | bool | false | 大文字小文字を無視 |
| `--ignore-space` | `-b` | bool | false | 空白の変更を無視 |
| `--ignore-all-space` | `-w` | bool | false | すべての空白を無視 |
| `--brief` | `-q` | bool | false | ファイルが異なるかどうかのみ報告 |

### 2.2 将来的に実装を検討するオプション（Phase 2）

| オプション | 短縮形 | 説明 |
|-----------|--------|------|
| `--recursive` | `-r` | ディレクトリを再帰的に比較 |
| `--side-by-side` | `-y` | 横並びで表示 |
| `--suppress-common-lines` | | 共通行を表示しない |
| `--color` | | 色付き出力 |
| `--exclude` | `-x` | パターンにマッチするファイルを除外 |

---

## 3. 出力形式

### 3.1 デフォルト出力（Normal形式）

行番号と変更タイプを表示します。

```
2c2
< 旧行
---
> 新行
5,7d4
< 削除行1
< 削除行2
< 削除行3
10a12,13
> 追加行1
> 追加行2
```

**変更タイプ:**
- `a` (add): 追加
- `d` (delete): 削除
- `c` (change): 変更

### 3.2 Unified形式（`-u` オプション）

git diffと同様の形式です。

```diff
--- file1.txt	2024-01-15 10:00:00
+++ file2.txt	2024-01-15 11:00:00
@@ -1,5 +1,6 @@
 共通行1
-削除された行
+追加された行
+もう一つの追加行
 共通行2
 共通行3
```

### 3.3 Context形式（`-c` オプション）

```
*** file1.txt	2024-01-15 10:00:00
--- file2.txt	2024-01-15 11:00:00
***************
*** 1,5 ****
  共通行1
! 変更前の行
  共通行2
--- 1,6 ----
  共通行1
! 変更後の行
+ 追加行
  共通行2
```

### 3.4 Brief出力（`-q` オプション）

```
Files file1.txt and file2.txt differ
```

または差分がない場合は何も出力しません。

---

## 4. 差分アルゴリズム

### 4.1 LCS（最長共通部分列）アルゴリズム

差分検出にはLCS（Longest Common Subsequence）アルゴリズムを使用します。

```csharp
// 動的計画法によるLCS計算
int[,] lcs = new int[m + 1, n + 1];
for (int i = 1; i <= m; i++)
{
    for (int j = 1; j <= n; j++)
    {
        if (lines1[i - 1] == lines2[j - 1])
            lcs[i, j] = lcs[i - 1, j - 1] + 1;
        else
            lcs[i, j] = Math.Max(lcs[i - 1, j], lcs[i, j - 1]);
    }
}
```

### 4.2 差分の種類

| 種類 | 説明 | 表示 |
|------|------|------|
| 追加 | file2にのみ存在 | `>` または `+` |
| 削除 | file1にのみ存在 | `<` または `-` |
| 変更 | 両方に存在するが内容が異なる | `!` または `c` |
| 共通 | 両方に同じ内容で存在 | ` ` |

---

## 5. 比較オプション

### 5.1 空白処理

| オプション | 動作 |
|-----------|------|
| デフォルト | 空白も含めて完全一致を比較 |
| `-b` | 連続する空白を1つとして扱う |
| `-w` | すべての空白を無視して比較 |

### 5.2 大文字小文字

| オプション | 動作 |
|-----------|------|
| デフォルト | 大文字小文字を区別 |
| `-i` | 大文字小文字を同一視 |

---

## 6. パス解決

### 6.1 対応するパス形式

| パス形式 | 例 | 説明 |
|---------|-----|------|
| 相対パス | `./file.txt`, `../file.txt` | 作業ディレクトリからの相対 |
| 絶対パス | `/path/to/file.txt` | ルートからの絶対パス |
| ホームディレクトリ | `~/file.txt` | ホームディレクトリを展開 |

### 6.2 特殊入力

| 入力 | 説明 |
|------|------|
| `-` | 標準入力から読み取り（片方のファイルのみ） |

---

## 7. エラー処理

### 7.1 エラーケース

| エラー | メッセージ | 終了コード |
|--------|-----------|-----------|
| ファイルが存在しない | `diff: {path}: No such file or directory` | RuntimeError |
| ディレクトリを指定 | `diff: {path}: Is a directory` | RuntimeError |
| 引数不足 | `diff: missing operand` | UsageError |
| 読み取り権限なし | `diff: {path}: Permission denied` | RuntimeError |

### 7.2 終了コード

| 終了コード | 意味 |
|-----------|------|
| Success (0) | 差分なし |
| Difference (1) | 差分あり |
| RuntimeError (2) | エラー発生 |

---

## 8. 実装詳細

### 8.1 クラス構造

```csharp
namespace Xeon.UniTerminal.BuiltInCommands
{
    [Command("diff", "Compare files line by line")]
    public class DiffCommand : ICommand
    {
        [Option("unified", "u", Description = "Output in unified format")]
        public int UnifiedContext = -1;  // -1 = 未指定

        [Option("context", "c", Description = "Output in context format")]
        public int ContextLines = -1;  // -1 = 未指定

        [Option("ignore-case", "i", Description = "Ignore case differences")]
        public bool IgnoreCase;

        [Option("ignore-space", "b", Description = "Ignore changes in whitespace")]
        public bool IgnoreSpaceChange;

        [Option("ignore-all-space", "w", Description = "Ignore all whitespace")]
        public bool IgnoreAllSpace;

        [Option("brief", "q", Description = "Report only when files differ")]
        public bool Brief;

        // 実装...
    }
}
```

### 8.2 差分計算

```csharp
public class DiffResult
{
    public List<DiffHunk> Hunks { get; }
    public bool HasDifferences => Hunks.Count > 0;
}

public class DiffHunk
{
    public int OldStart { get; }
    public int OldCount { get; }
    public int NewStart { get; }
    public int NewCount { get; }
    public List<DiffLine> Lines { get; }
}

public class DiffLine
{
    public DiffType Type { get; }  // Add, Delete, Context
    public string Content { get; }
}
```

### 8.3 行の正規化

```csharp
private string NormalizeLine(string line)
{
    if (IgnoreAllSpace)
        return Regex.Replace(line, @"\s+", "");
    if (IgnoreSpaceChange)
        return Regex.Replace(line, @"\s+", " ").Trim();
    return line;
}

private bool LinesEqual(string line1, string line2)
{
    var a = NormalizeLine(line1);
    var b = NormalizeLine(line2);
    return IgnoreCase
        ? string.Equals(a, b, StringComparison.OrdinalIgnoreCase)
        : string.Equals(a, b);
}
```

---

## 9. テストケース

### 9.1 基本テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| DIFF-001 | 同一ファイル | `diff file file` | 出力なし、Success |
| DIFF-002 | 異なるファイル | `diff file1 file2` | 差分を表示、Difference |
| DIFF-003 | 行追加検出 | 追加された行があるファイル | 追加行を表示 |
| DIFF-004 | 行削除検出 | 削除された行があるファイル | 削除行を表示 |
| DIFF-005 | 行変更検出 | 変更された行があるファイル | 変更を表示 |

### 9.2 出力形式テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| DIFF-010 | Unified形式 | `diff -u file1 file2` | unified形式で出力 |
| DIFF-011 | Context形式 | `diff -c file1 file2` | context形式で出力 |
| DIFF-012 | Brief形式 | `diff -q file1 file2` | "Files differ" のみ |
| DIFF-013 | コンテキスト行数 | `diff -u 5 file1 file2` | 前後5行のコンテキスト |

### 9.3 比較オプションテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| DIFF-020 | 大文字小文字無視 | `diff -i file1 file2` | Case違いは同一視 |
| DIFF-021 | 空白変更無視 | `diff -b file1 file2` | 空白数の違いは無視 |
| DIFF-022 | 全空白無視 | `diff -w file1 file2` | 空白を完全無視 |

### 9.4 エラーテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| DIFF-030 | 存在しないファイル | `diff noexist file` | エラーメッセージ |
| DIFF-031 | 引数不足 | `diff file` | UsageError |
| DIFF-032 | ディレクトリ指定 | `diff dir file` | エラーメッセージ |

### 9.5 パイプテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| DIFF-040 | 標準入力との比較 | `cat file1 \| diff - file2` | stdinとfile2を比較 |

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

## 11. 実装スケジュール

### Phase 1（必須機能）
1. 基本的な差分検出（LCSアルゴリズム）
2. Normal形式出力
3. Unified形式出力（`-u`）
4. Brief出力（`-q`）

### Phase 2（拡張機能）
1. Context形式出力（`-c`）
2. 空白処理オプション（`-b`, `-w`）
3. 大文字小文字無視（`-i`）
4. 標準入力対応（`-`）

### Phase 3（将来拡張）
1. ディレクトリ比較（`-r`）
2. Side-by-side表示（`-y`）
3. 色付き出力
