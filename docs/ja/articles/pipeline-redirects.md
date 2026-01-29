# パイプラインとリダイレクト

UniTerminalは、強力なコマンドチェーンのためにLinuxライクなパイプラインとリダイレクトをサポートしています。

## パイプライン

パイプ演算子（`|`）を使用してコマンドを接続します。1つのコマンドの出力が次のコマンドの入力になります。

### 基本構文

```bash
command1 | command2 | command3
```

### 使用例

```bash
# オブジェクトを検索してフィルタリング
hierarchy -r | grep -p "Player"

# 複数のフィルタをチェーン
hierarchy -r -l | grep -p "Enemy" | grep -p "Active: True"

# 結果をカウント
hierarchy -r | grep -p "Collider" | count

# ファイル内容を処理
cat config.json | grep -p "setting"
```

### 仕組み

```
┌─────────┐    stdout    ┌─────────┐    stdout    ┌─────────┐
│ Command1 │────────────▶│ Command2 │────────────▶│ Command3 │
└─────────┘              └─────────┘              └─────────┘
                              ▲                        │
                           stdin                    stdout
                                                       ▼
                                                   [出力]
```

1. Command1がstdoutに書き込む
2. Command2がstdin（Command1の出力）から読み取る
3. Command3がstdin（Command2の出力）から読み取る
4. 最終出力がターミナルに送られる

## リダイレクト

### 出力リダイレクト（`>`）

コマンドの出力をファイルに書き込み、既存の内容を**上書き**します:

```bash
# ヒエラルキーをファイルに保存
hierarchy -r > hierarchy.txt

# フィルタリングした結果を保存
hierarchy -r | grep -p "Player" > players.txt

# 設定をエクスポート
property list /Settings GameConfig > config_dump.txt
```

### 追記リダイレクト（`>>`）

コマンドの出力をファイルに追記し、既存の内容を**保持**します:

```bash
# ログファイルに追加
echo "Session started" >> session.log

# ヒエラルキーのスナップショットを追記
hierarchy -r >> snapshots.txt

# レポートを構築
echo "=== Players ===" >> report.txt
hierarchy -n "Player*" >> report.txt
echo "=== Enemies ===" >> report.txt
hierarchy -n "Enemy*" >> report.txt
```

### 入力リダイレクト（`<`）

ファイルの内容をコマンドの入力として読み取ります:

```bash
# ファイル内を検索
grep -p "error" < log.txt

# ファイル内容を処理
count < data.txt

# ファイル内の単語数をカウント
count -w < document.txt
```

## 組み合わせ使用

### パイプライン + 出力リダイレクト

```bash
# フィルタリングして保存
hierarchy -r | grep -p "UI" > ui_objects.txt

# 処理して保存
cat data.txt | grep -p "important" | count > result.txt
```

### 入力リダイレクト + パイプライン

```bash
# ファイルを読み取ってフィルタリング
grep -p "error" < log.txt | count

# パイプラインでファイルを処理
count -w < document.txt
```

### 複雑なチェーン

```bash
# 完全な分析パイプライン
hierarchy -r -l | grep -p "Enemy" | grep -v "Disabled" > active_enemies.txt

# 多段階処理
cat input.txt | grep -p "data" | count > analysis.txt
```

## 実用例

### シーン分析

```bash
# 完全なヒエラルキーをエクスポート
hierarchy -r -l > scene_dump.txt

# タイプ別にオブジェクト数をカウント
echo "Rigidbody count:" > physics_report.txt
hierarchy -c Rigidbody | count >> physics_report.txt
echo "Collider count:" >> physics_report.txt
hierarchy -c Collider | count >> physics_report.txt
```

### デバッグログ

```bash
# デバッグスナップショットを作成
echo "=== Debug Snapshot ===" >> debug.log
hierarchy -r -l >> debug.log
echo "" >> debug.log
```

### 設定エクスポート

```bash
# すべての設定をエクスポート
property list /GameManager Settings > settings.txt
property list /AudioManager AudioSettings >> settings.txt
property list /GraphicsManager GraphicsSettings >> settings.txt
```

### バッチ処理

```bash
# すべてのUI要素を検索してドキュメント化
hierarchy -c "UnityEngine.UI.Image" > ui_images.txt
hierarchy -c "UnityEngine.UI.Text" > ui_texts.txt
hierarchy -c "UnityEngine.UI.Button" > ui_buttons.txt
```

## エラー処理

### Stderr と Stdout

- **Stdout**: 通常の出力（パイプラインを通過）
- **Stderr**: エラーメッセージ（直接表示）

```bash
# エラーはパイプを通過しない
nonexistent_command | grep -p "test"
# Error: Command 'nonexistent_command' not found
# （grepは何も受け取らない）
```

### パイプラインの終了コード

パイプラインの終了コードは、最後のコマンドの終了コードです:

```bash
# grepが何も見つからなくても、終了コードはSuccess
# （grepは何も出力しないが、エラーではない）
hierarchy | grep -p "NonExistent"
```

## ヒント

1. **パイプラインはフィルタリングに使用** - すべてをメモリにロードするのを避ける
2. **大きな出力はリダイレクト** - 表示の代わりにファイルに保存
3. **段階的にチェーンを構築** - 複雑なパイプラインは少しずつ構築
4. **中間結果を確認** - デバッグのために最後のリダイレクトを削除

### パイプラインのデバッグ

```bash
# 完全なパイプライン
hierarchy -r | grep -p "Player" | grep -p "Active" > result.txt

# ステップ1をデバッグ
hierarchy -r

# ステップ2をデバッグ
hierarchy -r | grep -p "Player"

# ステップ3をデバッグ
hierarchy -r | grep -p "Player" | grep -p "Active"

# リダイレクト付きの最終版
hierarchy -r | grep -p "Player" | grep -p "Active" > result.txt
```
