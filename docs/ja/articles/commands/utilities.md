# ユーティリティ

一般的なユーティリティコマンドです。

## help

コマンドのヘルプ情報を表示します。

### 書式

```bash
help [command]
```

### 説明

引数なしの場合、利用可能なすべてのコマンドを一覧表示します。コマンド名を指定すると、そのコマンドの詳細なヘルプを表示します。

### 引数

| 引数 | 説明 |
|----------|------|
| `command` | ヘルプを表示するコマンド名（オプション） |

### 使用例

```bash
# すべてのコマンドを一覧表示
help

# 特定のコマンドのヘルプを表示
help ls
help hierarchy
help property
```

### 終了コード

| コード | 説明 |
|------|------|
| 0 | 成功 |
| 1 | 指定されたコマンドが見つからない |

---

## history

コマンド履歴を管理します。

### 書式

```bash
history [-c] [-d index] [-n count] [-r file]
```

### 説明

コマンド履歴を表示・管理します。引数なしの場合、すべての履歴を表示します。

### オプション

| オプション | ロング形式 | 説明 |
|--------|------|------|
| `-c` | `--clear` | 履歴をすべてクリア |
| `-d` | `--delete` | 指定インデックスのエントリを削除 |
| `-n` | `--number` | 最後のN件のみを表示 |
| `-r` | `--read` | ファイルから履歴を読み込み |

### 使用例

```bash
# コマンド履歴を表示
history

# 最後の10件を表示
history -n 10

# 履歴をクリア
history -c

# 特定のエントリを削除
history -d 5

# ファイルから履歴を読み込み
history -r ~/.terminal_history
```

### 出力

```
  1  echo Hello
  2  ls -la
  3  hierarchy -r
  4  go create Player
  5  transform /Player -p 0,1,0
```

### 終了コード

| コード | 説明 |
|------|------|
| 0 | 成功 |
| 1 | 無効なインデックス |
| 2 | ファイルが見つからない |

---

## clear

画面表示をクリアします。

### 書式

```bash
clear
```

### 説明

ターミナルの出力表示をクリアします。UniTerminal UIコンポーネントと連携して動作します。

### 使用例

```bash
clear
```

### 終了コード

| コード | 説明 |
|------|------|
| 0 | 常に成功 |

---

## log

Unityログを表示・監視します。

### 書式

```bash
log [-i] [-w] [-e] [-f] [-t N] [-h N] [-s]
```

### 説明

Unity Debug.Logで出力されたログを表示します。ログタイプでフィルタリングしたり、リアルタイムで監視したりできます。

### オプション

| オプション | ロング形式 | 説明 |
|--------|------|------|
| `-i` | `--info` | Info（通常ログ）のみを表示 |
| `-w` | `--warn` | Warningのみを表示 |
| `-e` | `--error` | Error、Exception、Assertを表示 |
| `-f` | `--follow` | リアルタイムで監視（Ctrl+Cで停止） |
| `-t` | `--tail` | 末尾N件を表示 |
| `-h` | `--head` | 先頭N件を表示 |
| `-s` | `--stack-trace` | スタックトレースを表示 |

### 使用例

```bash
# 全ログを表示
log

# Infoのみ表示
log -i

# Warningのみ表示
log -w

# Errorのみ表示（Exception, Assertを含む）
log -e

# 複合フィルタ
log -i -w

# 末尾N件を表示
log -t 20

# 先頭N件を表示
log -h 10

# スタックトレースを表示
log -s

# リアルタイム監視（Ctrl+Cで停止）
log -f

# フィルタ付きリアルタイム監視
log -f -e
```

### 出力形式

**通常形式:**
```
[10:30:15] [Info] Game started
[10:30:16] [Warning] Low memory warning
[10:30:17] [Error] Failed to load asset
```

**スタックトレース付き (`-s`):**
```
[10:30:17] [Error] Failed to load asset
  at GameManager.LoadAsset() (at Assets/Scripts/GameManager.cs:45)
  at GameManager.Start() (at Assets/Scripts/GameManager.cs:20)
```

### 終了コード

| コード | 説明 |
|------|------|
| 0 | 成功 |

---

## 実用的なパターン

### デバッグワークフロー

```bash
# エラーログを確認
log -e

# スタックトレース付きで最新のエラーを確認
log -e -t 5 -s

# エラーをリアルタイム監視
log -f -e

# ログをファイルに保存
log > debug_log.txt
```

### 開発中のログ監視

```bash
# 全ログをリアルタイム監視
log -f

# Warningとエラーのみ監視
log -f -w -e

# パイプラインでフィルタリング
log | grep -p "Player"
```

### 履歴の活用

```bash
# 履歴から特定のコマンドを検索
history | grep -p "hierarchy"

# 最近のコマンドを確認
history -n 20

# 履歴を保存
history > command_history.txt
```
