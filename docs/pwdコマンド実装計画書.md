# pwd コマンド実装計画書

## 1. 概要

`pwd` コマンドは、現在の作業ディレクトリ（カレントディレクトリ）の絶対パスを表示するコマンドです。Linux/Unix の `pwd` コマンドに準拠した動作を目指します。

### 1.1 基本仕様

- **コマンド名:** `pwd`
- **説明:** 現在の作業ディレクトリを表示
- **書式:** `pwd [オプション]`

### 1.2 参考

- GNU Coreutils pwd: https://www.gnu.org/software/coreutils/manual/html_node/pwd-invocation.html
- POSIX pwd: https://pubs.opengroup.org/onlinepubs/9699919799/utilities/pwd.html

---

## 2. オプション仕様

### 2.1 実装するオプション

| オプション | 短縮形 | 型 | デフォルト | 説明 |
|-----------|--------|-----|-----------|------|
| `--logical` | `-L` | bool | true | 環境変数やシンボリックリンクを含む論理パスを表示（デフォルト） |
| `--physical` | `-P` | bool | false | シンボリックリンクを解決した物理パスを表示 |

### 2.2 オプションの動作

- **`-L` (論理パス):** `Terminal.WorkingDirectory` に設定されているパスをそのまま表示
- **`-P` (物理パス):** シンボリックリンクを解決した実際のパスを表示（`Path.GetFullPath` を使用）

**注意:** Unity環境では、シンボリックリンクの扱いはプラットフォームに依存します。

---

## 3. 出力形式

### 3.1 標準出力

現在の作業ディレクトリの絶対パスを1行で出力します。

```
/home/user/projects/myapp
```

### 3.2 パス区切り文字

- **Unix系:** `/`（スラッシュ）
- **Windows:** `\`（バックスラッシュ）または `/`

Unity環境での一貫性のため、内部的には `Path.DirectorySeparatorChar` を使用しますが、出力時は `/` に統一することも検討します。

---

## 4. エラー処理

### 4.1 エラーケース

| エラー | メッセージ | 終了コード |
|--------|-----------|-----------|
| 作業ディレクトリが存在しない | `pwd: current directory does not exist` | RuntimeError |
| アクセス権限がない | `pwd: permission denied` | RuntimeError |

### 4.2 エラー発生条件

通常、`pwd` コマンドはエラーを発生させることは稀ですが、以下の場合に発生する可能性があります：

1. 作業ディレクトリが削除された
2. 作業ディレクトリへのアクセス権が失われた
3. `-P` オプション使用時にシンボリックリンクの解決に失敗

---

## 5. 実装詳細

### 5.1 クラス構造

```csharp
namespace Xeon.UniTerminal.BuiltInCommands
{
    /// <summary>
    /// 現在の作業ディレクトリを表示します。
    /// </summary>
    [Command("pwd", "Print current working directory")]
    public class PwdCommand : ICommand
    {
        [Option("logical", "L", Description = "Print logical path (default)")]
        public bool Logical;

        [Option("physical", "P", Description = "Print physical path with symlinks resolved")]
        public bool Physical;

        public string CommandName => "pwd";
        public string Description => "Print current working directory";

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            try
            {
                string path;

                if (Physical)
                {
                    // シンボリックリンクを解決した物理パスを取得
                    path = Path.GetFullPath(context.WorkingDirectory);
                }
                else
                {
                    // 論理パス（設定されているパスをそのまま使用）
                    path = context.WorkingDirectory;
                }

                // ディレクトリの存在確認
                if (!Directory.Exists(path))
                {
                    await context.Stderr.WriteLineAsync(
                        "pwd: current directory does not exist", ct);
                    return ExitCode.RuntimeError;
                }

                await context.Stdout.WriteLineAsync(path, ct);
                return ExitCode.Success;
            }
            catch (UnauthorizedAccessException)
            {
                await context.Stderr.WriteLineAsync("pwd: permission denied", ct);
                return ExitCode.RuntimeError;
            }
            catch (Exception ex)
            {
                await context.Stderr.WriteLineAsync($"pwd: {ex.Message}", ct);
                return ExitCode.RuntimeError;
            }
        }

        public IEnumerable<string> GetCompletions(CompletionContext context)
        {
            // pwdは引数を取らないため補完なし
            yield break;
        }
    }
}
```

### 5.2 オプションの排他処理

`-L` と `-P` は排他的なオプションです。両方指定された場合、後から指定されたオプションが優先されます（Linux準拠）。

実装では、`Physical` フラグのみをチェックし、`true` なら物理パス、`false` なら論理パスを使用します。

---

## 6. テストケース

### 6.1 基本テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| PWD-001 | 基本動作 | `pwd` | 作業ディレクトリの絶対パスを出力 |
| PWD-002 | 論理パス明示 | `pwd -L` | 作業ディレクトリの論理パスを出力 |
| PWD-003 | 物理パス | `pwd -P` | シンボリックリンク解決済みパスを出力 |

### 6.2 オプション組み合わせテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| PWD-010 | 両オプション指定（P後） | `pwd -L -P` | 物理パスを出力 |
| PWD-011 | 両オプション指定（L後） | `pwd -P -L` | 論理パスを出力 |

### 6.3 出力形式テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| PWD-020 | 末尾に改行 | `pwd` | 出力の末尾に改行がある |
| PWD-021 | 絶対パス | `pwd` | `/` または `C:\` で始まる絶対パス |

### 6.4 エラーテスト

| ID | テスト内容 | 条件 | 期待結果 |
|----|-----------|------|----------|
| PWD-030 | 存在しないディレクトリ | 作業ディレクトリ削除後 | エラーメッセージ、RuntimeError |

### 6.5 パイプ連携テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| PWD-040 | パイプ出力 | `pwd \| cat` | パスがcatを通過して出力 |
| PWD-041 | リダイレクト | `pwd > path.txt` | ファイルにパスが書き込まれる |

---

## 7. 補完対応

### 7.1 補完ターゲット

`pwd` コマンドは引数を取らないため、特別な補完は不要です。

オプションの補完（`--logical`, `--physical`）は `CompletionEngine` で自動的に処理されます。

---

## 8. 他コマンドとの連携

### 8.1 cd コマンドとの関係

`cd` コマンドで作業ディレクトリを変更した後、`pwd` で確認するという一般的なワークフローをサポートします。

```
$ cd /home/user
$ pwd
/home/user
$ cd ..
$ pwd
/home
```

### 8.2 Terminal クラスとの統合

`pwd` コマンドは `Terminal.WorkingDirectory` プロパティの値を表示します。この値は `cd` コマンドによって変更されます。

---

## 9. 実装上の注意点

### 9.1 プラットフォーム差異

| プラットフォーム | 注意点 |
|-----------------|--------|
| Windows | パス区切りが `\`、ドライブレターあり |
| macOS/Linux | パス区切りが `/`、ルートが `/` |
| Unity Editor | Application.dataPath がプロジェクト依存 |

### 9.2 シンボリックリンク

- **Windows:** ジャンクションとシンボリックリンクの扱い
- **macOS/Linux:** 標準的なシンボリックリンク
- **Unity:** プラットフォームに依存

`-P` オプション使用時は `Path.GetFullPath()` を使用しますが、完全なシンボリックリンク解決は .NET のバージョンやプラットフォームに依存します。

---

## 10. 実装スケジュール

### Phase 1（必須機能）
1. 基本的な作業ディレクトリ表示
2. `-L` オプション（デフォルト動作）
3. `-P` オプション

### Phase 2（拡張機能）
1. シンボリックリンクの完全な解決（プラットフォーム対応）
2. エラーハンドリングの強化
