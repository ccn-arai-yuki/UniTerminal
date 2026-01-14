# component コマンド実装計画書

## 1. 概要

`component` コマンドは、GameObjectにアタッチされているコンポーネントの確認・追加・削除を行うコマンドです。

### 1.1 基本仕様

- **コマンド名:** `component`（エイリアス: `comp`）
- **説明:** コンポーネントの管理
- **書式:** `component <サブコマンド> <パス> [引数...]`

### 1.2 サブコマンド一覧

| サブコマンド | 説明 |
|-------------|------|
| `list` | コンポーネント一覧を表示 |
| `add` | コンポーネントを追加 |
| `remove` | コンポーネントを削除 |
| `info` | コンポーネントの詳細情報を表示 |
| `enable` | コンポーネントを有効化 |
| `disable` | コンポーネントを無効化 |

### 1.3 Unity API

- `GameObject.GetComponents<Component>()`
- `GameObject.AddComponent(Type)`
- `Object.Destroy(Component)`
- `Behaviour.enabled`

---

## 2. サブコマンド仕様

### 2.1 list - コンポーネント一覧

```bash
component list <パス> [オプション]
```

| オプション | 短縮形 | 型 | 説明 |
|-----------|--------|-----|------|
| `--all` | `-a` | bool | システムコンポーネントも表示 |
| `--verbose` | `-v` | bool | 詳細情報を表示 |

**出力例:**
```bash
component list /Player
```

```
Components on Player (5):
  [0] Transform
  [1] PlayerController (enabled)
  [2] Rigidbody
  [3] CapsuleCollider
  [4] Animator (enabled)
```

**詳細表示:**
```bash
component list /Player -v
```

```
Components on Player (5):
  [0] Transform                    UnityEngine.Transform
  [1] PlayerController (enabled)   MyGame.PlayerController
  [2] Rigidbody                    UnityEngine.Rigidbody
  [3] CapsuleCollider              UnityEngine.CapsuleCollider
  [4] Animator (enabled)           UnityEngine.Animator
```

### 2.2 add - コンポーネント追加

```bash
component add <パス> <コンポーネント名> [オプション]
```

| オプション | 短縮形 | 型 | 説明 |
|-----------|--------|-----|------|
| `--namespace` | `-n` | string | 名前空間を指定 |

**例:**
```bash
component add /Player Rigidbody
component add /Player MyController --namespace MyGame
```

**出力:**
```
Added: Rigidbody to /Player
```

### 2.3 remove - コンポーネント削除

```bash
component remove <パス> <コンポーネント名|インデックス> [オプション]
```

| オプション | 短縮形 | 型 | 説明 |
|-----------|--------|-----|------|
| `--immediate` | `-i` | bool | 即座に削除（DestroyImmediate） |
| `--all` | `-a` | bool | 同名のコンポーネントをすべて削除 |

**例:**
```bash
component remove /Player Rigidbody
component remove /Player 2            # インデックス指定
component remove /Player BoxCollider --all
```

**出力:**
```
Removed: Rigidbody from /Player
```

### 2.4 info - コンポーネント情報

```bash
component info <パス> <コンポーネント名|インデックス>
```

**出力例:**
```bash
component info /Player Rigidbody
```

```
Component: Rigidbody
  Type: UnityEngine.Rigidbody
  GameObject: /Player
  Properties:
    mass: 1 (float)
    drag: 0 (float)
    angularDrag: 0.05 (float)
    useGravity: true (bool)
    isKinematic: false (bool)
    interpolation: None (RigidbodyInterpolation)
    collisionDetectionMode: Discrete (CollisionDetectionMode)
    constraints: None (RigidbodyConstraints)
```

### 2.5 enable / disable - 有効/無効切り替え

```bash
component enable <パス> <コンポーネント名>
component disable <パス> <コンポーネント名>
```

**注:** Behaviourを継承したコンポーネントのみ対応

**例:**
```bash
component disable /Player PlayerController
component enable /Player PlayerController
```

---

## 3. 型名解決

### 3.1 検索順序

1. `UnityEngine` 名前空間
2. `UnityEngine.UI` 名前空間
3. プロジェクト内のアセンブリ（Assembly-CSharp等）
4. 指定された名前空間（`--namespace`）

### 3.2 実装

```csharp
public static class TypeResolver
{
    private static readonly string[] DefaultNamespaces = new[]
    {
        "UnityEngine",
        "UnityEngine.UI",
        "UnityEngine.EventSystems",
        "TMPro"
    };

    public static Type ResolveComponentType(string typeName, string customNamespace = null)
    {
        // フルネームで指定された場合
        if (typeName.Contains("."))
        {
            return FindTypeInAllAssemblies(typeName);
        }

        // カスタム名前空間が指定された場合
        if (!string.IsNullOrEmpty(customNamespace))
        {
            var type = FindTypeInAllAssemblies($"{customNamespace}.{typeName}");
            if (type != null) return type;
        }

        // デフォルト名前空間を検索
        foreach (var ns in DefaultNamespaces)
        {
            var type = FindTypeInAllAssemblies($"{ns}.{typeName}");
            if (type != null) return type;
        }

        // 名前空間なしで全アセンブリ検索
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == typeName &&
                                   typeof(Component).IsAssignableFrom(t));
                if (type != null) return type;
            }
            catch (ReflectionTypeLoadException)
            {
                // 一部のアセンブリはロードに失敗する可能性
                continue;
            }
        }

        return null;
    }

    private static Type FindTypeInAllAssemblies(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(fullName);
            if (type != null && typeof(Component).IsAssignableFrom(type))
                return type;
        }
        return null;
    }
}
```

---

## 4. エラー処理

### 4.1 エラーケース

| エラー | メッセージ | 終了コード |
|--------|-----------|-----------|
| パスが存在しない | `component: '{path}': GameObject not found` | RuntimeError |
| 型が見つからない | `component: '{type}': Component type not found` | RuntimeError |
| 追加できない | `component: Cannot add '{type}' to '{path}'` | RuntimeError |
| Transformは削除不可 | `component: Cannot remove Transform component` | RuntimeError |
| インデックス範囲外 | `component: Index {n} out of range` | UsageError |
| サブコマンドなし | `component: missing subcommand` | UsageError |

---

## 5. 実装詳細

### 5.1 クラス構造

```csharp
namespace Xeon.UniTerminal.BuiltInCommands
{
    [Command("component", "Manage GameObject components")]
    public class ComponentCommand : ICommand
    {
        [Option("all", "a", Description = "Include all/remove all")]
        public bool All;

        [Option("verbose", "v", Description = "Show verbose output")]
        public bool Verbose;

        [Option("immediate", "i", Description = "Use DestroyImmediate")]
        public bool Immediate;

        [Option("namespace", "n", Description = "Component namespace")]
        public string Namespace;

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
            {
                await context.Stderr.WriteLineAsync("component: missing subcommand", ct);
                return ExitCode.UsageError;
            }

            var subCommand = context.PositionalArguments[0];
            var args = context.PositionalArguments.Skip(1).ToList();

            return subCommand.ToLower() switch
            {
                "list" => await ListAsync(context, args, ct),
                "add" => await AddAsync(context, args, ct),
                "remove" => await RemoveAsync(context, args, ct),
                "info" => await InfoAsync(context, args, ct),
                "enable" => await EnableAsync(context, args, true, ct),
                "disable" => await EnableAsync(context, args, false, ct),
                _ => await UnknownSubCommand(context, subCommand, ct)
            };
        }
    }
}
```

### 5.2 コンポーネント一覧

```csharp
private async Task<ExitCode> ListAsync(
    CommandContext context,
    List<string> args,
    CancellationToken ct)
{
    if (args.Count == 0)
    {
        await context.Stderr.WriteLineAsync("component list: missing path", ct);
        return ExitCode.UsageError;
    }

    var go = GameObjectPath.Resolve(args[0], context.WorkingDirectory);
    if (go == null)
    {
        await context.Stderr.WriteLineAsync(
            $"component: '{args[0]}': GameObject not found", ct);
        return ExitCode.RuntimeError;
    }

    var components = go.GetComponents<Component>();
    await context.Stdout.WriteLineAsync(
        $"Components on {go.name} ({components.Length}):", ct);

    for (int i = 0; i < components.Length; i++)
    {
        var comp = components[i];
        if (comp == null) continue;  // Missing script

        var type = comp.GetType();
        string enabledStr = "";

        // Behaviourの場合はenabled状態を表示
        if (comp is Behaviour behaviour)
        {
            enabledStr = behaviour.enabled ? " (enabled)" : " (disabled)";
        }

        if (Verbose)
        {
            await context.Stdout.WriteLineAsync(
                $"  [{i}] {type.Name,-30}{enabledStr} {type.FullName}", ct);
        }
        else
        {
            await context.Stdout.WriteLineAsync(
                $"  [{i}] {type.Name}{enabledStr}", ct);
        }
    }

    return ExitCode.Success;
}
```

### 5.3 コンポーネント追加

```csharp
private async Task<ExitCode> AddAsync(
    CommandContext context,
    List<string> args,
    CancellationToken ct)
{
    if (args.Count < 2)
    {
        await context.Stderr.WriteLineAsync(
            "component add: usage: component add <path> <type>", ct);
        return ExitCode.UsageError;
    }

    var go = GameObjectPath.Resolve(args[0], context.WorkingDirectory);
    if (go == null)
    {
        await context.Stderr.WriteLineAsync(
            $"component: '{args[0]}': GameObject not found", ct);
        return ExitCode.RuntimeError;
    }

    var typeName = args[1];
    var type = TypeResolver.ResolveComponentType(typeName, Namespace);

    if (type == null)
    {
        await context.Stderr.WriteLineAsync(
            $"component: '{typeName}': Component type not found", ct);
        return ExitCode.RuntimeError;
    }

#if UNITY_EDITOR
    UnityEditor.Undo.AddComponent(go, type);
#else
    go.AddComponent(type);
#endif

    await context.Stdout.WriteLineAsync(
        $"Added: {type.Name} to {GameObjectPath.GetPath(go)}", ct);
    return ExitCode.Success;
}
```

### 5.4 コンポーネント削除

```csharp
private async Task<ExitCode> RemoveAsync(
    CommandContext context,
    List<string> args,
    CancellationToken ct)
{
    if (args.Count < 2)
    {
        await context.Stderr.WriteLineAsync(
            "component remove: usage: component remove <path> <type|index>", ct);
        return ExitCode.UsageError;
    }

    var go = GameObjectPath.Resolve(args[0], context.WorkingDirectory);
    if (go == null)
    {
        await context.Stderr.WriteLineAsync(
            $"component: '{args[0]}': GameObject not found", ct);
        return ExitCode.RuntimeError;
    }

    var identifier = args[1];
    var components = go.GetComponents<Component>();

    // インデックス指定かどうか
    if (int.TryParse(identifier, out int index))
    {
        if (index < 0 || index >= components.Length)
        {
            await context.Stderr.WriteLineAsync(
                $"component: Index {index} out of range (0-{components.Length - 1})", ct);
            return ExitCode.UsageError;
        }

        var comp = components[index];
        if (comp is Transform)
        {
            await context.Stderr.WriteLineAsync(
                "component: Cannot remove Transform component", ct);
            return ExitCode.RuntimeError;
        }

        return await DestroyComponent(context, go, comp, ct);
    }

    // 型名指定
    var type = TypeResolver.ResolveComponentType(identifier, Namespace);
    if (type == null)
    {
        await context.Stderr.WriteLineAsync(
            $"component: '{identifier}': Component type not found", ct);
        return ExitCode.RuntimeError;
    }

    if (type == typeof(Transform))
    {
        await context.Stderr.WriteLineAsync(
            "component: Cannot remove Transform component", ct);
        return ExitCode.RuntimeError;
    }

    var toRemove = All
        ? go.GetComponents(type)
        : new[] { go.GetComponent(type) };

    if (toRemove.Length == 0 || toRemove[0] == null)
    {
        await context.Stderr.WriteLineAsync(
            $"component: '{identifier}' not found on {go.name}", ct);
        return ExitCode.RuntimeError;
    }

    foreach (var comp in toRemove.Where(c => c != null))
    {
        await DestroyComponent(context, go, comp, ct);
    }

    return ExitCode.Success;
}

private async Task<ExitCode> DestroyComponent(
    CommandContext context,
    GameObject go,
    Component comp,
    CancellationToken ct)
{
    var typeName = comp.GetType().Name;

#if UNITY_EDITOR
    if (Immediate)
        Object.DestroyImmediate(comp);
    else
        UnityEditor.Undo.DestroyObjectImmediate(comp);
#else
    if (Immediate)
        Object.DestroyImmediate(comp);
    else
        Object.Destroy(comp);
#endif

    await context.Stdout.WriteLineAsync(
        $"Removed: {typeName} from {GameObjectPath.GetPath(go)}", ct);
    return ExitCode.Success;
}
```

---

## 6. テストケース

### 6.1 listテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| COMP-001 | 一覧表示 | `component list /Player` | コンポーネント一覧表示 |
| COMP-002 | 詳細表示 | `component list /Player -v` | フルネーム付き表示 |
| COMP-003 | 存在しないパス | `component list /NoExist` | エラーメッセージ |

### 6.2 addテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| COMP-010 | Unity標準追加 | `component add /Obj Rigidbody` | Rigidbody追加 |
| COMP-011 | UI追加 | `component add /Obj Image` | Image追加 |
| COMP-012 | カスタム追加 | `component add /Obj MyComp -n MyGame` | カスタムコンポーネント追加 |
| COMP-013 | 存在しない型 | `component add /Obj NoExist` | エラーメッセージ |

### 6.3 removeテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| COMP-020 | 型名で削除 | `component remove /Obj Rigidbody` | Rigidbody削除 |
| COMP-021 | インデックスで削除 | `component remove /Obj 2` | 2番目のコンポーネント削除 |
| COMP-022 | Transform削除試行 | `component remove /Obj Transform` | エラー（削除不可） |
| COMP-023 | 全削除 | `component remove /Obj BoxCollider -a` | 全BoxCollider削除 |

### 6.4 infoテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| COMP-030 | 情報表示 | `component info /Obj Rigidbody` | プロパティ一覧表示 |
| COMP-031 | インデックス指定 | `component info /Obj 1` | 1番目のコンポーネント情報 |

### 6.5 enable/disableテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| COMP-040 | 無効化 | `component disable /Obj Animator` | Animator無効化 |
| COMP-041 | 有効化 | `component enable /Obj Animator` | Animator有効化 |
| COMP-042 | 非Behaviour | `component disable /Obj Rigidbody` | 警告またはエラー |

---

## 7. 補完対応

### 7.1 補完ターゲット

- **サブコマンド:** list, add, remove, info, enable, disable
- **パス引数:** GameObjectパス補完
- **型名:** よく使うコンポーネント名

### 7.2 実装

```csharp
public IEnumerable<string> GetCompletions(CompletionContext context)
{
    // サブコマンド補完
    if (context.TokenIndex == 1)
    {
        var subCommands = new[] { "list", "add", "remove", "info", "enable", "disable" };
        foreach (var cmd in subCommands)
        {
            if (cmd.StartsWith(context.WordToComplete, StringComparison.OrdinalIgnoreCase))
                yield return cmd;
        }
        yield break;
    }

    // addサブコマンドの型名補完
    if (context.TokenIndex == 3 && context.PreviousTokens.Contains("add"))
    {
        var commonTypes = new[]
        {
            "Rigidbody", "Rigidbody2D",
            "BoxCollider", "SphereCollider", "CapsuleCollider", "MeshCollider",
            "BoxCollider2D", "CircleCollider2D",
            "AudioSource", "AudioListener",
            "Camera", "Light",
            "Animator", "Animation",
            "Canvas", "Image", "Text", "Button", "RawImage"
        };

        foreach (var type in commonTypes)
        {
            if (type.StartsWith(context.WordToComplete, StringComparison.OrdinalIgnoreCase))
                yield return type;
        }
    }
}
```

---

## 8. 実装スケジュール

### Phase 1（必須機能）
1. list（一覧表示）
2. add（コンポーネント追加）
3. remove（コンポーネント削除）
4. TypeResolver実装

### Phase 2（拡張機能）
1. info（詳細情報表示）
2. enable/disable
3. 詳細表示（`-v`）
4. インデックス指定

### Phase 3（将来拡張）
1. 補完対応
2. Undo対応
3. Missing script検出・削除
