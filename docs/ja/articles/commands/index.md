# 組み込みコマンド

UniTerminalは、カテゴリ別に整理された包括的な組み込みコマンドセットを提供します。

## コマンドカテゴリ

| カテゴリ | コマンド | 説明 |
|----------|----------|------|
| [ファイル操作](file-operations.md) | `pwd`, `cd`, `ls`, `cat`, `find`, `less`, `diff`, `head`, `tail` | ファイルとディレクトリの操作 |
| [テキスト処理](text-processing.md) | `echo`, `grep` | テキストの処理とフィルタリング |
| [ユーティリティ](utilities.md) | `help`, `history`, `clear`, `log` | 一般的なユーティリティ |
| [Unityコマンド](unity-commands.md) | `hierarchy`, `go`, `transform`, `component`, `property`, `scene` | Unity固有の操作 |

## よく使うパターン

### コマンドの組み合わせ

```bash
# GameObjectを検索してフィルタリング
hierarchy -r | grep Enemy

# ファイルを一覧表示してフィルタリング
ls -la | grep .cs

# ヒエラルキーをファイルにエクスポート
hierarchy -r > hierarchy_dump.txt
```

### Unityオブジェクトの操作

```bash
# すべてのRigidbodyオブジェクトを検索
hierarchy -c Rigidbody

# Transform情報を取得
transform /Player -p

# コンポーネントのプロパティを変更
property set /Player Rigidbody mass 10
```

## 終了コード

| コード | 説明 |
|--------|------|
| 0 | 成功 |
| 1 | 使用方法エラー |
| 2 | 実行時エラー |
