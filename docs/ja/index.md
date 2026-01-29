# UniTerminal

Unity向けのLinuxライクなCLI実行フレームワークです。

このドキュメントでは、組み込みコマンドと `Terminal` クラスの使い方を中心にまとめています。

## 特徴

- **Linuxライクな構文** - パイプ (`|`)、リダイレクト (`>`, `>>`, `<`)
- **組み込みコマンド** - ファイル操作、テキスト処理、Unity固有のコマンド
- **拡張可能** - カスタムコマンドの簡単な追加
- **非同期サポート** - async/await と UniTask の統合
- **タブ補完** - コンテキストを考慮したコマンド補完

## クイックリンク

- [はじめに](articles/getting-started.md)
- [組み込みコマンド](articles/commands/index.md)
- [English Documentation](../index.md)

## インストール

### Package Manager経由（Git URL）

```
https://github.com/AraiYuhki/UniTerminal.git?path=Packages/jp.xeon.uni-terminal
```

### Unity Asset Store経由

Asset Storeウィンドウで「UniTerminal」を検索してください。

## 基本的な使い方

```csharp
using Xeon.UniTerminal;

var terminal = new Terminal(
    workingDirectory: Application.dataPath,
    homeDirectory: Application.dataPath,
    registerBuiltInCommands: true
);

var stdout = new StringWriter();
var stderr = new StringWriter();

await terminal.ExecuteAsync("echo Hello, World!", stdout, stderr, ct);
```

## ライセンス

MIT License - [LICENSE](https://github.com/AraiYuhki/UniTerminal/blob/main/Packages/jp.xeon.uni-terminal/LICENSE.md)を参照
