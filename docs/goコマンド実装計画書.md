# go コマンド実装計画書

## 1. 概要

`go` コマンドは、GameObjectの作成・削除・検索・操作を行うコマンドです。Unityシーン内のGameObjectをCLIから管理できます。

### 1.1 基本仕様

- **コマンド名:** `go`（gameobjectの略）
- **説明:** GameObjectの作成・削除・管理
- **書式:** `go <サブコマンド> [オプション] [引数...]`

### 1.2 サブコマンド一覧

| サブコマンド | 説明 |
|-------------|------|
| `create` | 新しいGameObjectを作成 |
| `delete` | GameObjectを削除 |
| `find` | GameObjectを検索 |
| `rename` | GameObjectの名前を変更 |
| `active` | アクティブ状態を変更 |
| `clone` | GameObjectを複製 |
| `info` | GameObjectの詳細情報を表示 |

### 1.3 Unity API

- `new GameObject()`
- `GameObject.CreatePrimitive()`
- `Object.Destroy()` / `Object.DestroyImmediate()`
- `GameObject.Find()` / `GameObject.FindWithTag()`
- `Object.Instantiate()`

---

## 2. サブコマンド仕様

### 2.1 create - GameObject作成

```bash
go create <名前> [オプション]
```

| オプション | 短縮形 | 型 | 説明 |
|-----------|--------|-----|------|
| `--primitive` | `-p` | enum | プリミティブタイプ（Cube, Sphere, etc.） |
| `--parent` | `-P` | string | 親オブジェクトのパス |
| `--position` | | Vector3 | 初期位置 |
| `--rotation` | | Vector3 | 初期回転（Euler角） |
| `--tag` | `-t` | string | タグ |
| `--layer` | `-l` | string/int | レイヤー |

**プリミティブタイプ:**
- `Cube`, `Sphere`, `Capsule`, `Cylinder`, `Plane`, `Quad`

**例:**
```bash
go create "MyObject"
go create "MyCube" --primitive Cube --position 0,1,0
go create "Child" --parent /Parent
```

### 2.2 delete - GameObject削除

```bash
go delete <パス> [オプション]
```

| オプション | 短縮形 | 型 | 説明 |
|-----------|--------|-----|------|
| `--immediate` | `-i` | bool | 即座に削除（DestroyImmediate） |
| `--children` | `-c` | bool | 子オブジェクトのみ削除 |

**例:**
```bash
go delete /Player
go delete /Canvas --children    # Canvasの子のみ削除
```

### 2.3 find - GameObject検索

```bash
go find <検索条件> [オプション]
```

| オプション | 短縮形 | 型 | 説明 |
|-----------|--------|-----|------|
| `--name` | `-n` | string | 名前で検索（部分一致） |
| `--tag` | `-t` | string | タグで検索 |
| `--component` | `-c` | string | 特定コンポーネントを持つ |
| `--inactive` | `-i` | bool | 非アクティブも含める |

**例:**
```bash
go find --name "Player"
go find --tag "Enemy"
go find --component Rigidbody
```

### 2.4 rename - 名前変更

```bash
go rename <パス> <新しい名前>
```

**例:**
```bash
go rename /OldName NewName
```

### 2.5 active - アクティブ状態変更

```bash
go active <パス> [オプション]
```

| オプション | 短縮形 | 型 | 説明 |
|-----------|--------|-----|------|
| `--set` | `-s` | bool | アクティブ状態を設定 |
| `--toggle` | `-t` | bool | アクティブ状態を切り替え |

**例:**
```bash
go active /Player                    # 現在の状態を表示
go active /Player --set true         # アクティブにする
go active /Player --set false        # 非アクティブにする
go active /Player --toggle           # 切り替え
```

### 2.6 clone - 複製

```bash
go clone <パス> [オプション]
```

| オプション | 短縮形 | 型 | 説明 |
|-----------|--------|-----|------|
| `--name` | `-n` | string | 複製後の名前 |
| `--parent` | `-p` | string | 複製先の親 |
| `--count` | `-c` | int | 複製数（デフォルト: 1） |

**例:**
```bash
go clone /Enemy --name "Enemy2"
go clone /Prefab --count 5
```

### 2.7 info - 詳細情報

```bash
go info <パス>
```

**出力例:**
```
GameObject: Player
  Path: /Player
  Active: true (self: true)
  Tag: Player
  Layer: Default (0)
  Static: false
  Transform:
    Position: (0.0, 1.0, 0.0)
    Rotation: (0.0, 0.0, 0.0)
    Scale: (1.0, 1.0, 1.0)
  Components (5):
    - Transform
    - PlayerController
    - Rigidbody
    - CapsuleCollider
    - Animator
  Children (2):
    - Body
    - Weapon
```

---

## 3. 出力形式

### 3.1 find コマンドの出力

```
Found 3 GameObjects:
  /Enemy1                    [Enemy] Active
  /SpawnPoint/Enemy2         [Enemy] Active
  /Pool/Enemy3               [Enemy] Inactive
```

### 3.2 create コマンドの出力

```
Created: /MyObject
```

### 3.3 delete コマンドの出力

```
Deleted: /MyObject
```

---

## 4. パス解決

hierarchyコマンドと共通のパス解決ユーティリティを使用します。

### 4.1 GameObjectPath ユーティリティ

```csharp
public static class GameObjectPath
{
    public static GameObject Resolve(string path);
    public static string GetPath(GameObject go);
    public static bool Exists(string path);
}
```

---

## 5. エラー処理

### 5.1 エラーケース

| エラー | メッセージ | 終了コード |
|--------|-----------|-----------|
| パスが存在しない | `go: '{path}': GameObject not found` | RuntimeError |
| 親が存在しない | `go: '{parent}': Parent not found` | RuntimeError |
| 無効なプリミティブ | `go: '{type}': Invalid primitive type` | UsageError |
| 名前が空 | `go: name cannot be empty` | UsageError |
| サブコマンドなし | `go: missing subcommand` | UsageError |

---

## 6. 実装詳細

### 6.1 クラス構造（サブコマンドパターン）

```csharp
namespace Xeon.UniTerminal.BuiltInCommands
{
    [Command("go", "Manage GameObjects")]
    public class GoCommand : ICommand
    {
        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
            {
                await context.Stderr.WriteLineAsync("go: missing subcommand", ct);
                return ExitCode.UsageError;
            }

            var subCommand = context.PositionalArguments[0];
            var args = context.PositionalArguments.Skip(1).ToList();

            return subCommand.ToLower() switch
            {
                "create" => await CreateAsync(context, args, ct),
                "delete" => await DeleteAsync(context, args, ct),
                "find" => await FindAsync(context, args, ct),
                "rename" => await RenameAsync(context, args, ct),
                "active" => await ActiveAsync(context, args, ct),
                "clone" => await CloneAsync(context, args, ct),
                "info" => await InfoAsync(context, args, ct),
                _ => await UnknownSubCommand(context, subCommand, ct)
            };
        }
    }
}
```

### 6.2 別案: 個別コマンドとして実装

サブコマンドが複雑になる場合は、個別コマンドとして分離することも検討：

```csharp
[Command("go-create", "Create a new GameObject")]
public class GoCreateCommand : ICommand { }

[Command("go-delete", "Delete a GameObject")]
public class GoDeleteCommand : ICommand { }
```

### 6.3 GameObject作成

```csharp
private async Task<ExitCode> CreateAsync(
    CommandContext context,
    List<string> args,
    CancellationToken ct)
{
    // 名前パラメータを解析
    string name = args.FirstOrDefault() ?? "GameObject";

    GameObject go;

    if (!string.IsNullOrEmpty(PrimitiveType))
    {
        if (!Enum.TryParse<PrimitiveType>(PrimitiveType, true, out var primitiveType))
        {
            await context.Stderr.WriteLineAsync(
                $"go: '{PrimitiveType}': Invalid primitive type", ct);
            return ExitCode.UsageError;
        }
        go = GameObject.CreatePrimitive(primitiveType);
        go.name = name;
    }
    else
    {
        go = new GameObject(name);
    }

    // 親の設定
    if (!string.IsNullOrEmpty(ParentPath))
    {
        var parent = GameObjectPath.Resolve(ParentPath);
        if (parent == null)
        {
            Object.DestroyImmediate(go);
            await context.Stderr.WriteLineAsync(
                $"go: '{ParentPath}': Parent not found", ct);
            return ExitCode.RuntimeError;
        }
        go.transform.SetParent(parent.transform, false);
    }

    // 位置・回転の設定
    if (Position.HasValue)
        go.transform.position = Position.Value;
    if (Rotation.HasValue)
        go.transform.eulerAngles = Rotation.Value;

    // タグ・レイヤー
    if (!string.IsNullOrEmpty(Tag))
        go.tag = Tag;
    if (Layer >= 0)
        go.layer = Layer;

    await context.Stdout.WriteLineAsync($"Created: {GameObjectPath.GetPath(go)}", ct);
    return ExitCode.Success;
}
```

### 6.4 GameObject検索

```csharp
private async Task<ExitCode> FindAsync(
    CommandContext context,
    List<string> args,
    CancellationToken ct)
{
    var results = new List<GameObject>();

    // 名前検索
    if (!string.IsNullOrEmpty(SearchName))
    {
        var all = Object.FindObjectsOfType<GameObject>(IncludeInactive);
        results.AddRange(all.Where(go =>
            go.name.IndexOf(SearchName, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    // タグ検索
    if (!string.IsNullOrEmpty(SearchTag))
    {
        var tagged = GameObject.FindGameObjectsWithTag(SearchTag);
        results.AddRange(tagged);
    }

    // コンポーネント検索
    if (!string.IsNullOrEmpty(SearchComponent))
    {
        var type = TypeResolver.ResolveComponentType(SearchComponent);
        if (type != null)
        {
            var withComponent = Object.FindObjectsOfType(type, IncludeInactive);
            results.AddRange(withComponent
                .Cast<Component>()
                .Select(c => c.gameObject));
        }
    }

    // 重複排除
    results = results.Distinct().ToList();

    if (results.Count == 0)
    {
        await context.Stdout.WriteLineAsync("No GameObjects found.", ct);
        return ExitCode.Success;
    }

    await context.Stdout.WriteLineAsync($"Found {results.Count} GameObjects:", ct);
    foreach (var go in results)
    {
        string active = go.activeInHierarchy ? "Active" : "Inactive";
        string path = GameObjectPath.GetPath(go);
        await context.Stdout.WriteLineAsync($"  {path,-30} [{go.tag}] {active}", ct);
    }

    return ExitCode.Success;
}
```

---

## 7. Undo対応（Editorのみ）

### 7.1 Editor環境でのUndo

```csharp
#if UNITY_EDITOR
private void RegisterUndo(GameObject go, string actionName)
{
    UnityEditor.Undo.RegisterCreatedObjectUndo(go, actionName);
}

private void RecordUndo(Object obj, string actionName)
{
    UnityEditor.Undo.RecordObject(obj, actionName);
}
#endif
```

---

## 8. テストケース

### 8.1 createテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| GO-001 | 空オブジェクト作成 | `go create "Test"` | Testが作成される |
| GO-002 | プリミティブ作成 | `go create "Cube" -p Cube` | Cubeが作成される |
| GO-003 | 親指定で作成 | `go create "Child" -P /Parent` | Parentの子として作成 |
| GO-004 | 位置指定 | `go create "Obj" --position 1,2,3` | 指定位置に作成 |

### 8.2 deleteテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| GO-010 | オブジェクト削除 | `go delete /Test` | Testが削除される |
| GO-011 | 子のみ削除 | `go delete /Parent -c` | 子のみ削除 |
| GO-012 | 存在しない削除 | `go delete /NoExist` | エラーメッセージ |

### 8.3 findテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| GO-020 | 名前検索 | `go find -n "Player"` | Playerを含むオブジェクト |
| GO-021 | タグ検索 | `go find -t "Enemy"` | Enemyタグのオブジェクト |
| GO-022 | コンポーネント検索 | `go find -c Rigidbody` | Rigidbody付きオブジェクト |

### 8.4 その他テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| GO-030 | 名前変更 | `go rename /Old New` | 名前が変更される |
| GO-031 | アクティブ切替 | `go active /Obj -t` | 状態が切り替わる |
| GO-032 | 複製 | `go clone /Obj` | オブジェクトが複製される |
| GO-033 | 情報表示 | `go info /Player` | 詳細情報が表示される |

---

## 9. 補完対応

### 9.1 補完ターゲット

- **サブコマンド:** create, delete, find, rename, active, clone, info
- **パス引数:** GameObjectパス補完
- **--primitive:** プリミティブタイプ補完
- **--tag:** 既存タグ補完
- **--layer:** レイヤー名補完

### 9.2 実装

```csharp
public IEnumerable<string> GetCompletions(CompletionContext context)
{
    // サブコマンド補完
    if (context.TokenIndex == 1)
    {
        var subCommands = new[] { "create", "delete", "find", "rename",
                                  "active", "clone", "info" };
        foreach (var cmd in subCommands)
        {
            if (cmd.StartsWith(context.WordToComplete, StringComparison.OrdinalIgnoreCase))
                yield return cmd;
        }
    }

    // プリミティブタイプ補完
    if (context.CurrentOption == "primitive")
    {
        var types = new[] { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };
        foreach (var type in types)
            yield return type;
    }

    // タグ補完
    if (context.CurrentOption == "tag")
    {
        foreach (var tag in UnityEditorInternal.InternalEditorUtility.tags)
            yield return tag;
    }
}
```

---

## 10. 実装スケジュール

### Phase 1（必須機能）
1. create（空オブジェクト、プリミティブ）
2. delete
3. find（名前検索）
4. info

### Phase 2（拡張機能）
1. create（親指定、位置・回転）
2. find（タグ、コンポーネント検索）
3. rename
4. active

### Phase 3（将来拡張）
1. clone
2. Undo対応
3. 補完対応
