# はじめに

このガイドでは、UnityプロジェクトでUniTerminalをセットアップして実行する方法を説明します。

## 動作要件

- Unity 6000.0 以上
- （オプション）UniTask 2.0 以上（高パフォーマンスな非同期処理のため）

## インストール

### 方法1: Unity Asset Store

1. **Window > Package Manager** を開く
2. **My Assets** タブを選択
3. 「UniTerminal」を検索
4. **Import** をクリック

### 方法2: Git URL

1. **Window > Package Manager** を開く
2. **+** > **Add package from git URL...** をクリック
3. 以下を入力:

```
https://github.com/AraiYuhki/UniTerminal.git?path=Packages/jp.xeon.uni-terminal
```

### 方法3: manifest.json

`Packages/manifest.json` に以下を追加:

```json
{
  "dependencies": {
    "jp.xeon.uni-terminal": "https://github.com/AraiYuhki/UniTerminal.git?path=Packages/jp.xeon.uni-terminal"
  }
}
```

## クイックスタート

### 1. Terminalインスタンスの作成

```csharp
using Xeon.UniTerminal;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class TerminalExample : MonoBehaviour
{
    private Terminal _terminal;

    void Awake()
    {
        _terminal = new Terminal(
            workingDirectory: Application.dataPath,
            homeDirectory: Application.dataPath,
            registerBuiltInCommands: true
        );
    }
}
```

### 2. コマンドの実行

```csharp
async Task RunCommand(CancellationToken ct)
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

### 3. パイプラインの使用

```csharp
// 複数のコマンドをチェーン
await _terminal.ExecuteAsync(
    "hierarchy -r | grep Player",
    stdout, stderr, ct
);
```

### 4. リダイレクトの使用

```csharp
// ファイルへ出力
await _terminal.ExecuteAsync(
    "hierarchy -r > hierarchy.txt",
    stdout, stderr, ct
);

// ファイルへ追記
await _terminal.ExecuteAsync(
    "echo NewLine >> output.txt",
    stdout, stderr, ct
);
```

## 次のステップ

- [組み込みコマンド](commands/index.md) - 利用可能なコマンドを学ぶ
- [カスタムコマンド](custom-commands.md) - 独自のコマンドを作成
- [パイプラインとリダイレクト](pipeline-redirects.md) - 高度な使い方
- [UniTaskサポート](unitask-support.md) - 高パフォーマンスな非同期処理
