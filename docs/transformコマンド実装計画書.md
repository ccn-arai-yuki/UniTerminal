# transform コマンド実装計画書

## 1. 概要

`transform` コマンドは、GameObjectのTransformコンポーネントを操作するコマンドです。位置、回転、スケール、親子関係の取得・設定ができます。

### 1.1 基本仕様

- **コマンド名:** `transform`（エイリアス: `tf`）
- **説明:** GameObjectのTransformを操作
- **書式:** `transform <パス> [オプション]`

### 1.2 Unity API

- `Transform.position` / `localPosition`
- `Transform.rotation` / `localRotation` / `eulerAngles`
- `Transform.localScale`
- `Transform.SetParent()`
- `Transform.SetSiblingIndex()`

---

## 2. オプション仕様

### 2.1 実装するオプション（Phase 1）

| オプション | 短縮形 | 型 | 説明 |
|-----------|--------|-----|------|
| `--position` | `-p` | Vector3 | ワールド位置を設定 |
| `--local-position` | `-P` | Vector3 | ローカル位置を設定 |
| `--rotation` | `-r` | Vector3 | ワールド回転を設定（Euler角） |
| `--local-rotation` | `-R` | Vector3 | ローカル回転を設定（Euler角） |
| `--scale` | `-s` | Vector3 | ローカルスケールを設定 |
| `--parent` | | string | 親オブジェクトを変更 |
| `--world` | `-w` | bool | 親変更時にワールド座標を維持 |

### 2.2 将来的に実装を検討するオプション（Phase 2）

| オプション | 短縮形 | 説明 |
|-----------|--------|------|
| `--look-at` | | 指定オブジェクトの方向を向く |
| `--translate` | | 現在位置から相対移動 |
| `--rotate` | | 現在回転から相対回転 |
| `--sibling` | | 兄弟順序を変更 |
| `--reset` | | Transform をリセット |

---

## 3. 出力形式

### 3.1 情報表示（オプションなし）

```bash
transform /Player
```

```
Transform: Player
  World Position:  (10.5, 1.0, -3.2)
  Local Position:  (0.0, 1.0, 0.0)
  World Rotation:  (0.0, 45.0, 0.0)
  Local Rotation:  (0.0, 45.0, 0.0)
  Local Scale:     (1.0, 1.0, 1.0)
  Parent:          /World
  Children:        3
  Sibling Index:   2
```

### 3.2 値設定後の出力

```bash
transform /Player --position 0,5,0
```

```
Transform: Player
  Position: (10.5, 1.0, -3.2) -> (0.0, 5.0, 0.0)
```

### 3.3 親変更後の出力

```bash
transform /Player --parent /NewParent
```

```
Transform: Player
  Parent: /World -> /NewParent
```

---

## 4. Vector3 パース

### 4.1 入力形式

| 形式 | 例 | 説明 |
|------|-----|------|
| カンマ区切り | `1,2,3` | x=1, y=2, z=3 |
| スペース区切り | `"1 2 3"` | クォート必須 |
| 単一値 | `5` | x=5, y=5, z=5 |
| 2値 | `1,2` | x=1, y=2, z=0 |

### 4.2 パース実装

```csharp
public static Vector3 ParseVector3(string input)
{
    if (string.IsNullOrEmpty(input))
        throw new ArgumentException("Vector3 input cannot be empty");

    var parts = input.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

    return parts.Length switch
    {
        1 => new Vector3(float.Parse(parts[0]), float.Parse(parts[0]), float.Parse(parts[0])),
        2 => new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), 0),
        3 => new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2])),
        _ => throw new ArgumentException($"Invalid Vector3 format: {input}")
    };
}
```

---

## 5. エラー処理

### 5.1 エラーケース

| エラー | メッセージ | 終了コード |
|--------|-----------|-----------|
| パスが存在しない | `transform: '{path}': GameObject not found` | RuntimeError |
| 親が存在しない | `transform: '{parent}': Parent not found` | RuntimeError |
| 無効なVector3 | `transform: invalid vector3: '{value}'` | UsageError |
| 循環参照 | `transform: cannot set parent to self or descendant` | RuntimeError |

---

## 6. 実装詳細

### 6.1 クラス構造

```csharp
namespace Xeon.UniTerminal.BuiltInCommands
{
    [Command("transform", "Manipulate GameObject Transform")]
    public class TransformCommand : ICommand
    {
        [Option("position", "p", Description = "Set world position")]
        public string Position;

        [Option("local-position", "P", Description = "Set local position")]
        public string LocalPosition;

        [Option("rotation", "r", Description = "Set world rotation (euler angles)")]
        public string Rotation;

        [Option("local-rotation", "R", Description = "Set local rotation (euler angles)")]
        public string LocalRotation;

        [Option("scale", "s", Description = "Set local scale")]
        public string Scale;

        [Option("parent", "", Description = "Set parent object")]
        public string Parent;

        [Option("world", "w", Description = "Maintain world position when changing parent")]
        public bool WorldPositionStays = true;

        public string CommandName => "transform";
        public string Description => "Manipulate GameObject Transform";

        // 実装...
    }
}
```

### 6.2 メイン処理

```csharp
public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
{
    if (context.PositionalArguments.Count == 0)
    {
        await context.Stderr.WriteLineAsync("transform: missing path argument", ct);
        return ExitCode.UsageError;
    }

    var path = context.PositionalArguments[0];
    var go = GameObjectPath.Resolve(path, context.WorkingDirectory);

    if (go == null)
    {
        await context.Stderr.WriteLineAsync($"transform: '{path}': GameObject not found", ct);
        return ExitCode.RuntimeError;
    }

    var transform = go.transform;
    bool modified = false;

    // 各オプションを処理
    if (!string.IsNullOrEmpty(Position))
    {
        var oldPos = transform.position;
        transform.position = ParseVector3(Position);
        await context.Stdout.WriteLineAsync(
            $"  Position: {FormatVector3(oldPos)} -> {FormatVector3(transform.position)}", ct);
        modified = true;
    }

    if (!string.IsNullOrEmpty(LocalPosition))
    {
        var oldPos = transform.localPosition;
        transform.localPosition = ParseVector3(LocalPosition);
        await context.Stdout.WriteLineAsync(
            $"  Local Position: {FormatVector3(oldPos)} -> {FormatVector3(transform.localPosition)}", ct);
        modified = true;
    }

    // ... 他のオプション処理

    // 変更なしの場合は情報表示
    if (!modified)
    {
        await DisplayTransformInfo(context, go, ct);
    }

    return ExitCode.Success;
}
```

### 6.3 情報表示

```csharp
private async Task DisplayTransformInfo(
    CommandContext context,
    GameObject go,
    CancellationToken ct)
{
    var t = go.transform;

    await context.Stdout.WriteLineAsync($"Transform: {go.name}", ct);
    await context.Stdout.WriteLineAsync($"  World Position:  {FormatVector3(t.position)}", ct);
    await context.Stdout.WriteLineAsync($"  Local Position:  {FormatVector3(t.localPosition)}", ct);
    await context.Stdout.WriteLineAsync($"  World Rotation:  {FormatVector3(t.eulerAngles)}", ct);
    await context.Stdout.WriteLineAsync($"  Local Rotation:  {FormatVector3(t.localEulerAngles)}", ct);
    await context.Stdout.WriteLineAsync($"  Local Scale:     {FormatVector3(t.localScale)}", ct);

    string parentPath = t.parent != null ? GameObjectPath.GetPath(t.parent.gameObject) : "(none)";
    await context.Stdout.WriteLineAsync($"  Parent:          {parentPath}", ct);
    await context.Stdout.WriteLineAsync($"  Children:        {t.childCount}", ct);
    await context.Stdout.WriteLineAsync($"  Sibling Index:   {t.GetSiblingIndex()}", ct);
}

private string FormatVector3(Vector3 v)
{
    return $"({v.x:F1}, {v.y:F1}, {v.z:F1})";
}
```

### 6.4 親の変更

```csharp
private async Task<ExitCode> SetParent(
    Transform transform,
    string parentPath,
    CommandContext context,
    CancellationToken ct)
{
    Transform newParent = null;

    if (parentPath != "/" && parentPath != "null" && parentPath != "none")
    {
        var parentGo = GameObjectPath.Resolve(parentPath, context.WorkingDirectory);
        if (parentGo == null)
        {
            await context.Stderr.WriteLineAsync(
                $"transform: '{parentPath}': Parent not found", ct);
            return ExitCode.RuntimeError;
        }

        // 循環参照チェック
        if (parentGo.transform.IsChildOf(transform))
        {
            await context.Stderr.WriteLineAsync(
                "transform: cannot set parent to self or descendant", ct);
            return ExitCode.RuntimeError;
        }

        newParent = parentGo.transform;
    }

    string oldParent = transform.parent != null
        ? GameObjectPath.GetPath(transform.parent.gameObject)
        : "(none)";

    transform.SetParent(newParent, WorldPositionStays);

    string newParentStr = newParent != null
        ? GameObjectPath.GetPath(newParent.gameObject)
        : "(none)";

    await context.Stdout.WriteLineAsync($"  Parent: {oldParent} -> {newParentStr}", ct);

    return ExitCode.Success;
}
```

---

## 7. Undo対応（Editorのみ）

```csharp
#if UNITY_EDITOR
private void RecordUndo(Transform transform, string action)
{
    UnityEditor.Undo.RecordObject(transform, $"Transform {action}");
}
#endif
```

---

## 8. テストケース

### 8.1 情報表示テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| TF-001 | 情報表示 | `transform /Player` | Transform情報が表示される |
| TF-002 | 存在しないパス | `transform /NoExist` | エラーメッセージ |

### 8.2 位置設定テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| TF-010 | ワールド位置設定 | `transform /Obj -p 1,2,3` | 位置が(1,2,3)になる |
| TF-011 | ローカル位置設定 | `transform /Obj -P 0,0,0` | ローカル位置が原点に |
| TF-012 | 単一値位置 | `transform /Obj -p 5` | 位置が(5,5,5)になる |

### 8.3 回転設定テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| TF-020 | 回転設定 | `transform /Obj -r 0,90,0` | Y軸90度回転 |
| TF-021 | ローカル回転 | `transform /Obj -R 45,0,0` | ローカルX軸45度 |

### 8.4 スケール設定テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| TF-030 | スケール設定 | `transform /Obj -s 2,2,2` | 2倍スケール |
| TF-031 | 非均等スケール | `transform /Obj -s 1,2,1` | Y軸のみ2倍 |

### 8.5 親変更テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| TF-040 | 親変更 | `transform /Obj --parent /NewParent` | 親が変更される |
| TF-041 | 親解除 | `transform /Obj --parent /` | ルートに移動 |
| TF-042 | 循環参照 | `transform /Parent --parent /Parent/Child` | エラー |
| TF-043 | ワールド座標維持 | `transform /Obj --parent /P -w` | 位置維持で親変更 |

### 8.6 複合テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| TF-050 | 複数設定 | `transform /Obj -p 0,0,0 -r 0,0,0 -s 1,1,1` | 全て設定される |

---

## 9. 補完対応

### 9.1 補完ターゲット

- **位置引数:** GameObjectパス補完
- **--parent:** GameObjectパス補完

### 9.2 実装

```csharp
public IEnumerable<string> GetCompletions(CompletionContext context)
{
    // パス補完（hierarchyと共通）
    if (!context.WordToComplete.StartsWith("-"))
    {
        foreach (var path in GameObjectPath.GetCompletions(context.WordToComplete))
        {
            yield return path;
        }
    }
}
```

---

## 10. 実装スケジュール

### Phase 1（必須機能）
1. 情報表示
2. ワールド/ローカル位置設定
3. 回転設定（Euler角）
4. スケール設定

### Phase 2（拡張機能）
1. 親の変更
2. ワールド座標維持オプション
3. Vector3パースの拡張

### Phase 3（将来拡張）
1. 相対移動（--translate）
2. 相対回転（--rotate）
3. LookAt
4. 兄弟順序変更
5. Undo対応
