# hierarchy コマンド実装計画書

## 1. 概要

`hierarchy` コマンドは、現在のUnityシーンのヒエラルキー構造を表示するコマンドです。Unityエディタのヒエラルキーウィンドウと同等の情報をCLIで確認できます。

### 1.1 基本仕様

- **コマンド名:** `hierarchy`（エイリアス: `hier`, `h`）
- **説明:** シーンのヒエラルキー構造を表示
- **書式:** `hierarchy [オプション] [パス]`

### 1.2 Unity API

- `UnityEngine.SceneManagement.SceneManager`
- `GameObject.transform.GetChild()`
- `Transform.childCount`

---

## 2. オプション仕様

### 2.1 実装するオプション（Phase 1）

| オプション | 短縮形 | 型 | デフォルト | 説明 |
|-----------|--------|-----|-----------|------|
| `--recursive` | `-r` | bool | false | 子オブジェクトを再帰的に表示 |
| `--depth` | `-d` | int | -1 | 表示する最大深度（-1=無制限） |
| `--all` | `-a` | bool | false | 非アクティブオブジェクトも表示 |
| `--long` | `-l` | bool | false | 詳細情報を表示（コンポーネント数等） |
| `--scene` | `-s` | string | null | 対象シーン名（未指定時はアクティブシーン） |

### 2.2 将来的に実装を検討するオプション（Phase 2）

| オプション | 短縮形 | 説明 |
|-----------|--------|------|
| `--filter` | `-f` | 名前でフィルタリング |
| `--component` | `-c` | 特定コンポーネントを持つオブジェクトのみ |
| `--tag` | `-t` | 特定タグのオブジェクトのみ |
| `--layer` | | 特定レイヤーのオブジェクトのみ |

---

## 3. 出力形式

### 3.1 デフォルト出力（ルートオブジェクト一覧）

```
Scene: SampleScene (5 root objects)
├── Main Camera
├── Directional Light
├── Canvas
├── EventSystem
└── Player
```

### 3.2 再帰表示（`-r` オプション）

```
Scene: SampleScene
├── Main Camera
├── Directional Light
├── Canvas
│   ├── Panel
│   │   ├── Button
│   │   └── Text
│   └── Image
├── EventSystem
└── Player
    ├── Body
    │   ├── Head
    │   └── Arms
    └── Weapon
```

### 3.3 詳細表示（`-l` オプション）

```
Scene: SampleScene
├── [A] Main Camera          (3 components) [MainCamera]
├── [A] Directional Light    (2 components) [Untagged]
├── [A] Canvas               (4 components) [Untagged]
├── [A] EventSystem          (2 components) [Untagged]
└── [A] Player               (5 components) [Player]
```

**凡例:**
- `[A]` = Active, `[-]` = Inactive
- `(N components)` = アタッチされているコンポーネント数
- `[Tag]` = タグ名

### 3.4 パス指定時

```bash
hierarchy /Canvas
```

```
Canvas (children: 2)
├── Panel
└── Image
```

### 3.5 非アクティブ表示（`-a` オプション）

```
Scene: SampleScene
├── [A] Main Camera
├── [A] Directional Light
├── [-] DebugPanel           # 非アクティブ
├── [A] Canvas
└── [A] Player
```

---

## 4. パス指定

### 4.1 パス形式

| パス | 説明 |
|------|------|
| `/` | シーンルート（全ルートオブジェクト） |
| `/ObjectName` | ルートオブジェクト |
| `/Parent/Child` | 階層パス |
| `ObjectName` | `/` なしでもルートから検索 |

### 4.2 パス解決

```csharp
public static GameObject ResolvePath(string path)
{
    if (string.IsNullOrEmpty(path) || path == "/")
        return null;  // ルート全体

    var parts = path.TrimStart('/').Split('/');
    var scene = SceneManager.GetActiveScene();
    var roots = scene.GetRootGameObjects();

    GameObject current = roots.FirstOrDefault(r => r.name == parts[0]);

    for (int i = 1; i < parts.Length && current != null; i++)
    {
        var child = current.transform.Find(parts[i]);
        current = child?.gameObject;
    }

    return current;
}
```

---

## 5. 複数シーン対応

### 5.1 ロード済みシーン一覧

```bash
hierarchy --scene list
```

```
Loaded Scenes:
  * SampleScene (active)
    AdditiveScene1
    AdditiveScene2
```

### 5.2 特定シーン指定

```bash
hierarchy --scene AdditiveScene1
```

---

## 6. エラー処理

### 6.1 エラーケース

| エラー | メッセージ | 終了コード |
|--------|-----------|-----------|
| パスが存在しない | `hierarchy: '{path}': GameObject not found` | RuntimeError |
| シーンが存在しない | `hierarchy: '{scene}': Scene not loaded` | RuntimeError |
| 無効な深度 | `hierarchy: invalid depth: {n}` | UsageError |

---

## 7. 実装詳細

### 7.1 クラス構造

```csharp
namespace Xeon.UniTerminal.BuiltInCommands
{
    [Command("hierarchy", "Display scene hierarchy")]
    public class HierarchyCommand : ICommand
    {
        [Option("recursive", "r", Description = "Show children recursively")]
        public bool Recursive;

        [Option("depth", "d", Description = "Maximum depth to display")]
        public int MaxDepth = -1;

        [Option("all", "a", Description = "Include inactive objects")]
        public bool ShowInactive;

        [Option("long", "l", Description = "Show detailed information")]
        public bool LongFormat;

        [Option("scene", "s", Description = "Target scene name")]
        public string SceneName;

        // 実装...
    }
}
```

### 7.2 ツリー描画

```csharp
private async Task PrintTree(
    Transform transform,
    string prefix,
    bool isLast,
    int currentDepth,
    CommandContext context,
    CancellationToken ct)
{
    if (MaxDepth >= 0 && currentDepth > MaxDepth)
        return;

    var go = transform.gameObject;

    // 非アクティブをスキップ（オプションによる）
    if (!ShowInactive && !go.activeInHierarchy)
        return;

    // 行頭の記号
    string connector = isLast ? "└── " : "├── ";
    string childPrefix = isLast ? "    " : "│   ";

    // オブジェクト情報を構築
    string line = BuildObjectLine(go);

    await context.Stdout.WriteLineAsync(prefix + connector + line, ct);

    // 子オブジェクトを処理
    if (Recursive || currentDepth == 0)
    {
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var child = transform.GetChild(i);
            bool childIsLast = (i == childCount - 1);
            await PrintTree(child, prefix + childPrefix, childIsLast,
                          currentDepth + 1, context, ct);
        }
    }
}

private string BuildObjectLine(GameObject go)
{
    if (!LongFormat)
        return go.name;

    string active = go.activeSelf ? "[A]" : "[-]";
    int compCount = go.GetComponents<Component>().Length;
    string tag = go.tag;

    return $"{active} {go.name,-24} ({compCount} components) [{tag}]";
}
```

### 7.3 ルートオブジェクト取得

```csharp
private GameObject[] GetRootGameObjects()
{
    Scene scene;

    if (!string.IsNullOrEmpty(SceneName))
    {
        scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException($"Scene not loaded: {SceneName}");
    }
    else
    {
        scene = SceneManager.GetActiveScene();
    }

    return scene.GetRootGameObjects();
}
```

---

## 8. テストケース

### 8.1 基本テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| HIER-001 | ルート一覧 | `hierarchy` | ルートオブジェクト一覧表示 |
| HIER-002 | 再帰表示 | `hierarchy -r` | 全階層をツリー表示 |
| HIER-003 | 深度制限 | `hierarchy -r -d 2` | 2階層までのみ表示 |
| HIER-004 | 詳細表示 | `hierarchy -l` | コンポーネント数等を表示 |

### 8.2 パス指定テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| HIER-010 | ルートパス | `hierarchy /` | ルート一覧 |
| HIER-011 | オブジェクト指定 | `hierarchy /Canvas` | Canvasの子を表示 |
| HIER-012 | 深い階層 | `hierarchy /A/B/C` | Cの子を表示 |
| HIER-013 | 存在しないパス | `hierarchy /NoExist` | エラーメッセージ |

### 8.3 フィルタテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| HIER-020 | 非アクティブ含む | `hierarchy -a` | 非アクティブも表示 |
| HIER-021 | アクティブのみ | `hierarchy` | アクティブのみ表示 |

### 8.4 シーンテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| HIER-030 | シーン一覧 | `hierarchy -s list` | ロード済みシーン一覧 |
| HIER-031 | シーン指定 | `hierarchy -s OtherScene` | 指定シーンのルート表示 |

### 8.5 パイプテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| HIER-040 | grep連携 | `hierarchy -r \| grep Player` | Playerを含む行のみ |

---

## 9. 補完対応

### 9.1 補完ターゲット

- **位置引数:** GameObject パス補完
- **オプション値:** `--scene` のシーン名補完

### 9.2 実装

```csharp
public IEnumerable<string> GetCompletions(CompletionContext context)
{
    var prefix = context.WordToComplete;

    // パス補完
    if (!prefix.StartsWith("-"))
    {
        foreach (var path in GetGameObjectPaths(prefix))
        {
            yield return path;
        }
    }
}

private IEnumerable<string> GetGameObjectPaths(string prefix)
{
    // 現在のパスから候補を生成
    var parentPath = GetParentPath(prefix);
    var parent = ResolvePath(parentPath);

    if (parent == null)
    {
        // ルートオブジェクト
        foreach (var root in GetRootGameObjects())
        {
            var path = "/" + root.name;
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                yield return path;
        }
    }
    else
    {
        // 子オブジェクト
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            var child = parent.transform.GetChild(i);
            var path = parentPath + "/" + child.name;
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                yield return path;
        }
    }
}
```

---

## 10. 実装スケジュール

### Phase 1（必須機能）
1. ルートオブジェクト一覧表示
2. 再帰表示（`-r`）
3. 深度制限（`-d`）
4. パス指定

### Phase 2（拡張機能）
1. 詳細表示（`-l`）
2. 非アクティブ表示（`-a`）
3. 複数シーン対応（`-s`）

### Phase 3（将来拡張）
1. フィルタリング（名前、タグ、レイヤー）
2. コンポーネントフィルタ
3. 補完対応
