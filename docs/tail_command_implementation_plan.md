# Tail Command Implementation Plan

## 概要

Unityのログファイルを表示・監視する`tail`コマンドを実装します。
- 最後のN行を表示（デフォルト10行）
- `-f` オプションでリアルタイム監視
- 監視中はQキーで終了

## ユーザー要件

1. ログを表示するコマンドの追加
2. tail -f のような監視オプション
3. qキーでの終了

**重要**: ユーザーはUnityのログ（`Application.consoleLogPath`）を監視したい

## 実装方針

### アーキテクチャ拡張: IInteractiveKeyHandler

コマンド実行中のキー入力を処理するため、新しいインターフェースを導入します。
- **利点**: テスト可能、拡張性が高い、将来の対話型コマンドに再利用可能
- **トレードオフ**: 複数ファイルの修正が必要

### ファイル監視: ポーリング方式

- FileSystemWatcherではなくポーリングを採用（Unity環境での安定性）
- 間隔: 200ms（CPU負荷と応答性のバランス）
- `FileShare.ReadWrite`でファイルロック競合を回避

## 実装内容

### 1. 新規作成するファイル

#### IInteractiveKeyHandler.cs
**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Core/IInteractiveKeyHandler.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Xeon.UniTerminal
{
    /// <summary>
    /// コマンド実行中のインタラクティブなキー入力を処理するインターフェース
    /// </summary>
    public interface IInteractiveKeyHandler
    {
        /// <summary>
        /// 指定したキーが押されるまで待機
        /// </summary>
        Task<KeyCode> WaitForKeyAsync(KeyCode[] acceptedKeys, CancellationToken ct);
    }
}
```

#### TailCommand.cs
**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/BuiltInCommands/TailCommand.cs`

**機能**:
- `-n, --lines <N>`: 表示行数（デフォルト10）
- `-f, --follow`: リアルタイム監視モード
- 引数なしの場合: `Application.consoleLogPath`（Unityログ）を表示
- 引数ありの場合: 指定ファイルを表示

**実装ポイント**:
- `ReadLastNLinesAsync()`: ブロック単位の逆読み（4KB単位）
  - 大きなログファイルでもメモリ効率的
  - UTF-8 エンコーディング対応
- `MonitorFileAsync()`: 200ms間隔でポーリング
  - ファイル切り詰め検出（ログローテーション対応）
  - 新しい行のみを出力
- `WaitForQuitKeyAsync()`: IInteractiveKeyHandlerを使用してQキー待機

**フォローモードの処理フロー**:
```csharp
var quitTask = WaitForQuitKeyAsync(context, ct);
var monitorTask = MonitorFileAsync(resolvedPath, lastPosition, context, ct);
var completedTask = await Task.WhenAny(quitTask, monitorTask);
// どちらかが完了したら終了
```

### 2. 修正するファイル

#### CommandContext.cs
**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Core/CommandContext.cs`

**変更内容**:
- `IInteractiveKeyHandler InteractiveKeyHandler { get; }` プロパティを追加
- コンストラクタに `IInteractiveKeyHandler interactiveKeyHandler = null` パラメータ追加

```csharp
// 追加するプロパティ
public IInteractiveKeyHandler InteractiveKeyHandler { get; }

// コンストラクタに追加
public CommandContext(
    // ... 既存のパラメータ ...
    IInteractiveKeyHandler interactiveKeyHandler = null)
{
    // ... 既存の初期化 ...
    InteractiveKeyHandler = interactiveKeyHandler;
}
```

#### Terminal.cs
**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Terminal.cs`

**変更内容**:
- `ExecuteAsync()`に `IInteractiveKeyHandler keyHandler = null` パラメータ追加
- `PipelineExecutor`にkeyHandlerを渡す
- `RegisterBuiltInCommands()`に`TailCommand`を追加

#### PipelineExecutor.cs
**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Execution/PipelineExecutor.cs`

**変更内容**:
- コンストラクタまたは`ExecuteAsync()`に`IInteractiveKeyHandler`を追加
- `CommandContext`生成時にkeyHandlerを渡す

#### UniTerminal.cs
**パス**: `Packages/jp.xeon.uni-terminal/Sample/Scripts/UniTerminal.cs`

**変更内容**:

1. **InteractiveKeyHandlerImpl クラスを追加**（内部クラス）:
```csharp
private class InteractiveKeyHandlerImpl : IInteractiveKeyHandler
{
    private TaskCompletionSource<KeyCode> tcs;
    private KeyCode[] acceptedKeys;

    public Task<KeyCode> WaitForKeyAsync(KeyCode[] keys, CancellationToken ct)
    {
        tcs = new TaskCompletionSource<KeyCode>();
        acceptedKeys = keys;
        ct.Register(() => tcs.TrySetCanceled());
        return tcs.Task;
    }

    public void CheckKeys()
    {
        if (tcs == null || acceptedKeys == null) return;

        foreach (var key in acceptedKeys)
        {
            if (IsKeyPressed(key))
            {
                tcs.TrySetResult(key);
                tcs = null;
                acceptedKeys = null;
                return;
            }
        }
    }

    private bool IsKeyPressed(KeyCode key)
    {
        // InputHandlerのパターンを使用
        var result = false;
        #if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            var keyObj = keyboard[key];
            result = keyObj != null && keyObj.wasPressedThisFrame;
        }
        #endif
        #if ENABLE_LEGACY_INPUT_MANAGER
        result |= UnityEngine.Input.GetKeyDown(key);
        #endif
        return result;
    }
}
```

2. **Update()メソッドを修正**:
```csharp
private InteractiveKeyHandlerImpl interactiveKeyHandler = new InteractiveKeyHandlerImpl();

private async void Update()
{
    // 既存の処理...

    // 対話的キー入力のチェック（コマンド実行中でも動作）
    interactiveKeyHandler.CheckKeys();

    // input.interactable && input.isFocused の既存チェック
    if (!input.isFocused || !input.interactable)
        return;

    // 既存のタブ補完、履歴ナビゲーション...
}
```

3. **OnInputCommand()メソッドを修正**:
```csharp
await terminal.ExecuteAsync(
    command,
    normalOutput,
    errorOutput,
    keyHandler: interactiveKeyHandler,  // 追加
    ct: destroyCancellationToken);
```

#### InputHandler.cs
**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Common/InputHandler.cs`

**変更内容**:
- `IsPressedQ()` メソッドを追加（既存パターンに従う）

```csharp
public static bool IsPressedQ()
{
    var result = false;
    #if ENABLE_INPUT_SYSTEM
    result = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
    #endif
    #if ENABLE_LEGACY_INPUT_MANAGER
    result |= Input.GetKeyDown(KeyCode.Q);
    #endif
    return result;
}
```

### 3. UniTask対応（オプション）

**必要な場合**:
- `TerminalUniTaskExtensions.cs`に`keyHandler`パラメータ追加
- `UniTaskPipelineExecutor.cs`に`keyHandler`パラメータ追加

## 技術的な課題と解決策

### 課題1: 実行中のキー入力処理
**問題**: コマンド実行中は`input.interactable = false`のため通常はキー入力が処理されない

**解決**: `Update()`内でInteractiveKeyHandlerを常にチェック

### 課題2: ファイル読み込み効率
**問題**: 大きなログファイルから最後のN行だけを取得

**解決**: 末尾からブロック単位（4KB）で逆読みして必要な行数を取得

### 課題3: ログローテーション対応
**問題**: 監視中にログファイルが切り詰められる可能性

**解決**: ファイルサイズが減少した場合は先頭から再読み込み

### 課題4: Unityログパスの取得
**問題**: `Application.consoleLogPath`はランタイムでのみ有効

**解決**:
```csharp
var defaultPath = Application.consoleLogPath;
if (string.IsNullOrEmpty(defaultPath))
{
    await context.Stderr.WriteLineAsync("tail: Unity log path not available", ct);
    return ExitCode.RuntimeError;
}
```

## 実装順序

### Phase 1: インターフェース基盤
1. `IInteractiveKeyHandler.cs`を作成
2. `CommandContext.cs`にプロパティ追加
3. `Terminal.cs`にパラメータ追加
4. `PipelineExecutor.cs`にパラメータ追加

### Phase 2: UI層の実装
1. `InputHandler.cs`に`IsPressedQ()`追加
2. `UniTerminal.cs`に`InteractiveKeyHandlerImpl`追加
3. `Update()`の修正
4. `OnInputCommand()`の修正

### Phase 3: TailCommand実装
1. `TailCommand.cs`のスケルトン作成
2. 通常モード（-n オプション）実装
   - `ReadLastNLinesAsync()`実装
   - オプション定義
3. フォローモード（-f オプション）実装
   - `MonitorFileAsync()`実装
   - `WaitForQuitKeyAsync()`実装
4. エラーハンドリング

### Phase 4: コマンド登録とテスト
1. `Terminal.RegisterBuiltInCommands()`に登録
2. Unity Editorで動作確認
3. エッジケースのテスト

## 検証方法

### 基本機能テスト

1. **引数なしで実行**:
   ```
   tail
   ```
   → Unityログの最後10行を表示

2. **行数指定**:
   ```
   tail -n 20
   ```
   → Unityログの最後20行を表示

3. **ファイル指定**:
   ```
   tail -n 5 test.log
   ```
   → 指定ファイルの最後5行を表示

4. **フォローモード**:
   ```
   tail -f
   ```
   → Unityログをリアルタイム監視
   → Debug.Log()を実行して新しい行が表示されるか確認
   → Qキーで終了

5. **パイプライン**:
   ```
   cat test.log | tail -n 3
   ```
   → 標準入力から最後3行を表示

### エッジケーステスト

1. **存在しないファイル**: エラーメッセージ表示
2. **空ファイル**: 何も表示せず正常終了
3. **1行だけのファイル**: 1行表示
4. **要求行数より少ないファイル**: 全行表示
5. **フォロー中のCtrl+C（CancellationToken）**: 正常終了

## CLAUDE.md準拠

- **ネスト制限**: 最大3レベル（namespace/class/method/ループ）
- **switch制限**: 使用しない
- **可読性優先**: メソッドを分割、明確な命名
- **制御文スタイル**: 2行形式、ブレース使用

## ファイル一覧

### 新規作成
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Core/IInteractiveKeyHandler.cs`
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/BuiltInCommands/TailCommand.cs`

### 修正
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Core/CommandContext.cs`
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Terminal.cs`
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Execution/PipelineExecutor.cs`
- `Packages/jp.xeon.uni-terminal/Sample/Scripts/UniTerminal.cs`
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Common/InputHandler.cs`

### オプション（UniTask対応が有効な場合）
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/UniTask/TerminalUniTaskExtensions.cs`
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Execution/UniTaskPipelineExecutor.cs`

## 実装後の拡張可能性

この設計により以下が可能になります：
- 他の対話型コマンド（top, watch等）の実装
- 複数キーの同時監視
- キーバインディングのカスタマイズ
- テスト容易性の向上
