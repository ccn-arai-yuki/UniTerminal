# cd コマンド実装計画書

## 1. 概要

`cd` コマンドは、現在の作業ディレクトリを変更するコマンドです。Linux/Unix の `cd` コマンドに準拠した動作を目指します。

### 1.1 基本仕様

- **コマンド名:** `cd`
- **説明:** 作業ディレクトリを変更
- **書式:** `cd [オプション] [ディレクトリ]`

### 1.2 参考

- Bash cd: https://www.gnu.org/software/bash/manual/html_node/Bourne-Shell-Builtins.html
- POSIX cd: https://pubs.opengroup.org/onlinepubs/9699919799/utilities/cd.html

### 1.3 特記事項

`cd` はシェルの組み込みコマンド（builtin）であり、外部コマンドとしては実装できません。UniTerminal では `Terminal.WorkingDirectory` プロパティを変更することで作業ディレクトリの変更を実現します。

---

## 2. オプション仕様

### 2.1 実装するオプション

| オプション | 短縮形 | 型 | デフォルト | 説明 |
|-----------|--------|-----|-----------|------|
| `--logical` | `-L` | bool | true | シンボリックリンクを保持（デフォルト） |
| `--physical` | `-P` | bool | false | シンボリックリンクを解決 |

### 2.2 特殊な引数

| 引数 | 説明 |
|------|------|
| （なし） | ホームディレクトリに移動 |
| `-` | 前のディレクトリに移動（OLDPWD） |
| `~` | ホームディレクトリに移動 |
| `~username` | （非対応）指定ユーザーのホームディレクトリ |
| `..` | 親ディレクトリに移動 |
| `.` | 現在のディレクトリ（変更なし） |

---

## 3. パス解決

### 3.1 対応するパス形式

| パス形式 | 例 | 説明 |
|---------|-----|------|
| 相対パス | `dir`, `./dir`, `../dir` | 作業ディレクトリからの相対 |
| 絶対パス | `/path/to/dir` | ルートからの絶対パス |
| ホームディレクトリ | `~`, `~/dir` | ホームディレクトリを展開 |
| 前のディレクトリ | `-` | OLDPWD に移動 |

### 3.2 パス解決の優先順位

1. `-` の場合、OLDPWD を使用
2. `~` で始まる場合、ホームディレクトリを展開
3. 絶対パスの場合、そのまま使用
4. 相対パスの場合、作業ディレクトリを基準に解決

### 3.3 正規化

パスの正規化処理：
- `.` を除去
- `..` を解決（親ディレクトリへ）
- 連続する `/` を単一に
- 末尾の `/` を除去

---

## 4. OLDPWD（前のディレクトリ）

### 4.1 仕様

- `cd` でディレクトリを変更する前に、現在のディレクトリを OLDPWD として保存
- `cd -` で OLDPWD に移動し、移動先のパスを標準出力に表示
- 初回起動時、OLDPWD は未設定

### 4.2 実装方法

`Terminal` クラスに `PreviousWorkingDirectory` プロパティを追加します。

```csharp
public class Terminal
{
    public string WorkingDirectory { get; set; }
    public string PreviousWorkingDirectory { get; private set; }

    // cd コマンドから呼び出されるメソッド
    internal void ChangeDirectory(string newPath)
    {
        PreviousWorkingDirectory = WorkingDirectory;
        WorkingDirectory = newPath;
    }
}
```

---

## 5. エラー処理

### 5.1 エラーケース

| エラー | メッセージ | 終了コード |
|--------|-----------|-----------|
| ディレクトリが存在しない | `cd: {path}: No such file or directory` | RuntimeError |
| パスがファイル | `cd: {path}: Not a directory` | RuntimeError |
| アクセス権限がない | `cd: {path}: Permission denied` | RuntimeError |
| OLDPWD未設定で`-`使用 | `cd: OLDPWD not set` | RuntimeError |
| 引数が多すぎる | `cd: too many arguments` | UsageError |

### 5.2 エラー時の動作

エラーが発生した場合、作業ディレクトリは変更されません。

---

## 6. 出力仕様

### 6.1 標準出力

| 状況 | 出力 |
|------|------|
| 通常の cd | 出力なし |
| `cd -` | 移動先のパスを出力 |

### 6.2 例

```
$ pwd
/home/user
$ cd /tmp
$ cd -
/home/user
$ pwd
/home/user
```

---

## 7. 実装詳細

### 7.1 クラス構造

```csharp
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
                targetPath = Path.GetFullPath(targetPath);
            }

            // ディレクトリの存在確認
            if (!Directory.Exists(targetPath))
            {
                if (File.Exists(targetPath))
                {
                    await context.Stderr.WriteLineAsync(
                        $"cd: {context.PositionalArguments[0]}: Not a directory", ct);
                }
                else
                {
                    await context.Stderr.WriteLineAsync(
                        $"cd: {context.PositionalArguments[0]}: No such file or directory", ct);
                }
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
```

### 7.2 CommandContext の拡張

`cd` コマンドが作業ディレクトリを変更できるように、`CommandContext` に機能を追加します。

```csharp
public class CommandContext
{
    // 既存のプロパティ...

    /// <summary>
    /// 前の作業ディレクトリ。
    /// </summary>
    public string PreviousWorkingDirectory { get; }

    /// <summary>
    /// 作業ディレクトリを変更します（cdコマンド専用）。
    /// </summary>
    public Action<string> ChangeWorkingDirectory { get; }
}
```

### 7.3 Terminal クラスの拡張

```csharp
public class Terminal
{
    private string _workingDirectory;
    private string _previousWorkingDirectory;

    public string WorkingDirectory
    {
        get => _workingDirectory;
        set
        {
            if (_workingDirectory != value)
            {
                _previousWorkingDirectory = _workingDirectory;
                _workingDirectory = value;
            }
        }
    }

    public string PreviousWorkingDirectory => _previousWorkingDirectory;
}
```

---

## 8. テストケース

### 8.1 基本テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| CD-001 | 絶対パス移動 | `cd /tmp` | 作業ディレクトリが /tmp に変更 |
| CD-002 | 相対パス移動 | `cd subdir` | 作業ディレクトリが subdir に変更 |
| CD-003 | 親ディレクトリ移動 | `cd ..` | 親ディレクトリに移動 |
| CD-004 | ホームディレクトリ移動 | `cd` | ホームディレクトリに移動 |
| CD-005 | チルダ展開 | `cd ~` | ホームディレクトリに移動 |
| CD-006 | チルダ相対パス | `cd ~/subdir` | ホーム下の subdir に移動 |

### 8.2 OLDPWD テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| CD-010 | 前のディレクトリに移動 | `cd -` | OLDPWD に移動、パスを出力 |
| CD-011 | 連続 cd - | `cd /a` → `cd /b` → `cd -` → `cd -` | /a → /b を往復 |
| CD-012 | OLDPWD未設定 | 初回 `cd -` | エラー「OLDPWD not set」 |

### 8.3 オプションテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| CD-020 | 論理パス | `cd -L path` | シンボリックリンクを保持 |
| CD-021 | 物理パス | `cd -P path` | シンボリックリンクを解決 |

### 8.4 エラーテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| CD-030 | 存在しないディレクトリ | `cd noexist` | エラー「No such file or directory」 |
| CD-031 | ファイルを指定 | `cd file.txt` | エラー「Not a directory」 |
| CD-032 | 引数過多 | `cd a b` | エラー「too many arguments」 |

### 8.5 pwd との連携テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| CD-040 | cd → pwd | `cd /tmp` → `pwd` | `/tmp` を出力 |
| CD-041 | 連続移動 | `cd /a` → `cd /b` → `pwd` | `/b` を出力 |

---

## 9. 補完対応

### 9.1 補完ターゲット

`cd` コマンドの位置引数は**ディレクトリのみ**を補完対象とします。

### 9.2 CompletionEngine の拡張

```csharp
// cd コマンドの場合、ディレクトリのみ補完
if (commandName == "cd")
{
    // ディレクトリのみをフィルタリング
    foreach (var dir in Directory.GetDirectories(basePath))
    {
        yield return new CompletionCandidate(
            Path.GetFileName(dir) + "/",
            CompletionTarget.Path
        );
    }
}
```

---

## 10. 他コマンドとの連携

### 10.1 pwd コマンド

`cd` で変更した作業ディレクトリは `pwd` で確認できます。

### 10.2 ls コマンド

`cd` で移動した後、`ls` で内容を確認する一般的なワークフローをサポートします。

### 10.3 相対パスを使用するすべてのコマンド

`cd` で設定した作業ディレクトリは、他のコマンド（`cat`, `grep` など）の相対パス解決にも影響します。

---

## 11. 実装上の注意点

### 11.1 シェルビルトインとの違い

Linux の `cd` は**シェルのビルトインコマンド**であり、シェルプロセス自体の作業ディレクトリを変更します。

UniTerminal では、`Terminal` クラスのプロパティとして作業ディレクトリを管理し、各コマンドはこの値を参照します。実際のプロセスの作業ディレクトリは変更されません。

### 11.2 プラットフォーム差異

| プラットフォーム | 注意点 |
|-----------------|--------|
| Windows | ドライブレターの扱い、`cd D:` でドライブ変更 |
| macOS/Linux | ケースセンシティブなパス |
| Unity | Application.persistentDataPath などの特殊パス |

### 11.3 パーミッション

ディレクトリへの移動には「実行権限（x）」が必要です。Unity/C# では `Directory.Exists()` で存在確認を行いますが、アクセス権限の詳細な確認はプラットフォーム依存です。

---

## 12. 実装スケジュール

### Phase 1（必須機能）
1. 基本的なディレクトリ移動（絶対パス、相対パス）
2. ホームディレクトリ移動（引数なし、`~`）
3. 親ディレクトリ移動（`..`）
4. エラーハンドリング

### Phase 2（拡張機能）
1. OLDPWD 対応（`cd -`）
2. `-L`, `-P` オプション
3. Terminal/CommandContext の拡張

### Phase 3（将来拡張）
1. CDPATH 環境変数対応
2. ディレクトリスタック（pushd/popd）
