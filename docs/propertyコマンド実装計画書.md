# property コマンド実装計画書

## 1. 概要

`property` コマンドは、コンポーネントのプロパティ（フィールド・プロパティ）の取得・設定を行うコマンドです。Reflectionを使用して任意のコンポーネントの値を操作できます。

### 1.1 基本仕様

- **コマンド名:** `property`（エイリアス: `prop`）
- **説明:** コンポーネントのプロパティを取得・設定
- **書式:** `property <サブコマンド> <パス> <コンポーネント> [プロパティ名] [値]`

### 1.2 サブコマンド一覧

| サブコマンド | 説明 |
|-------------|------|
| `get` | プロパティ値を取得 |
| `set` | プロパティ値を設定 |
| `list` | プロパティ一覧を表示 |

### 1.3 使用技術

- `System.Reflection`
- `FieldInfo`, `PropertyInfo`
- 型変換（文字列 → 各種型）

---

## 2. サブコマンド仕様

### 2.1 list - プロパティ一覧

```bash
property list <パス> <コンポーネント> [オプション]
```

| オプション | 短縮形 | 型 | 説明 |
|-----------|--------|-----|------|
| `--all` | `-a` | bool | privateフィールドも表示 |
| `--serialized` | `-s` | bool | SerializeFieldのみ表示 |

**出力例:**
```bash
property list /Player Rigidbody
```

```
Properties of Rigidbody on /Player:
  mass           float     1
  drag           float     0
  angularDrag    float     0.05
  useGravity     bool      true
  isKinematic    bool      false
  interpolation  enum      None
  constraints    enum      None
  velocity       Vector3   (0.0, 0.0, 0.0)  [readonly]
  position       Vector3   (0.0, 1.0, 0.0)
```

### 2.2 get - プロパティ値取得

```bash
property get <パス> <コンポーネント> <プロパティ名>
```

**出力例:**
```bash
property get /Player Rigidbody mass
```

```
Rigidbody.mass = 1 (float)
```

**複数プロパティ:**
```bash
property get /Player Rigidbody mass,drag,useGravity
```

```
Rigidbody.mass = 1 (float)
Rigidbody.drag = 0 (float)
Rigidbody.useGravity = true (bool)
```

### 2.3 set - プロパティ値設定

```bash
property set <パス> <コンポーネント> <プロパティ名> <値>
```

**例:**
```bash
property set /Player Rigidbody mass 10
property set /Player Transform position 0,5,0
property set /Light Light color 1,0.5,0,1
property set /Player Rigidbody useGravity false
property set /Player Rigidbody interpolation Interpolate
```

**出力:**
```
Rigidbody.mass: 1 -> 10
```

---

## 3. 型変換

### 3.1 対応する型

| 型 | 入力形式 | 例 |
|-----|---------|-----|
| `int` | 整数 | `10`, `-5` |
| `float` | 小数 | `1.5`, `3.14` |
| `bool` | true/false | `true`, `false` |
| `string` | 文字列 | `"hello"`, `hello` |
| `Vector2` | x,y | `1,2` |
| `Vector3` | x,y,z | `1,2,3` |
| `Vector4` | x,y,z,w | `1,2,3,4` |
| `Color` | r,g,b,a または名前 | `1,0,0,1`, `red` |
| `Quaternion` | x,y,z,w または euler | `0,0,0,1`, `euler:0,90,0` |
| `enum` | 列挙名 | `Interpolate`, `None` |
| `LayerMask` | レイヤー名/番号 | `Default`, `8` |

### 3.2 型変換実装

```csharp
public static class ValueConverter
{
    public static object Convert(string value, Type targetType)
    {
        // null/空チェック
        if (string.IsNullOrEmpty(value))
        {
            if (targetType.IsValueType)
                return Activator.CreateInstance(targetType);
            return null;
        }

        // 基本型
        if (targetType == typeof(int))
            return int.Parse(value);
        if (targetType == typeof(float))
            return float.Parse(value);
        if (targetType == typeof(double))
            return double.Parse(value);
        if (targetType == typeof(bool))
            return bool.Parse(value);
        if (targetType == typeof(string))
            return value.Trim('"');

        // Unity型
        if (targetType == typeof(Vector2))
            return ParseVector2(value);
        if (targetType == typeof(Vector3))
            return ParseVector3(value);
        if (targetType == typeof(Vector4))
            return ParseVector4(value);
        if (targetType == typeof(Color))
            return ParseColor(value);
        if (targetType == typeof(Quaternion))
            return ParseQuaternion(value);

        // Enum
        if (targetType.IsEnum)
            return Enum.Parse(targetType, value, ignoreCase: true);

        // LayerMask
        if (targetType == typeof(LayerMask))
            return ParseLayerMask(value);

        throw new NotSupportedException($"Cannot convert to type: {targetType.Name}");
    }

    private static Vector3 ParseVector3(string value)
    {
        var parts = value.Split(',');
        return new Vector3(
            float.Parse(parts[0]),
            float.Parse(parts[1]),
            float.Parse(parts[2])
        );
    }

    private static Color ParseColor(string value)
    {
        // 名前で指定
        switch (value.ToLower())
        {
            case "red": return Color.red;
            case "green": return Color.green;
            case "blue": return Color.blue;
            case "white": return Color.white;
            case "black": return Color.black;
            case "yellow": return Color.yellow;
            case "cyan": return Color.cyan;
            case "magenta": return Color.magenta;
            case "gray": case "grey": return Color.gray;
            case "clear": return Color.clear;
        }

        // RGBA値で指定
        var parts = value.Split(',');
        return new Color(
            float.Parse(parts[0]),
            float.Parse(parts[1]),
            float.Parse(parts[2]),
            parts.Length > 3 ? float.Parse(parts[3]) : 1f
        );
    }

    private static Quaternion ParseQuaternion(string value)
    {
        // euler: プレフィックスでEuler角指定
        if (value.StartsWith("euler:", StringComparison.OrdinalIgnoreCase))
        {
            var euler = ParseVector3(value.Substring(6));
            return Quaternion.Euler(euler);
        }

        // 直接Quaternion値
        var parts = value.Split(',');
        return new Quaternion(
            float.Parse(parts[0]),
            float.Parse(parts[1]),
            float.Parse(parts[2]),
            float.Parse(parts[3])
        );
    }
}
```

---

## 4. 参照型プロパティ

### 4.1 対応方針

参照型（Material, Sprite, GameObject等）は直接設定が困難なため、以下の方針を採用：

1. **表示のみ:** 参照の有無と型名を表示
2. **null設定:** `null` を指定して参照をクリア
3. **将来対応:** アセットパスやリソースパスでの指定

**出力例:**
```
material      Material    "PlayerMaterial" (UnityEngine.Material)
sprite        Sprite      (null)
target        Transform   "/Enemy/Body" (UnityEngine.Transform)
```

### 4.2 参照型の設定（Phase 2）

```bash
# リソースから読み込み
property set /Player SpriteRenderer sprite resource:Sprites/Player

# シーン内オブジェクトの参照
property set /Player TargetScript target ref:/Enemy
```

---

## 5. エラー処理

### 5.1 エラーケース

| エラー | メッセージ | 終了コード |
|--------|-----------|-----------|
| パスが存在しない | `property: '{path}': GameObject not found` | RuntimeError |
| コンポーネントが見つからない | `property: '{comp}': Component not found` | RuntimeError |
| プロパティが見つからない | `property: '{prop}': Property not found on {comp}` | RuntimeError |
| 読み取り専用 | `property: '{prop}' is read-only` | RuntimeError |
| 型変換エラー | `property: Cannot convert '{value}' to {type}` | UsageError |
| サポートされない型 | `property: Type '{type}' is not supported` | UsageError |

---

## 6. 実装詳細

### 6.1 クラス構造

```csharp
namespace Xeon.UniTerminal.BuiltInCommands
{
    [Command("property", "Get or set component properties")]
    public class PropertyCommand : ICommand
    {
        [Option("all", "a", Description = "Include private fields")]
        public bool IncludePrivate;

        [Option("serialized", "s", Description = "Show only SerializeField")]
        public bool SerializedOnly;

        public async Task<ExitCode> ExecuteAsync(CommandContext context, CancellationToken ct)
        {
            if (context.PositionalArguments.Count == 0)
            {
                await context.Stderr.WriteLineAsync("property: missing subcommand", ct);
                return ExitCode.UsageError;
            }

            var subCommand = context.PositionalArguments[0];
            var args = context.PositionalArguments.Skip(1).ToList();

            return subCommand.ToLower() switch
            {
                "list" => await ListAsync(context, args, ct),
                "get" => await GetAsync(context, args, ct),
                "set" => await SetAsync(context, args, ct),
                _ => await UnknownSubCommand(context, subCommand, ct)
            };
        }
    }
}
```

### 6.2 プロパティ一覧

```csharp
private async Task<ExitCode> ListAsync(
    CommandContext context,
    List<string> args,
    CancellationToken ct)
{
    if (args.Count < 2)
    {
        await context.Stderr.WriteLineAsync(
            "property list: usage: property list <path> <component>", ct);
        return ExitCode.UsageError;
    }

    var (go, comp) = ResolveComponent(args[0], args[1], context);
    if (comp == null)
        return ExitCode.RuntimeError;

    var type = comp.GetType();
    var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

    if (IncludePrivate)
        bindingFlags |= BindingFlags.NonPublic;

    await context.Stdout.WriteLineAsync(
        $"Properties of {type.Name} on {GameObjectPath.GetPath(go)}:", ct);

    // フィールド
    foreach (var field in type.GetFields(bindingFlags))
    {
        if (SerializedOnly && !HasSerializeField(field))
            continue;

        var value = field.GetValue(comp);
        var valueStr = FormatValue(value);
        var readOnly = field.IsInitOnly ? " [readonly]" : "";

        await context.Stdout.WriteLineAsync(
            $"  {field.Name,-20} {GetFriendlyTypeName(field.FieldType),-12} {valueStr}{readOnly}", ct);
    }

    // プロパティ
    foreach (var prop in type.GetProperties(bindingFlags))
    {
        if (!prop.CanRead) continue;
        if (prop.GetIndexParameters().Length > 0) continue;  // インデクサーは除外

        try
        {
            var value = prop.GetValue(comp);
            var valueStr = FormatValue(value);
            var readOnly = !prop.CanWrite ? " [readonly]" : "";

            await context.Stdout.WriteLineAsync(
                $"  {prop.Name,-20} {GetFriendlyTypeName(prop.PropertyType),-12} {valueStr}{readOnly}", ct);
        }
        catch
        {
            // 一部のプロパティは読み取り時にエラーになる場合がある
        }
    }

    return ExitCode.Success;
}

private bool HasSerializeField(FieldInfo field)
{
    return field.GetCustomAttribute<SerializeField>() != null ||
           (field.IsPublic && field.GetCustomAttribute<NonSerializedAttribute>() == null);
}
```

### 6.3 値の取得

```csharp
private async Task<ExitCode> GetAsync(
    CommandContext context,
    List<string> args,
    CancellationToken ct)
{
    if (args.Count < 3)
    {
        await context.Stderr.WriteLineAsync(
            "property get: usage: property get <path> <component> <property>", ct);
        return ExitCode.UsageError;
    }

    var (go, comp) = ResolveComponent(args[0], args[1], context);
    if (comp == null)
        return ExitCode.RuntimeError;

    var propertyNames = args[2].Split(',');
    var type = comp.GetType();

    foreach (var propName in propertyNames)
    {
        var (value, memberType, error) = GetMemberValue(comp, type, propName.Trim());

        if (error != null)
        {
            await context.Stderr.WriteLineAsync(error, ct);
            continue;
        }

        var valueStr = FormatValue(value);
        var typeName = GetFriendlyTypeName(memberType);
        await context.Stdout.WriteLineAsync(
            $"{type.Name}.{propName} = {valueStr} ({typeName})", ct);
    }

    return ExitCode.Success;
}

private (object value, Type type, string error) GetMemberValue(
    Component comp,
    Type type,
    string memberName)
{
    // フィールドを検索
    var field = type.GetField(memberName,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (field != null)
    {
        return (field.GetValue(comp), field.FieldType, null);
    }

    // プロパティを検索
    var prop = type.GetProperty(memberName,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (prop != null && prop.CanRead)
    {
        return (prop.GetValue(comp), prop.PropertyType, null);
    }

    return (null, null, $"property: '{memberName}': Property not found on {type.Name}");
}
```

### 6.4 値の設定

```csharp
private async Task<ExitCode> SetAsync(
    CommandContext context,
    List<string> args,
    CancellationToken ct)
{
    if (args.Count < 4)
    {
        await context.Stderr.WriteLineAsync(
            "property set: usage: property set <path> <component> <property> <value>", ct);
        return ExitCode.UsageError;
    }

    var (go, comp) = ResolveComponent(args[0], args[1], context);
    if (comp == null)
        return ExitCode.RuntimeError;

    var propName = args[2];
    var newValueStr = args[3];
    var type = comp.GetType();

#if UNITY_EDITOR
    UnityEditor.Undo.RecordObject(comp, $"Set {propName}");
#endif

    // フィールドを検索
    var field = type.GetField(propName,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (field != null)
    {
        if (field.IsInitOnly)
        {
            await context.Stderr.WriteLineAsync(
                $"property: '{propName}' is read-only", ct);
            return ExitCode.RuntimeError;
        }

        var oldValue = field.GetValue(comp);

        try
        {
            var newValue = ValueConverter.Convert(newValueStr, field.FieldType);
            field.SetValue(comp, newValue);

            await context.Stdout.WriteLineAsync(
                $"{type.Name}.{propName}: {FormatValue(oldValue)} -> {FormatValue(newValue)}", ct);
            return ExitCode.Success;
        }
        catch (Exception ex)
        {
            await context.Stderr.WriteLineAsync(
                $"property: Cannot convert '{newValueStr}' to {field.FieldType.Name}: {ex.Message}", ct);
            return ExitCode.UsageError;
        }
    }

    // プロパティを検索
    var prop = type.GetProperty(propName,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (prop != null)
    {
        if (!prop.CanWrite)
        {
            await context.Stderr.WriteLineAsync(
                $"property: '{propName}' is read-only", ct);
            return ExitCode.RuntimeError;
        }

        var oldValue = prop.CanRead ? prop.GetValue(comp) : null;

        try
        {
            var newValue = ValueConverter.Convert(newValueStr, prop.PropertyType);
            prop.SetValue(comp, newValue);

            await context.Stdout.WriteLineAsync(
                $"{type.Name}.{propName}: {FormatValue(oldValue)} -> {FormatValue(newValue)}", ct);
            return ExitCode.Success;
        }
        catch (Exception ex)
        {
            await context.Stderr.WriteLineAsync(
                $"property: Cannot convert '{newValueStr}' to {prop.PropertyType.Name}: {ex.Message}", ct);
            return ExitCode.UsageError;
        }
    }

    await context.Stderr.WriteLineAsync(
        $"property: '{propName}': Property not found on {type.Name}", ct);
    return ExitCode.RuntimeError;
}
```

### 6.5 値のフォーマット

```csharp
private string FormatValue(object value)
{
    if (value == null)
        return "(null)";

    return value switch
    {
        Vector2 v => $"({v.x:F1}, {v.y:F1})",
        Vector3 v => $"({v.x:F1}, {v.y:F1}, {v.z:F1})",
        Vector4 v => $"({v.x:F1}, {v.y:F1}, {v.z:F1}, {v.w:F1})",
        Quaternion q => $"({q.x:F2}, {q.y:F2}, {q.z:F2}, {q.w:F2})",
        Color c => $"({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})",
        string s => $"\"{s}\"",
        UnityEngine.Object obj => obj != null ? $"\"{obj.name}\"" : "(null)",
        _ => value.ToString()
    };
}
```

---

## 7. テストケース

### 7.1 listテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| PROP-001 | プロパティ一覧 | `property list /Obj Rigidbody` | プロパティ一覧表示 |
| PROP-002 | private含む | `property list /Obj MyComp -a` | privateも表示 |
| PROP-003 | SerializeFieldのみ | `property list /Obj MyComp -s` | SerializeFieldのみ |

### 7.2 getテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| PROP-010 | 単一取得 | `property get /Obj Rigidbody mass` | mass値を表示 |
| PROP-011 | 複数取得 | `property get /Obj Rigidbody mass,drag` | 両方を表示 |
| PROP-012 | 存在しない | `property get /Obj Rigidbody noexist` | エラーメッセージ |

### 7.3 setテスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| PROP-020 | float設定 | `property set /Obj Rigidbody mass 10` | massが10に |
| PROP-021 | bool設定 | `property set /Obj Rigidbody useGravity false` | gravityがfalseに |
| PROP-022 | Vector3設定 | `property set /Obj Transform position 0,5,0` | 位置変更 |
| PROP-023 | Color設定 | `property set /Obj Light color red` | 色が赤に |
| PROP-024 | enum設定 | `property set /Obj Rigidbody interpolation Interpolate` | 補間有効 |
| PROP-025 | 読み取り専用 | `property set /Obj Rigidbody velocity 0,0,0` | エラー |
| PROP-026 | 型エラー | `property set /Obj Rigidbody mass abc` | 変換エラー |

### 7.4 特殊型テスト

| ID | テスト内容 | 入力 | 期待結果 |
|----|-----------|------|----------|
| PROP-030 | Quaternion euler | `property set /Obj Transform rotation euler:0,90,0` | Y軸90度 |
| PROP-031 | Color名前 | `property set /Obj Light color yellow` | 黄色に |
| PROP-032 | LayerMask | `property set /Obj Comp layerMask Default` | Defaultレイヤー |

---

## 8. 補完対応

### 8.1 補完ターゲット

- **サブコマンド:** list, get, set
- **パス引数:** GameObjectパス補完
- **コンポーネント:** そのオブジェクトにアタッチされているコンポーネント
- **プロパティ名:** そのコンポーネントのプロパティ

### 8.2 実装

```csharp
public IEnumerable<string> GetCompletions(CompletionContext context)
{
    // サブコマンド補完
    if (context.TokenIndex == 1)
    {
        var subCommands = new[] { "list", "get", "set" };
        foreach (var cmd in subCommands)
        {
            if (cmd.StartsWith(context.WordToComplete, StringComparison.OrdinalIgnoreCase))
                yield return cmd;
        }
        yield break;
    }

    // プロパティ名補完（get/setの3番目の引数）
    if (context.TokenIndex == 4)
    {
        var path = context.PreviousTokens.ElementAtOrDefault(2);
        var compName = context.PreviousTokens.ElementAtOrDefault(3);

        if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(compName))
        {
            var go = GameObjectPath.Resolve(path);
            var comp = go?.GetComponent(compName);
            if (comp != null)
            {
                foreach (var member in GetMemberNames(comp.GetType()))
                {
                    if (member.StartsWith(context.WordToComplete, StringComparison.OrdinalIgnoreCase))
                        yield return member;
                }
            }
        }
    }
}
```

---

## 9. 実装スケジュール

### Phase 1（必須機能）
1. list（プロパティ一覧）
2. get（値取得）
3. set（基本型: int, float, bool, string）
4. ValueConverter（基本型）

### Phase 2（拡張機能）
1. set（Unity型: Vector3, Color, Quaternion）
2. set（enum型）
3. 複数プロパティ取得
4. private/SerializeField対応

### Phase 3（将来拡張）
1. 参照型の設定（Resources, シーン内参照）
2. 配列/リストの操作
3. 補完対応
4. Undo対応
