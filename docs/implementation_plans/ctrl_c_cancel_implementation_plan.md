# Ctrl+C Cancel Implementation Plan

## 概要

コマンド実行中に`Ctrl+C`でキャンセルできる機能を実装します。
Linuxターミナルの標準的な動作を再現し、long-runningコマンド（`tail -f`など）を終了できるようにします。

## 現状の問題

現在のUniTerminalでは：
- コマンド実行中は`input.interactable = false`となり、キー入力を受け付けない
- `destroyCancellationToken`はオブジェクト破棄時のみキャンセルされる
- 実行中のコマンドをユーザーが中断する手段がない

## 実装方針

### 設計方針

- Linuxターミナルの`Ctrl+C`（SIGINT）動作を再現
- 既存のCancellationToken機構を活用
- コマンド実行中でもキー入力を監視

### アーキテクチャ

```
Update()
    ↓
Ctrl+C検出 → commandCancellationTokenSource.Cancel()
    ↓
ExecuteAsync() が OperationCanceledException をスロー
    ↓
"^C" を表示して正常終了
```

## 実装内容

### 修正ファイル

#### 1. InputHandler.cs

**パス**: `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Common/InputHandler.cs`

**変更内容**: `IsPressedCtrlC()`メソッドを追加

```csharp
public static bool IsPressedCtrlC()
{
    var result = false;
#if ENABLE_INPUT_SYSTEM
    var keyboard = Keyboard.current;
    if (keyboard != null)
    {
        var ctrlPressed = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        var cPressed = keyboard.cKey.wasPressedThisFrame;
        result = ctrlPressed && cPressed;
    }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
    var ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    var cPressed = Input.GetKeyDown(KeyCode.C);
    result |= ctrlPressed && cPressed;
#endif
    return result;
}
```

#### 2. UniTerminal.cs

**パス**: `Packages/jp.xeon.uni-terminal/Sample/Scripts/UniTerminal.cs`

**変更内容**:

1. コマンド実行用の`CancellationTokenSource`フィールドを追加
2. `Update()`でCtrl+Cを監視
3. `OnInputCommand()`でCancellationTokenSourceを管理
4. キャンセル時に`^C`を表示

```csharp
// フィールド追加
private CancellationTokenSource commandCancellationTokenSource;

// Update()に追加（input.isFocusedチェックの前に配置）
private async void Update()
{
    // ディレクトリ表示の更新...
    // maxCharsPerLineの計算...

    // Ctrl+C: コマンド実行中のキャンセル
    if (InputHandler.IsPressedCtrlC() && commandCancellationTokenSource != null)
    {
        commandCancellationTokenSource.Cancel();
        return;
    }

    // 入力フィールドがアクティブでない場合は無視
    if (!input.isFocused || !input.interactable)
        return;

    // Tab補完、履歴ナビゲーション...
}

// OnInputCommand()の修正
private async void OnInputCommand(string command)
{
    // 既存のバリデーション...

    try
    {
        input.interactable = false;
        await normalOutput.WriteAsync($"> {command}");
        ScrollToBottom();
        input.DeactivateInputField();

        // コマンド実行用のCancellationTokenSourceを作成
        commandCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

        try
        {
#if UNI_TERMINAL_UNI_TASK_SUPPORT
            await terminal.ExecuteUniTaskAsync(command, normalOutput, errorOutput, ct: commandCancellationTokenSource.Token);
#else
            await terminal.ExecuteAsync(command, normalOutput, errorOutput, ct: commandCancellationTokenSource.Token);
#endif
        }
        catch (OperationCanceledException) when (!destroyCancellationToken.IsCancellationRequested)
        {
            // Ctrl+Cによるキャンセル
            await normalOutput.WriteAsync("^C");
        }
    }
    catch (OperationCanceledException)
    {
        // destroyCancellationTokenによるキャンセル（アプリ終了時など）
    }
    finally
    {
        commandCancellationTokenSource?.Dispose();
        commandCancellationTokenSource = null;

        ScrollToBottom();
        input.text = string.Empty;
        input.interactable = true;
        FocusInputFieldAsync().Forget();
    }
}
```

## 技術詳細

### CancellationTokenSource.CreateLinkedTokenSource

```csharp
commandCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
```

- `destroyCancellationToken`と連動したCancellationTokenSourceを作成
- オブジェクト破棄時は`destroyCancellationToken`経由でキャンセル
- Ctrl+C時は`commandCancellationTokenSource.Cancel()`でキャンセル
- どちらの場合も`OperationCanceledException`がスローされる

### キャンセル判定

```csharp
catch (OperationCanceledException) when (!destroyCancellationToken.IsCancellationRequested)
{
    // Ctrl+Cによるキャンセルのみここに到達
    await normalOutput.WriteAsync("^C");
}
```

- `when`句で`destroyCancellationToken`のキャンセルと区別
- Ctrl+Cの場合のみ`^C`を表示

### Update()でのCtrl+C検出位置

```csharp
// Ctrl+C: コマンド実行中のキャンセル
if (InputHandler.IsPressedCtrlC() && commandCancellationTokenSource != null)
{
    commandCancellationTokenSource.Cancel();
    return;
}

// 入力フィールドがアクティブでない場合は無視
if (!input.isFocused || !input.interactable)
    return;
```

- `input.interactable`チェックの**前**に配置
- コマンド実行中（`interactable = false`）でもCtrl+Cを検出可能

## 実装順序

1. `InputHandler.cs`に`IsPressedCtrlC()`を追加
2. `UniTerminal.cs`にフィールドを追加
3. `Update()`にCtrl+C検出を追加
4. `OnInputCommand()`を修正
5. 動作確認

## 検証方法

### 基本テスト

```bash
# 長時間実行されるコマンドを実行
tail -f test.log

# Ctrl+Cを押す
# → "^C" が表示されてコマンドが終了
# → 入力フィールドが再びアクティブになる
```

### エッジケーステスト

1. **通常のコマンド実行**: Ctrl+Cなしで正常終了
2. **即座にCtrl+C**: コマンド開始直後にキャンセル
3. **複数回Ctrl+C**: 2回目以降は無視される（commandCancellationTokenSourceがnull）
4. **アプリ終了時**: destroyCancellationTokenでキャンセルされ、^Cは表示されない

## ファイル一覧

### 修正
- `Packages/jp.xeon.uni-terminal/Runtime/Scripts/Common/InputHandler.cs`
- `Packages/jp.xeon.uni-terminal/Sample/Scripts/UniTerminal.cs`

## CLAUDE.md準拠

- **ネスト制限**: 最大3レベル（namespace除く）
- **switch制限**: 使用しない
- **可読性優先**: 条件分岐を明確に
- **制御文スタイル**: 2行形式を使用

## 依存関係

この機能は以下のコマンドで活用されます：
- `tail -f` - ファイル監視の終了
- 将来の対話型コマンド（`top`, `watch`など）
