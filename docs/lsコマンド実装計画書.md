# ls コマンド実装計画書

## 1. 概要

`ls` コマンドは、ディレクトリの内容を一覧表示するコマンドです。Linux/Unix の `ls` コマンドに準拠した動作を目指します。

### 1.1 基本仕様

- **コマンド名:** `ls`
- **説明:** ディレクトリの内容を一覧表示
- **書式:** `ls [オプション] [パス...]`

### 1.2 参考

- GNU Coreutils ls: https://www.gnu.org/software/coreutils/manual/html_node/ls-invocation.html
- POSIX ls: https://pubs.opengroup.org/onlinepubs/9699919799/utilities/ls.html

---

## 2. オプション仕様

### 2.1 実装するオプション（Phase 1）

| オプション | 短縮形 | 型 | デフォルト | 説明 |
|-----------|--------|-----|-----------|------|
| `--all` | `-a` | bool | false | `.` で始まる隠しファイルも表示 |
| `--long` | `-l` | bool | false | 詳細形式で表示 |
| `--human-readable` | `-h` | bool | false | サイズを人間が読みやすい形式で表示（-l と併用） |
| `--reverse` | `-r` | bool | false | 逆順でソート |
| `--recursive` | `-R` | bool | false | サブディレクトリを再帰的に表示 |
| `--sort` | `-S` | enum | name | ソート方法（name, size, time） |

### 2.2 将来的に実装を検討するオプション（Phase 2）

| オプション | 短縮形 | 説明 |
|-----------|--------|------|
| `--directory` | `-d` | ディレクトリ自体の情報を表示 |
| `--inode` | `-i` | iノード番号を表示 |
| `--color` | | 色付き出力 |
| `--time` | `-t` | 更新時刻でソート |
| `--almost-all` | `-A` | `.` と `..` 以外の隠しファイルを表示 |

---

## 3. 出力形式

### 3.1 通常出力（デフォルト）

ファイル名をスペース区切りで横に並べて表示します。

```
file1.txt  file2.txt  directory1  script.sh
```

### 3.2 詳細出力（`-l` オプション）

```
drwxr-xr-x  2  2024-01-15 10:30  directory1
-rw-r--r--  1  2024-01-15 09:15  file1.txt
-rwxr-xr-x  1  2024-01-14 18:00  script.sh
```

**列の説明:**
1. ファイルタイプとパーミッション（10文字）
2. ハードリンク数（簡略化のため常に1または2）
3. 更新日時（YYYY-MM-DD HH:MM形式）
4. ファイル名

**ファイルタイプ:**
- `d` : ディレクトリ
- `-` : 通常ファイル
- `l` : シンボリックリンク（対応する場合）

**パーミッション:**
- `r` : 読み取り可能
- `w` : 書き込み可能
- `x` : 実行可能（ディレクトリの場合はアクセス可能）
- `-` : 権限なし

### 3.3 人間が読みやすいサイズ（`-h` オプション）

```
-rw-r--r--  1  1.2K  2024-01-15 09:15  file1.txt
-rw-r--r--  1  4.5M  2024-01-15 09:15  large.bin
-rw-r--r--  1  2.1G  2024-01-15 09:15  huge.iso
```

単位: B, K, M, G, T

---

## 4. ソート仕様

### 4.1 ソート順序

| ソート種別 | 説明 |
|-----------|------|
| `name` | ファイル名のアルファベット順（デフォルト） |
| `size` | ファイルサイズ順（大きい順） |
| `time` | 更新時刻順（新しい順） |

### 4.2 ソートルール

1. `-r` オプションで逆順
2. ディレクトリとファイルは混在してソート（Linux準拠）
3. 大文字小文字を区別しない（Unity環境を考慮）

---

## 5. パス解決

### 5.1 対応するパス形式

| パス形式 | 例 | 説明 |
|---------|-----|------|
| 相対パス | `./dir`, `../dir`, `dir` | 作業ディレクトリからの相対 |
| 絶対パス | `/path/to/dir` | ルートからの絶対パス |
| ホームディレクトリ | `~`, `~/dir` | ホームディレクトリを展開 |

### 5.2 複数パス指定

複数のパスを指定した場合、各パスの内容を順番に表示します。

```
$ ls dir1 dir2
dir1:
file1.txt  file2.txt

dir2:
file3.txt  file4.txt
```

---

## 6. エラー処理

### 6.1 エラーケース

| エラー | メッセージ | 終了コード |
|--------|-----------|-----------|
| パスが存在しない | `ls: {path}: No such file or directory` | RuntimeError |
| アクセス権限がない | `ls: {path}: Permission denied` | RuntimeError |
| ファイルを指定した場合 | （エラーではなく、そのファイル情報を表示） | Success |

### 6.2 部分的成功

複数パス指定時、一部がエラーでも他のパスは処理を継続します。最終的な終了コードはエラーがあった場合 RuntimeError になります。

---

## 7. 実装詳細

### 7.1 クラス構造

```csharp
namespace Xeon.UniTerminal.BuiltInCommands
{
    public enum LsSortType
    {
        Name,
        Size,
        Time
    }

    [Command("ls", "List directory contents")]
    public class LsCommand : ICommand
    {
        [Option("all", "a", Description = "Do not ignore entries starting with .")]
        public bool ShowAll;

        [Option("long", "l", Description = "Use a long listing format")]
        public bool LongFormat;

        [Option("human-readable", "h", Description = "Print sizes in human readable format")]
        public bool HumanReadable;

        [Option("reverse", "r", Description = "Reverse order while sorting")]
        public bool Reverse;

        [Option("recursive", "R", Description = "List subdirectories recursively")]
        public bool Recursive;

        [Option("sort", "S", Description = "Sort by: name, size, time")]
        public LsSortType SortBy;

        // 実装...
    }
}
```

### 7.2 ファイル情報取得

```csharp
// System.IO を使用
FileInfo / DirectoryInfo
- Name
- Length
- LastWriteTime
- Attributes (Hidden, Directory, etc.)

// パーミッションは簡略化
// Windows/Unity環境ではUnixパーミッションを完全には再現できないため、
// 読み取り可能/書き込み可能の判定で代替
```

### 7.3 サイズのフォーマット

```csharp
private string FormatSize(long bytes, bool humanReadable)
{
    if (!humanReadable)
        return bytes.ToString();

    string[] units = { "B", "K", "M", "G", "T" };
    int unitIndex = 0;
    double size = bytes;

    while (size >= 1024 && unitIndex < units.Length - 1)
    {
        size /= 1024;
        unitIndex++;
    }

    return unitIndex == 0
        ? $"{size:F0}{units[unitIndex]}"
        : $"{size:F1}{units[unitIndex]}";
}
```

---

## 8. テストケース

### 8.1 基本テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| LS-001 | カレントディレクトリ一覧 | `ls` | 作業ディレクトリの内容を表示 |
| LS-002 | 指定ディレクトリ一覧 | `ls path/to/dir` | 指定ディレクトリの内容を表示 |
| LS-003 | 隠しファイル表示 | `ls -a` | `.`で始まるファイルも表示 |
| LS-004 | 詳細形式 | `ls -l` | パーミッション、サイズ、日時を表示 |

### 8.2 オプション組み合わせテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| LS-010 | 詳細+人間可読サイズ | `ls -lh` | サイズがK/M/G形式 |
| LS-011 | 全表示+詳細 | `ls -la` | 隠しファイル含む詳細表示 |
| LS-012 | 逆順ソート | `ls -r` | Z→A順で表示 |
| LS-013 | サイズ順ソート | `ls -lS --sort=size` | サイズ大→小で表示 |

### 8.3 エラーテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| LS-020 | 存在しないパス | `ls noexist` | エラーメッセージ、RuntimeError |
| LS-021 | ファイル指定 | `ls file.txt` | そのファイルの情報を表示 |
| LS-022 | 複数パス（一部エラー） | `ls dir1 noexist dir2` | dir1,dir2は表示、noexistはエラー |

### 8.4 再帰テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| LS-030 | 再帰表示 | `ls -R` | サブディレクトリも表示 |
| LS-031 | 再帰+詳細 | `ls -lR` | 詳細形式で再帰表示 |

---

## 9. 補完対応

### 9.1 補完ターゲット

- **位置引数:** ファイルパス補完（ディレクトリ優先）
- **オプション値:** `--sort` の値（name, size, time）

### 9.2 実装

```csharp
public IEnumerable<string> GetCompletions(CompletionContext context)
{
    // --sort オプションの値補完
    if (context.CurrentOption == "sort")
    {
        yield return "name";
        yield return "size";
        yield return "time";
    }
    // パス補完はCompletionEngineで処理
}
```

---

## 10. 実装スケジュール

### Phase 1（必須機能）
1. 基本的なディレクトリ一覧表示
2. `-a`, `-l`, `-h` オプション
3. 基本的なソート機能
4. パス解決（相対、絶対、~）

### Phase 2（拡張機能）
1. `-R` 再帰表示
2. 複数パス対応
3. 追加のソートオプション

### Phase 3（将来拡張）
1. 色付き出力
2. 追加オプション
