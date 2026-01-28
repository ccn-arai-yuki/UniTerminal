# Terminal の使い方とコマンド一覧

このページでは `Terminal` クラスの基本的な使い方と、実装済みのコマンドを一覧で確認できます。

## Terminal クラスの基本

### 1. Terminal の初期化

```csharp
using Xeon.UniTerminal;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public sealed class TerminalExample : MonoBehaviour
{
    private Terminal _terminal;

    private void Awake()
    {
        _terminal = new Terminal(
            workingDirectory: Application.dataPath,
            homeDirectory: Application.dataPath,
            registerBuiltInCommands: true
        );
    }
}
```

### 2. コマンド実行

```csharp
async Task RunCommandAsync(CancellationToken ct)
{
    var stdout = new StringWriter();
    var stderr = new StringWriter();

    var exitCode = await _terminal.ExecuteAsync(
        "echo Hello, UniTerminal!",
        stdout,
        stderr,
        ct
    );

    if (exitCode == ExitCode.Success)
    {
        Debug.Log(stdout.ToString());
    }
    else
    {
        Debug.LogError(stderr.ToString());
    }
}
```

### 3. パイプラインとリダイレクト

```csharp
// パイプライン
await _terminal.ExecuteAsync(
    "hierarchy -r | grep Player",
    stdout,
    stderr,
    ct
);

// リダイレクト（ファイル出力）
await _terminal.ExecuteAsync(
    "hierarchy -r > hierarchy.txt",
    stdout,
    stderr,
    ct
);
```

## 実装済みコマンド一覧

### ファイル操作

| コマンド | 概要 |
|---------|------|
| `pwd` | 現在の作業ディレクトリを表示 |
| `cd` | 作業ディレクトリを変更 |
| `ls` | ディレクトリ内容を一覧表示 |
| `cat` | ファイル内容を表示 |
| `find` | ファイルを検索 |
| `less` | ファイルをページ単位で表示 |
| `diff` | ファイル差分を比較 |
| `head` | ファイル先頭を表示 |
| `tail` | ファイル末尾を表示 |

### テキスト処理

| コマンド | 概要 |
|---------|------|
| `echo` | テキストを出力 |
| `grep` | パターンマッチング検索 |

### ユーティリティ

| コマンド | 概要 |
|---------|------|
| `help` | ヘルプを表示 |
| `history` | コマンド履歴を管理 |
| `clear` | 画面表示をクリア |

### Unity 固有

| コマンド | 概要 |
|---------|------|
| `hierarchy` | シーンヒエラルキーを表示 |
| `go` | GameObject を操作 |
| `transform` | Transform を操作 |
| `component` | コンポーネントを管理 |
| `property` | プロパティ値を操作 |
| `scene` | シーンを管理 |
| `log` | Unity ログを表示・監視 |
