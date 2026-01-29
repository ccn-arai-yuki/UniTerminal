# Unityコマンド

Unity GameObjects、Transform、Componentsを操作するためのコマンドです。

## hierarchy

シーンヒエラルキーをツリー構造で表示します。

### 書式

```bash
hierarchy [-r] [-d depth] [-a] [-l] [-i] [-s scene] [-n pattern] [-c component] [-t tag] [-y layer] [path]
```

### 説明

現在のシーンのGameObjectsをツリー構造で表示します。名前、コンポーネント、タグ、レイヤーでフィルタリングできます。パスを指定すると、そのオブジェクトの子を表示します。

### オプション

| オプション | ロング形式 | 説明 |
|--------|------|------|
| `-r` | `--recursive` | 子を再帰的に表示 |
| `-d` | `--depth` | 表示する最大深度（-1 = 無制限、デフォルト） |
| `-a` | `--all` | 非アクティブなオブジェクトも含める |
| `-l` | `--long` | 詳細情報を表示（アクティブ状態、コンポーネント数、タグ） |
| `-i` | `--id` | インスタンスIDを表示（参照設定に使用） |
| `-s` | `--scene` | 対象シーン名。`list` でロード済みシーンを表示 |
| `-n` | `--name` | 名前でフィルタ（`*` と `?` ワイルドカード対応） |
| `-c` | `--component` | コンポーネントタイプでフィルタ |
| `-t` | `--tag` | タグでフィルタ |
| `-y` | `--layer` | レイヤー名または番号（0-31）でフィルタ |

### 引数

| 引数 | 説明 |
|----------|------|
| `path` | オプション。シーンルートの代わりにこのオブジェクトの子を表示。 |

### 出力形式

**通常形式:**
```
Scene: SampleScene (3 root objects)
├── Main Camera
├── Directional Light
└── Player
    ├── Model
    └── Weapon
```

**長形式 (`-l`):**
```
Scene: SampleScene (3 root objects)
├── [A] Main Camera              (4 components) [MainCamera]
├── [A] Directional Light        (2 components) [Untagged]
└── [A] Player                   (5 components) [Player]
```

`[A]` = アクティブ、`[-]` = 非アクティブ

**インスタンスID付き (`-i`):**
```
├── Player #12345
├── Enemy #12346
└── Camera #12347
```

### 使用例

```bash
# アクティブシーンのルートオブジェクトを表示
hierarchy

# 再帰的に表示
hierarchy -r

# 詳細情報付きで表示
hierarchy -l

# インスタンスIDを表示（参照設定に使用）
hierarchy -i

# 深度を3レベルに制限
hierarchy -r -d 3

# 非アクティブなオブジェクトも含める
hierarchy -r -a

# 名前でフィルタ（ワイルドカード）
hierarchy -n "Player*"
hierarchy -n "*Enemy*"

# コンポーネントでフィルタ
hierarchy -c Rigidbody
hierarchy -r -c "UnityEngine.UI.Image"

# タグでフィルタ
hierarchy -t Player

# レイヤーでフィルタ
hierarchy -y 5
hierarchy -y "UI"

# 特定のオブジェクトの子を表示
hierarchy /Canvas

# ロード済みシーンを一覧表示
hierarchy -s list

# 特定のシーンを対象
hierarchy -s "Level1"

# フィルタを組み合わせ
hierarchy -r -a -n "*Manager*" -c MonoBehaviour
```

### 終了コード

| コード | 説明 |
|------|------|
| 0 | 成功 |
| 1 | 無効なフィルタパターンまたは不明なレイヤー/コンポーネント |
| 2 | GameObjectまたはシーンが見つからない |

---

## go

GameObjectを管理します（create、delete、find、rename、active、clone、info）。

### 書式

```bash
go <subcommand> [options] [arguments]
```

### サブコマンド

#### create

新しいGameObjectを作成します。

```bash
go create [name] [--primitive type] [--parent path] [--position x,y,z] [--rotation x,y,z] [--tag tag]
```

| オプション | 説明 |
|--------|------|
| `--primitive`, `-p` | プリミティブタイプ: Cube, Sphere, Capsule, Cylinder, Plane, Quad |
| `--parent` | 親オブジェクトのパス |
| `--position` | 初期ワールド位置 (x,y,z) |
| `--rotation` | 初期回転（オイラー角） (x,y,z) |
| `--tag`, `-t` | 割り当てるタグ |

**使用例:**
```bash
# 空のGameObjectを作成
go create MyObject

# 名前と位置を指定して作成
go create Player --position 0,1,0

# プリミティブを作成
go create MyCube --primitive Cube

# 別のオブジェクトの子として作成
go create Child --parent /Parent

# タグ付きで作成
go create Enemy --tag Enemy
```

#### delete

GameObjectを削除します。

```bash
go delete <path> [--immediate] [--children]
```

| オプション | 説明 |
|--------|------|
| `--immediate` | Destroyの代わりにDestroyImmediateを使用 |
| `--children` | 子のみを削除し、オブジェクト自体は保持 |

**使用例:**
```bash
# オブジェクトを削除
go delete /MyObject

# 即座に削除（エディタスクリプト用）
go delete /Temp --immediate

# 子のみを削除
go delete /Parent --children
```

#### find

GameObjectを検索します。

```bash
go find [-n name] [-t tag] [-c component] [-i]
```

| オプション | 説明 |
|--------|------|
| `-n`, `--name` | 名前パターン（部分一致、大文字小文字区別なし） |
| `-t`, `--tag` | タグ名 |
| `-c`, `--component` | コンポーネントタイプ |
| `-i`, `--inactive` | 非アクティブなオブジェクトも含める |

**使用例:**
```bash
# 名前で検索
go find -n Player
go find -n "Enemy"

# タグで検索
go find -t Player

# コンポーネントで検索
go find -c Rigidbody

# 非アクティブなオブジェクトも含める
go find -n Manager -i

# フィルタを組み合わせ
go find -t Enemy -c EnemyAI
```

#### rename

GameObjectの名前を変更します。

```bash
go rename <path> <new-name>
```

**使用例:**
```bash
go rename /OldName NewName
go rename /Player/Weapon Sword
```

#### active

アクティブ状態を取得または設定します。

```bash
go active <path> [--set true|false] [--toggle]
```

| オプション | 説明 |
|--------|------|
| `-s`, `--set` | アクティブ状態を設定 (true/false) |
| `--toggle` | 現在の状態を切り替え |

**使用例:**
```bash
# アクティブ状態を表示
go active /MyObject

# アクティブにする
go active /MyObject --set true

# 非アクティブにする
go active /MyObject --set false

# トグル
go active /MyObject --toggle
```

#### clone

GameObjectを複製します。

```bash
go clone <path> [-n name] [--parent path] [--count N]
```

| オプション | 説明 |
|--------|------|
| `-n`, `--name` | クローンの新しい名前 |
| `--parent` | クローンの親（デフォルト: 元と同じ） |
| `--count` | 作成するクローンの数 |

**使用例:**
```bash
# オブジェクトを複製
go clone /Template

# 新しい名前で複製
go clone /Enemy -n EnemyClone

# 複数複製
go clone /Bullet --count 10

# 別の親に複製
go clone /Prefab --parent /Container
```

#### info

GameObjectの詳細情報を表示します。

```bash
go info <path>
```

**出力内容:**
- 名前、パス、アクティブ状態
- タグ、レイヤー、静的フラグ
- Transform（位置、回転、スケール）
- コンポーネント一覧
- 子オブジェクト一覧

**使用例:**
```bash
go info /Player
```

### 終了コード

| コード | 説明 |
|------|------|
| 0 | 成功 |
| 1 | サブコマンドがない、または引数が無効 |
| 2 | GameObjectが見つからない、無効なプリミティブタイプ、または操作失敗 |

---

## transform

GameObjectのTransformを操作します。

### 書式

```bash
transform <path> [-p pos] [-P pos] [-r rot] [-R rot] [-s scale] [--parent path] [-w]
```

### 説明

Transformプロパティを取得または設定します。オプションなしの場合、現在のTransform情報を表示します。

### オプション

| オプション | ロング形式 | 説明 |
|--------|------|------|
| `-p` | `--position` | ワールド位置を設定 (x,y,z) |
| `-P` | `--local-position` | ローカル位置を設定 (x,y,z) |
| `-r` | `--rotation` | ワールド回転をオイラー角で設定 (x,y,z) |
| `-R` | `--local-rotation` | ローカル回転をオイラー角で設定 (x,y,z) |
| `-s` | `--scale` | ローカルスケールを設定（x,y,z または均一値） |
| | `--parent` | 親を設定（`/`、`null`、`none` で親を解除） |
| `-w` | `--world` | 親変更時にワールド位置を維持（デフォルト: true） |

### ベクトル形式

ベクトルは以下の形式で指定できます:
- `x,y,z` - 3つのコンポーネント
- `x,y` - 2つのコンポーネント（z = 0）
- `n` - 単一値（すべてのコンポーネントに適用）

### 使用例

```bash
# Transform情報を表示
transform /Player

# ワールド位置を設定
transform /Player -p 10,0,5

# ローカル位置を設定
transform /Player -P 0,1,0

# ワールド回転を設定
transform /Player -r 0,90,0

# ローカル回転を設定
transform /Player -R 45,0,0

# 均一スケールを設定
transform /Player -s 2

# 非均一スケールを設定
transform /Player -s 1,2,1

# 親を設定
transform /Child --parent /NewParent

# 親を解除（ルートに移動）
transform /Child --parent /
transform /Child --parent null

# 複数のプロパティを設定
transform /Player -p 0,1,0 -r 0,180,0 -s 1.5,1.5,1.5
```

### 出力

**情報表示:**
```
Transform: Player
  World Position:  (10.00, 0.00, 5.00)
  Local Position:  (10.00, 0.00, 5.00)
  World Rotation:  (0.00, 90.00, 0.00)
  Local Rotation:  (0.00, 90.00, 0.00)
  Local Scale:     (1.00, 1.00, 1.00)
  Parent:          (none)
  Children:        3
  Sibling Index:   0
```

**変更表示:**
```
Transform: Player
  Position: (0.00, 0.00, 0.00) -> (10.00, 0.00, 5.00)
```

### 終了コード

| コード | 説明 |
|------|------|
| 0 | 成功 |
| 1 | 無効なベクトル形式 |
| 2 | GameObjectまたは親が見つからない、循環参照 |

---

## component

GameObjectのコンポーネントを管理します。

### 書式

```bash
component <subcommand> <path> [arguments] [-a] [-v] [-i] [-n namespace]
```

### オプション（グローバル）

| オプション | ロング形式 | 説明 |
|--------|------|------|
| `-a` | `--all` | すべての一致するコンポーネントを削除 / すべてのメンバーを含める |
| `-v` | `--verbose` | 完全な型名を表示 |
| `-i` | `--immediate` | 削除にDestroyImmediateを使用 |
| `-n` | `--namespace` | 型解決のための名前空間 |

### サブコマンド

#### list

GameObjectのコンポーネントを一覧表示します。

```bash
component list <path> [-v]
```

**出力:**
```
Components on Player (5):
  [0] Transform
  [1] Rigidbody (enabled)
  [2] CapsuleCollider (enabled)
  [3] PlayerController (enabled)
  [4] Animator (disabled)
```

**詳細出力 (`-v`):**
```
Components on Player (5):
  [0] Transform                    UnityEngine.Transform
  [1] Rigidbody                    UnityEngine.Rigidbody
  ...
```

#### add

GameObjectにコンポーネントを追加します。

```bash
component add <path> <type>
```

**使用例:**
```bash
component add /Player Rigidbody
component add /Player BoxCollider
component add /Canvas "UnityEngine.UI.Image"
```

#### remove

GameObjectからコンポーネントを削除します。

```bash
component remove <path> <type|index> [-a] [-i]
```

**使用例:**
```bash
# 型名で削除
component remove /Player Rigidbody

# インデックスで削除
component remove /Player 2

# 指定型のすべてを削除
component remove /Player BoxCollider -a

# 即座に削除
component remove /Player Rigidbody --immediate
```

**注:** Transformは削除できません。

#### info

コンポーネントの詳細情報を表示します。

```bash
component info <path> <type|index>
```

**出力:**
```
Component: Rigidbody
  Type: UnityEngine.Rigidbody
  GameObject: /Player
  Enabled: true
  Properties:
    mass: 1 (Single)
    drag: 0 (Single)
    angularDrag: 0.05 (Single)
    useGravity: true (Boolean)
    ...
```

#### enable / disable

コンポーネントを有効または無効にします。

```bash
component enable <path> <type|index>
component disable <path> <type|index>
```

**使用例:**
```bash
component enable /Player Rigidbody
component disable /Player 3
```

**注:** Behaviour、Collider、Rendererコンポーネントのみが有効/無効をサポートします。

### 終了コード

| コード | 説明 |
|------|------|
| 0 | 成功 |
| 1 | 引数が不足、またはインデックスが範囲外 |
| 2 | GameObjectが見つからない、コンポーネントタイプが見つからない、追加/削除不可 |

---

## property

リフレクションを使用してコンポーネントのプロパティ値を取得または設定します。

### 書式

```bash
property <subcommand> <path> <component> [property] [value] [-a] [-s] [-n namespace]
```

### オプション

| オプション | ロング形式 | 説明 |
|--------|------|------|
| `-a` | `--all` | プライベートフィールドを含める |
| `-s` | `--serialized` | SerializeFieldメンバーのみを表示 |
| `-n` | `--namespace` | コンポーネント解決のための名前空間 |

### サブコマンド

#### list

コンポーネントのすべてのプロパティを一覧表示します。

```bash
property list <path> <component> [-a] [-s]
```

**出力:**
```
Properties of Rigidbody on /Player:
  mass                     float          1
  drag                     float          0
  angularDrag              float          0.05
  useGravity               bool           true
  isKinematic              bool           false
  velocity                 Vector3        (0.00, 0.00, 0.00) [readonly]
  ...
```

#### get

1つ以上のプロパティ値を取得します。

```bash
property get <path> <component> <property[,property2,...]>
```

**使用例:**
```bash
# 単一プロパティ
property get /Player Rigidbody mass

# 複数プロパティ
property get /Player Transform position,rotation,localScale

# 配列要素
property get /Renderer MeshRenderer materials[0]
```

**出力:**
```
Rigidbody.mass = 1 (float)
```

#### set

プロパティ値を設定します。

```bash
property set <path> <component> <property> <value>
```

### サポートされる値の型

| 型 | 形式 | 例 |
|------|--------|---------|
| int, float, double | 数値 | `10`, `3.14` |
| bool | true/false | `true`, `false` |
| string | テキスト | `"Hello World"` |
| Vector2 | x,y | `1.5,2.0` |
| Vector3 | x,y,z | `1,2,3` |
| Vector4 | x,y,z,w | `1,2,3,4` |
| Color | r,g,b,a (0-1) | `1,0,0,1`（赤） |
| Quaternion | x,y,z,w | `0,0,0,1` |
| Enum | 名前または値 | `ForceMode.Impulse`, `1` |
| 配列要素 | name[index] | `materials[0]` |
| 参照型 | パスまたはインスタンスID | `/Parent`, `#12345` |

### 使用例

```bash
# 数値
property set /Player Rigidbody mass 10
property set /Player Rigidbody drag 0.5

# ブール値
property set /Player Rigidbody useGravity false
property set /Player Rigidbody isKinematic true

# Vector3
property set /Player Transform position 0,1,0
property set /Player Transform localScale 2,2,2

# Color（RGBA、0-1範囲）
property set /Sprite SpriteRenderer color 1,0,0,1

# 文字列
property set /Text TextMesh text "Hello World"

# Enum
property set /Player Rigidbody interpolation Interpolate

# 配列要素
property set /Renderer MeshRenderer materials[0] MyMaterial

# 参照型（パス指定）
property set /Child Transform parent /Parent
property set /MyObject Transform parent null

# 参照型（インスタンスID指定）
# まずhierarchy -iでインスタンスIDを確認
hierarchy -i
# ├── Player #12345
# ├── Enemy #12346
# └── Target #12347

# インスタンスIDで参照を設定
property set /Enemy FollowTarget target #12345
```

### 終了コード

| コード | 説明 |
|------|------|
| 0 | 成功 |
| 1 | 無効な値形式または型変換失敗 |
| 2 | GameObject、コンポーネント、またはプロパティが見つからない；プロパティが読み取り専用 |

---

## scene

シーンを管理します。

### 書式

```bash
scene <subcommand> [options] [arguments]
```

### サブコマンド

#### list

シーン一覧を表示します。

```bash
scene list [-a] [-l]
```

| オプション | 説明 |
|--------|------|
| `-a` | Build Settings内のすべてのシーンを表示 |
| `-l` | 詳細情報を表示 |

**使用例:**
```bash
# ロード済みシーン一覧
scene list

# Build Settingsの全シーン一覧（ロード状態も表示）
scene list -a
```

#### load

シーンをロードします。

```bash
scene load <scene-name> [--additive] [--async]
```

| オプション | 説明 |
|--------|------|
| `--additive` | 追加ロード（既存シーンを維持） |
| `--async` | 非同期ロード |

**使用例:**
```bash
# シーンをロード
scene load GameScene

# 追加ロード + 非同期
scene load GameScene --additive --async
```

#### unload

シーンをアンロードします。

```bash
scene unload <scene-name>
```

**使用例:**
```bash
scene unload GameScene
```

#### active

アクティブシーンを取得または設定します。

```bash
scene active [scene-name]
```

**使用例:**
```bash
# 現在のアクティブシーンを取得
scene active

# アクティブシーンを変更
scene active GameScene
```

#### info

シーン情報を表示します。

```bash
scene info [scene-name]
```

**使用例:**
```bash
# 現在のシーン情報
scene info

# 特定シーンの情報
scene info GameScene
```

#### create（エディタのみ）

新しいシーンを作成します。

```bash
scene create <scene-name> [--setup]
```

| オプション | 説明 |
|--------|------|
| `--setup` | デフォルトオブジェクト（カメラ、ライト）を含める |

**使用例:**
```bash
scene create NewScene --setup
```

### 終了コード

| コード | 説明 |
|------|------|
| 0 | 成功 |
| 1 | サブコマンドがない、または引数が無効 |
| 2 | シーンが見つからない、またはロード/アンロード失敗 |

---

## 実用例

### シーンセットアップスクリプト

```bash
# ゲーム構造を作成
go create GameManager
go create Player --primitive Capsule --position 0,1,0
go create Ground --primitive Plane
transform /Ground -s 10,1,10

# 物理を追加
component add /Player Rigidbody
component add /Player CapsuleCollider
component add /Ground MeshCollider

# プレイヤーの物理を設定
property set /Player Rigidbody mass 1
property set /Player Rigidbody drag 0.5
property set /Player Rigidbody angularDrag 0.5

# 敵を作成
go create EnemySpawner
go create Enemy --primitive Cube --position 5,1,0 --tag Enemy
go clone /Enemy --count 4
```

### デバッグと検査

```bash
# すべてのRigidbodyを検索
hierarchy -r -c Rigidbody

# プレイヤーの状態を検査
go info /Player
component list /Player -v
property list /Player Rigidbody

# Transformヒエラルキーを確認
hierarchy /Player -r -l

# 非アクティブなオブジェクトを検索
hierarchy -r -a | grep "\[-\]"

# シーン構造をエクスポート
hierarchy -r -l > scene_structure.txt
```

### ランタイム変更

```bash
# オブジェクトの表示を切り替え
go active /UI/PauseMenu --toggle

# プレイヤー位置をリセット
transform /Player -p 0,1,0 -r 0,0,0

# すべての敵AIを無効化
go find -t Enemy -c EnemyAI | component disable EnemyAI

# マテリアルの色を変更
property set /Player MeshRenderer materials[0].color 0,1,0,1
```
