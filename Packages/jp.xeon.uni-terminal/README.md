# UniTerminal

Unity向けのLinuxライクなCLI実行フレームワークです。文字列ベースのコマンドを解析・実行し、パイプラインやリダイレクトなどのシェル機能をサポートします。

## 特徴

- **Linuxライクなコマンド構文**: パイプ (`|`)、リダイレクト (`>`, `>>`, `<`) をサポート
- **豊富な組み込みコマンド**: ファイル操作、テキスト処理、Unity固有のコマンドを提供
- **拡張可能**: カスタムコマンドを簡単に追加可能
- **非同期実行**: async/awaitによる非同期コマンド実行
- **UniTaskサポート**: UniTaskを使用した高パフォーマンスな非同期処理（オプション）
- **タブ補完**: コマンドやパスの補完機能
- **FlyweightScrollView**: 大量のログ表示に対応した仮想スクロールビュー

## 動作要件

- Unity 6000.0 以上
- （オプション）UniTask 2.0 以上

## インストール

### Package Manager経由

1. Window > Package Manager を開く
2. 「+」ボタン > 「Add package from git URL...」を選択
3. 以下のURLを入力:
```
https://github.com/AraiYuhki/UniTerminal.git?path=Packages/jp.xeon.uni-terminal
```

### manifest.json経由

`Packages/manifest.json` に以下を追加:
```json
{
  "dependencies": {
    "jp.xeon.uni-terminal": "https://github.com/AraiYuhki/UniTerminal.git?path=Packages/jp.xeon.uni-terminal"
  }
}
```

### UniTaskサポートを有効にする

UniTaskがプロジェクトにインストールされている場合、自動的にUniTaskサポートが有効になります。

1. UniTaskをインストール（OpenUPM経由推奨）:
```
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

2. UniTerminalがUniTaskを検出すると、`UNI_TERMINAL_UNI_TASK_SUPPORT` シンボルが自動定義されます

## 基本的な使い方

### Terminalの初期化

```csharp
using Xeon.UniTerminal;

// Terminalインスタンスを作成
var terminal = new Terminal(
    workingDirectory: Application.dataPath,
    homeDirectory: Application.dataPath,
    registerBuiltInCommands: true  // 組み込みコマンドを登録
);
```

### コマンドの実行

```csharp
using Xeon.UniTerminal;

// 出力用のIAsyncTextWriter
var stdout = new StringBuilderTextWriter();
var stderr = new StringBuilderTextWriter();

// コマンドを実行
var exitCode = await terminal.ExecuteAsync("echo Hello, World!", stdout, stderr);

// 結果を取得
Debug.Log(stdout.ToString());  // "Hello, World!"
```

### UniTaskを使用したコマンド実行

```csharp
using Cysharp.Threading.Tasks;
using Xeon.UniTerminal;

// UniTask版の非同期実行
var exitCode = await terminal.ExecuteUniTaskAsync("echo Hello!", stdout, stderr);
```

### パイプラインの使用

```csharp
// コマンドをパイプでつなげる
await terminal.ExecuteAsync("cat myfile.txt | grep --pattern=error | less", stdout, stderr);
```

### リダイレクト

```csharp
// ファイルへの出力
await terminal.ExecuteAsync("echo Hello > output.txt", stdout, stderr);

// ファイルへの追記
await terminal.ExecuteAsync("echo World >> output.txt", stdout, stderr);

// ファイルからの入力
await terminal.ExecuteAsync("grep --pattern=pattern < input.txt", stdout, stderr);
```

## コマンド一覧

### ファイル操作コマンド

| コマンド | 説明 | 主なオプション |
|---------|------|---------------|
| `pwd` | 現在の作業ディレクトリを表示 | `-L, --logical`, `-P, --physical` |
| `cd` | 作業ディレクトリを変更 | `-L, --logical`, `-P, --physical` |
| `ls` | ディレクトリ内容を一覧表示 | `-a, --all`, `-l, --long`, `-h, --human-readable`, `-r, --reverse`, `-R, --recursive`, `-S, --sort` |
| `cat` | ファイル内容を表示 | - |
| `find` | ファイルを検索 | `-n, --name`, `-i, --iname`, `-t, --type`, `-d, --maxdepth`, `--mindepth` |
| `less` | ファイルをページ単位で表示 | `-n, --lines`, `-f, --from-line`, `-N, --line-numbers`, `-S, --chop-long-lines` |
| `diff` | ファイルの差分を比較 | `-u, --unified`, `-i, --ignore-case`, `-b, --ignore-space`, `-w, --ignore-all-space`, `-q, --brief` |

### テキスト処理コマンド

| コマンド | 説明 | 主なオプション |
|---------|------|---------------|
| `echo` | テキストを出力 | `-n, --newline` |
| `grep` | パターンマッチング検索 | `-p, --pattern`, `-i, --ignorecase`, `-v, --invert`, `-c, --count` |

### ユーティリティコマンド

| コマンド | 説明 | 主なオプション |
|---------|------|---------------|
| `help` | ヘルプを表示 | - |
| `history` | コマンド履歴を管理 | `-c, --clear`, `-d, --delete`, `-n, --number`, `-r, --reverse` |
| `clear` | 画面表示をクリア | - |

### Unity固有コマンド

| コマンド | 説明 | 主なオプション |
|---------|------|---------------|
| `hierarchy` | シーンヒエラルキーを表示 | `-r, --recursive`, `-d, --depth`, `-a, --all`, `-l, --long`, `-s, --scene`, `-n, --name`, `-c, --component`, `-t, --tag`, `-y, --layer`, `-i, --id` |
| `go` | GameObjectを操作 | `-p, --primitive`, `--parent`, `--position`, `--rotation`, `-t, --tag`, `-n, --name`, `-c, --component`, `-i, --inactive`, `-s, --set`, `--toggle`, `--immediate`, `--children`, `--count` |
| `transform` | Transformを操作 | `-p, --position`, `-P, --local-position`, `-r, --rotation`, `-R, --local-rotation`, `-s, --scale`, `--parent`, `-w, --world` |
| `component` | コンポーネントを管理 | `-a, --all`, `-v, --verbose`, `-i, --immediate`, `-n, --namespace` |
| `property` | プロパティ値を操作 | `-a, --all`, `-s, --serialized`, `-n, --namespace` |
| `scene` | シーンを管理 | `-a, --all`, `-l, --long`, `--additive`, `--async`, `-s, --setup` |

## コマンド詳細

### hierarchy - シーンヒエラルキー表示

```bash
# シーン全体のルートオブジェクトを表示
hierarchy

# 再帰的に全オブジェクトを表示
hierarchy -r

# 詳細情報付きで表示
hierarchy -l

# インスタンスIDを表示（参照設定に使用）
hierarchy -i
# 出力例:
# ├── Player #12345
# ├── Enemy #12346
# └── Camera #12347

# 特定のパス以下を表示
hierarchy /Canvas/Panel

# フィルタリング
hierarchy -n "Player*"           # 名前でフィルタ（ワイルドカード対応）
hierarchy -c Rigidbody           # コンポーネントでフィルタ
hierarchy -t Player              # タグでフィルタ
hierarchy -y UI                  # レイヤーでフィルタ

# シーン一覧を表示
hierarchy -s list

# 特定シーンを表示
hierarchy -s MyScene
```

### go - GameObject操作

```bash
# 新しいGameObjectを作成
go create MyObject

# プリミティブを作成
go create Cube --primitive=Cube

# 親を指定して作成
go create Child --parent /Parent

# 位置/回転を指定して作成
go create Player --position 0,1,0 --rotation 0,90,0

# GameObjectを削除
go delete /MyObject

# GameObjectを検索（部分一致）
go find -n "Enemy"
go find -t Player
go find -c Rigidbody

# GameObjectの情報を表示
go info /Player

# 名前を変更
go rename /OldName NewName

# アクティブ状態を変更
go active /MyObject -s false
go active /MyObject --toggle

# 複製
go clone /Original -n Clone --count 5
```

### scene - シーン管理

```bash
# ロード済みシーン一覧
scene list

# Build Settingsの全シーン一覧（ロード状態も表示）
scene list -a

# シーンを読み込み（追加ロード + 非同期）
scene load GameScene --additive --async

# シーンをアンロード
scene unload GameScene

# アクティブシーンの取得/変更
scene active
scene active GameScene

# シーン情報
scene info
scene info GameScene

# シーン作成（エディタのみ）
scene create NewScene --setup
```

### transform - Transform操作

```bash
# 位置を設定
transform /MyObject -p 1,2,3              # ワールド座標
transform /MyObject -P 0,1,0              # ローカル座標

# 回転を設定（オイラー角）
transform /MyObject -r 0,90,0             # ワールド回転
transform /MyObject -R 45,0,0             # ローカル回転

# スケールを設定
transform /MyObject -s 2,2,2

# 親を設定
transform /Child --parent /Parent
transform /Child --parent null            # 親を解除

# 複合操作
transform /MyObject -p 0,0,0 -r 0,0,0 -s 1,1,1
```

### component - コンポーネント管理

```bash
# コンポーネント一覧を表示
component list /MyObject

# コンポーネントを追加
component add /MyObject Rigidbody
component add /MyObject BoxCollider

# コンポーネントを削除
component remove /MyObject Rigidbody

# コンポーネントの詳細情報を表示
component info /MyObject Rigidbody

# コンポーネントの有効/無効を切り替え
component enable /MyObject BoxCollider
component disable /MyObject BoxCollider
```

### property - プロパティ操作

```bash
# プロパティ一覧を表示
property list /MyObject Rigidbody

# プロパティ値を取得
property get /MyObject Rigidbody mass
property get /MyObject Transform position

# 複数プロパティを取得
property get /MyObject Rigidbody mass,useGravity,drag

# プロパティ値を設定
property set /MyObject Rigidbody mass 10
property set /MyObject Rigidbody useGravity false
property set /MyObject Transform position 1,2,3

# 配列要素にアクセス
property get /MyObject MeshRenderer sharedMaterials[0]

# 参照型の設定
property set /Child Transform parent /Parent
property set /MyObject Transform parent null
```

## カスタムコマンドの作成

### 標準のICommandインターフェース

```csharp
using Xeon.UniTerminal;
using System.Threading;
using System.Threading.Tasks;

[Command("mycommand", "My custom command description")]
public class MyCommand : ICommand
{
    [Option("message", "m", Description = "Message to display")]
    public string Message;

    [Option("count", "c", Description = "Repeat count")]
    public int Count = 1;

    public string CommandName => "mycommand";
    public string Description => "My custom command";

    public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
    {
        for (int i = 0; i < Count; i++)
        {
            await context.Stdout.WriteLineAsync(Message ?? "Hello!", ct);
        }
        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context)
    {
        yield break;  // 補完候補を返す場合はここに実装
    }
}
```

### UniTask対応コマンド

UniTaskを使用する場合は `IUniTaskCommand` インターフェースを実装します:

```csharp
using Cysharp.Threading.Tasks;
using Xeon.UniTerminal;

[Command("myasync", "UniTask-based async command")]
public class MyUniTaskCommand : IUniTaskCommand
{
    [Option("delay", "d", Description = "Delay in milliseconds")]
    public int Delay = 1000;

    public string CommandName => "myasync";
    public string Description => "UniTask-based async command";

    public async UniTask<ExitCode> ExecuteAsync(UniTaskCommandContext context, CancellationToken ct)
    {
        await context.Stdout.WriteLineAsync("Starting...", ct);
        await UniTask.Delay(Delay, cancellationToken: ct);
        await context.Stdout.WriteLineAsync("Done!", ct);
        return ExitCode.Success;
    }

    public IEnumerable<string> GetCompletions(CompletionContext context)
    {
        yield break;
    }
}
```

### 位置引数の扱い

コマンドに渡されたオプション以外の引数は「位置引数」として `context.PositionalArguments` に格納されます。

```csharp
// 例: echo Hello World
// → context.PositionalArguments = ["Hello", "World"]

public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
{
    // 位置引数の数をチェック
    if (context.PositionalArguments.Count == 0)
    {
        await context.Stderr.WriteLineAsync("引数が必要です", ct);
        return ExitCode.UsageError;
    }

    // 最初の位置引数を取得
    var firstArg = context.PositionalArguments[0];

    // すべての位置引数を連結
    var allArgs = string.Join(" ", context.PositionalArguments);

    return ExitCode.Success;
}
```

#### サブコマンドパターン

サブコマンドを持つコマンドでは、最初の位置引数をサブコマンドとして使用し、残りを引数として処理できます。

```csharp
// 例: go create MyObject -p Cube
// → PositionalArguments[0] = "create" (サブコマンド)
// → PositionalArguments[1] = "MyObject" (引数)

public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
{
    if (context.PositionalArguments.Count == 0)
    {
        await context.Stderr.WriteLineAsync("サブコマンドを指定してください", ct);
        return ExitCode.UsageError;
    }

    var subCommand = context.PositionalArguments[0].ToLower();
    var args = context.PositionalArguments.Skip(1).ToList();

    return subCommand switch
    {
        "create" => await CreateAsync(context, args, ct),
        "delete" => await DeleteAsync(context, args, ct),
        _ => ExitCode.UsageError
    };
}
```

### コマンドの登録

```csharp
// 手動で登録
terminal.Registry.Register<MyCommand>();

// または、アセンブリから自動登録
terminal.Registry.RegisterFromAssembly(typeof(MyCommand).Assembly);
```

## FlyweightScrollView

大量のログを効率的に表示するための仮想スクロールビューコンポーネントです。Flyweightパターンを使用し、表示に必要な最小限のUIアイテムのみを生成・再利用します。

### 特徴

- 大量データの効率的な表示（数万行のログも軽快に表示）
- 垂直/水平スクロール対応
- CircularBufferによる固定サイズのログバッファ
- ObservableCollectionとの連携

### 使用例

```csharp
using Xeon.Common.FlyweightScrollView;
using Xeon.Common.FlyweightScrollView.Model;

// CircularBufferを使用（最大1000行のログを保持）
var logBuffer = new CircularBuffer<string>(1000);

// スクロールビューにバインド
scrollView.Initialize<string, LogItemView>(logItemPrefab, logBuffer);

// ログを追加（バッファが満杯になると古いログが自動削除）
logBuffer.Add("New log entry");
```

## 終了コード

| コード | 説明 |
|--------|------|
| `ExitCode.Success` (0) | 正常終了 |
| `ExitCode.UsageError` (1) | 使用方法エラー |
| `ExitCode.RuntimeError` (2) | 実行時エラー |

## ライセンス

MIT OR Apache-2.0（デュアルライセンス）

詳細は [LICENSE.md](LICENSE.md) を参照してください。

## 作者

Xeon
