# find コマンド実装計画書

## 1. 概要

`find` コマンドは、ディレクトリ階層内でファイルやディレクトリを検索するコマンドです。Linux/Unix の `find` コマンドに準拠した動作を目指します。

### 1.1 基本仕様

- **コマンド名:** `find`
- **説明:** ディレクトリ階層内でファイルを検索
- **書式:** `find [パス...] [検索条件] [アクション]`

### 1.2 参考

- GNU findutils: https://www.gnu.org/software/findutils/manual/html_mono/find.html
- POSIX find: https://pubs.opengroup.org/onlinepubs/9699919799/utilities/find.html

---

## 2. オプション仕様

### 2.1 実装するオプション（Phase 1）

| オプション | 短縮形 | 型 | デフォルト | 説明 |
|-----------|--------|-----|-----------|------|
| `--name` | `-n` | string | null | ファイル名パターン（ワイルドカード対応） |
| `--iname` | `-i` | string | null | ファイル名パターン（大文字小文字区別なし） |
| `--type` | `-t` | enum | all | ファイルタイプ（f=ファイル, d=ディレクトリ） |
| `--maxdepth` | `-d` | int | -1 | 検索する最大深度（-1=無制限） |
| `--mindepth` | | int | 0 | 検索を開始する最小深度 |

### 2.2 将来的に実装を検討するオプション（Phase 2）

| オプション | 短縮形 | 説明 |
|-----------|--------|------|
| `--size` | `-s` | ファイルサイズで検索 |
| `--mtime` | `-m` | 更新時刻で検索 |
| `--newer` | | 指定ファイルより新しいファイル |
| `--regex` | | 正規表現でマッチ |
| `--exec` | | 見つかったファイルに対してコマンド実行 |
| `--delete` | | 見つかったファイルを削除 |

---

## 3. 検索条件

### 3.1 名前パターン

#### ワイルドカード

| パターン | 説明 | 例 |
|---------|------|-----|
| `*` | 0文字以上の任意の文字列 | `*.txt` → すべての.txtファイル |
| `?` | 任意の1文字 | `file?.txt` → file1.txt, fileA.txt |
| `[abc]` | 指定文字のいずれか | `file[123].txt` → file1.txt, file2.txt |
| `[a-z]` | 範囲内の文字 | `[a-z]*.txt` → 小文字で始まる.txtファイル |

#### 例

```bash
# すべての.csファイルを検索
find . --name "*.cs"

# 大文字小文字を区別せずに検索
find . --iname "readme*"
```

### 3.2 ファイルタイプ

| タイプ | 説明 |
|-------|------|
| `f` | 通常ファイル |
| `d` | ディレクトリ |
| `all` | すべて（デフォルト） |

---

## 4. 出力形式

### 4.1 デフォルト出力

見つかったファイルのパスを1行ずつ出力します。

```
./src/main.cs
./src/utils/helper.cs
./tests/test.cs
```

### 4.2 パス表記

- 検索開始パスからの相対パスで表示
- `.` で始まる（カレントディレクトリの場合）

---

## 5. パス解決

### 5.1 検索開始パス

| パス形式 | 例 | 説明 |
|---------|-----|------|
| 相対パス | `./dir`, `../dir`, `dir` | 作業ディレクトリからの相対 |
| 絶対パス | `/path/to/dir` | ルートからの絶対パス |
| ホームディレクトリ | `~`, `~/dir` | ホームディレクトリを展開 |
| 省略時 | (なし) | カレントディレクトリ `.` |

### 5.2 複数パス指定

複数の検索開始パスを指定可能です。

```bash
find dir1 dir2 --name "*.txt"
```

---

## 6. エラー処理

### 6.1 エラーケース

| エラー | メッセージ | 終了コード |
|--------|-----------|-----------|
| パスが存在しない | `find: {path}: No such file or directory` | RuntimeError |
| アクセス権限がない | `find: {path}: Permission denied` | RuntimeError（継続） |
| 無効なパターン | `find: invalid pattern: {pattern}` | UsageError |

### 6.2 部分的成功

アクセス権限がないディレクトリがあっても、他のディレクトリの検索は継続します。エラーメッセージはstderrに出力します。

---

## 7. 実装詳細

### 7.1 クラス構造

```csharp
namespace Xeon.UniTerminal.BuiltInCommands
{
    public enum FindFileType
    {
        All,
        File,
        Directory
    }

    [Command("find", "Search for files in a directory hierarchy")]
    public class FindCommand : ICommand
    {
        [Option("name", "n", Description = "File name pattern (supports wildcards)")]
        public string NamePattern;

        [Option("iname", "i", Description = "Case-insensitive file name pattern")]
        public string INamePattern;

        [Option("type", "t", Description = "File type: f (file), d (directory)")]
        public FindFileType FileType;

        [Option("maxdepth", "d", Description = "Maximum search depth")]
        public int MaxDepth = -1;

        [Option("mindepth", "", Description = "Minimum search depth")]
        public int MinDepth = 0;

        // 実装...
    }
}
```

### 7.2 ワイルドカードマッチング

```csharp
private bool MatchWildcard(string name, string pattern, bool ignoreCase)
{
    // パターンを正規表現に変換
    // * → .*
    // ? → .
    // [abc] → [abc]
    var regexPattern = "^" + Regex.Escape(pattern)
        .Replace("\\*", ".*")
        .Replace("\\?", ".")
        .Replace("\\[", "[")
        .Replace("\\]", "]") + "$";

    var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
    return Regex.IsMatch(name, regexPattern, options);
}
```

### 7.3 再帰検索

```csharp
private IEnumerable<string> SearchDirectory(
    string basePath,
    string currentPath,
    int currentDepth)
{
    if (MaxDepth >= 0 && currentDepth > MaxDepth)
        yield break;

    // 現在の深度がMinDepth以上ならマッチを確認
    if (currentDepth >= MinDepth)
    {
        if (MatchesCriteria(currentPath))
            yield return GetRelativePath(basePath, currentPath);
    }

    // ディレクトリの場合、サブディレクトリを再帰検索
    if (Directory.Exists(currentPath))
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(currentPath))
        {
            foreach (var result in SearchDirectory(basePath, entry, currentDepth + 1))
                yield return result;
        }
    }
}
```

---

## 8. テストケース

### 8.1 基本テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| FIND-001 | 全ファイル検索 | `find .` | すべてのファイル・ディレクトリを表示 |
| FIND-002 | 名前パターン検索 | `find . --name "*.txt"` | .txtファイルのみ表示 |
| FIND-003 | 大文字小文字無視 | `find . --iname "README*"` | readme*, README*等を表示 |
| FIND-004 | タイプ指定（ファイル） | `find . --type f` | ファイルのみ表示 |
| FIND-005 | タイプ指定（ディレクトリ） | `find . --type d` | ディレクトリのみ表示 |

### 8.2 深度制限テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| FIND-010 | 最大深度指定 | `find . --maxdepth 1` | 直下のみ検索 |
| FIND-011 | 最小深度指定 | `find . --mindepth 2` | 2階層目以降を検索 |
| FIND-012 | 深度範囲指定 | `find . --mindepth 1 --maxdepth 2` | 1-2階層目を検索 |

### 8.3 ワイルドカードテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| FIND-020 | アスタリスク | `find . --name "file*"` | file で始まるファイル |
| FIND-021 | クエスチョン | `find . --name "file?.txt"` | file + 1文字 + .txt |
| FIND-022 | 文字クラス | `find . --name "[abc]*.txt"` | a,b,cで始まる.txtファイル |

### 8.4 エラーテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| FIND-030 | 存在しないパス | `find noexist` | エラーメッセージ |
| FIND-031 | 空の結果 | `find . --name "*.xyz"` | 結果なし（エラーではない） |

### 8.5 パイプテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| FIND-040 | grep連携 | `find . --name "*.cs" \| grep test` | test を含むパスのみ |

---

## 9. 補完対応

### 9.1 補完ターゲット

- **位置引数:** ディレクトリパス補完
- **オプション値:** `--type` の値（f, d）

### 9.2 実装

```csharp
public IEnumerable<string> GetCompletions(CompletionContext context)
{
    // --type オプションの値補完
    if (context.CurrentOption == "type")
    {
        yield return "f";
        yield return "d";
    }
    // パス補完はCompletionEngineで処理
}
```

---

## 10. 実装スケジュール

### Phase 1（必須機能）
1. 基本的な再帰検索
2. `--name`, `--iname` パターンマッチ
3. `--type` によるタイプフィルタ
4. `--maxdepth`, `--mindepth` による深度制限

### Phase 2（拡張機能）
1. `--size` サイズフィルタ
2. `--mtime` 時刻フィルタ
3. 複数パス対応

### Phase 3（将来拡張）
1. `--exec` コマンド実行
2. `--regex` 正規表現対応
3. 論理演算子（-and, -or, -not）
