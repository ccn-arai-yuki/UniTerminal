# Unity Commands

Commands for manipulating Unity GameObjects, Transforms, and Components.

## hierarchy

Display the scene hierarchy in a tree structure.

### Synopsis

```bash
hierarchy [-r] [-d depth] [-a] [-l] [-s scene] [-n pattern] [-c component] [-t tag] [-y layer] [path]
```

### Description

Displays GameObjects in the current scene as a tree structure. Can filter by name, component, tag, or layer. When a path is specified, shows children of that object.

### Options

| Option | Long | Description |
|--------|------|-------------|
| `-r` | `--recursive` | Show children recursively |
| `-d` | `--depth` | Maximum depth to display (-1 = unlimited, default) |
| `-a` | `--all` | Include inactive objects |
| `-l` | `--long` | Show detailed information (active state, component count, tag) |
| `-s` | `--scene` | Target scene name. Use `list` to show loaded scenes |
| `-n` | `--name` | Filter by name (supports `*` and `?` wildcards) |
| `-c` | `--component` | Filter by component type |
| `-t` | `--tag` | Filter by tag |
| `-y` | `--layer` | Filter by layer name or number (0-31) |

### Arguments

| Argument | Description |
|----------|-------------|
| `path` | Optional. Show children of this object instead of scene roots. |

### Output Formats

**Normal format:**
```
Scene: SampleScene (3 root objects)
├── Main Camera
├── Directional Light
└── Player
    ├── Model
    └── Weapon
```

**Long format (`-l`):**
```
Scene: SampleScene (3 root objects)
├── [A] Main Camera              (4 components) [MainCamera]
├── [A] Directional Light        (2 components) [Untagged]
└── [A] Player                   (5 components) [Player]
```

`[A]` = Active, `[-]` = Inactive

### Examples

```bash
# Show root objects in active scene
hierarchy

# Recursive display
hierarchy -r

# Show with details
hierarchy -l

# Limit depth to 3 levels
hierarchy -r -d 3

# Include inactive objects
hierarchy -r -a

# Filter by name (wildcards)
hierarchy -n "Player*"
hierarchy -n "*Enemy*"

# Filter by component
hierarchy -c Rigidbody
hierarchy -r -c "UnityEngine.UI.Image"

# Filter by tag
hierarchy -t Player

# Filter by layer
hierarchy -y 5
hierarchy -y "UI"

# Show children of specific object
hierarchy /Canvas

# List loaded scenes
hierarchy -s list

# Target specific scene
hierarchy -s "Level1"

# Combine filters
hierarchy -r -a -n "*Manager*" -c MonoBehaviour
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | Invalid filter pattern or unknown layer/component |
| 2 | GameObject or scene not found |

---

## go

Manage GameObjects (create, delete, find, rename, active, clone, info).

### Synopsis

```bash
go <subcommand> [options] [arguments]
```

### Subcommands

#### create

Create a new GameObject.

```bash
go create [name] [--primitive type] [--parent path] [--position x,y,z] [--rotation x,y,z] [--tag tag]
```

| Option | Description |
|--------|-------------|
| `--primitive`, `-p` | Primitive type: Cube, Sphere, Capsule, Cylinder, Plane, Quad |
| `--parent` | Parent object path |
| `--position` | Initial world position (x,y,z) |
| `--rotation` | Initial rotation in euler angles (x,y,z) |
| `--tag`, `-t` | Tag to assign |

**Examples:**
```bash
# Create empty GameObject
go create MyObject

# Create with name and position
go create Player --position 0,1,0

# Create primitive
go create MyCube --primitive Cube

# Create as child of another object
go create Child --parent /Parent

# Create with tag
go create Enemy --tag Enemy
```

#### delete

Delete a GameObject.

```bash
go delete <path> [--immediate] [--children]
```

| Option | Description |
|--------|-------------|
| `--immediate` | Use DestroyImmediate instead of Destroy |
| `--children` | Delete only children, keep the object itself |

**Examples:**
```bash
# Delete object
go delete /MyObject

# Delete immediately (for editor scripts)
go delete /Temp --immediate

# Delete only children
go delete /Parent --children
```

#### find

Search for GameObjects.

```bash
go find [-n name] [-t tag] [-c component] [-i]
```

| Option | Description |
|--------|-------------|
| `-n`, `--name` | Name pattern (partial match, case-insensitive) |
| `-t`, `--tag` | Tag name |
| `-c`, `--component` | Component type |
| `-i`, `--inactive` | Include inactive objects |

**Examples:**
```bash
# Find by name
go find -n Player
go find -n "Enemy"

# Find by tag
go find -t Player

# Find by component
go find -c Rigidbody

# Include inactive objects
go find -n Manager -i

# Combine filters
go find -t Enemy -c EnemyAI
```

#### rename

Rename a GameObject.

```bash
go rename <path> <new-name>
```

**Examples:**
```bash
go rename /OldName NewName
go rename /Player/Weapon Sword
```

#### active

Get or set active state.

```bash
go active <path> [--set true|false] [--toggle]
```

| Option | Description |
|--------|-------------|
| `-s`, `--set` | Set active state (true/false) |
| `--toggle` | Toggle current state |

**Examples:**
```bash
# Show active state
go active /MyObject

# Activate
go active /MyObject --set true

# Deactivate
go active /MyObject --set false

# Toggle
go active /MyObject --toggle
```

#### clone

Clone a GameObject.

```bash
go clone <path> [-n name] [--parent path] [--count N]
```

| Option | Description |
|--------|-------------|
| `-n`, `--name` | New name for clone(s) |
| `--parent` | Parent for clones (default: same as original) |
| `--count` | Number of clones to create |

**Examples:**
```bash
# Clone object
go clone /Template

# Clone with new name
go clone /Enemy -n EnemyClone

# Clone multiple
go clone /Bullet --count 10

# Clone to different parent
go clone /Prefab --parent /Container
```

#### info

Display detailed information about a GameObject.

```bash
go info <path>
```

**Output includes:**
- Name, path, active state
- Tag, layer, static flag
- Transform (position, rotation, scale)
- Component list
- Children list

**Example:**
```bash
go info /Player
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | Missing subcommand or invalid arguments |
| 2 | GameObject not found, invalid primitive type, or operation failed |

---

## transform

Manipulate GameObject transforms.

### Synopsis

```bash
transform <path> [-p pos] [-P pos] [-r rot] [-R rot] [-s scale] [--parent path] [-w]
```

### Description

Gets or sets transform properties. Without options, displays current transform information.

### Options

| Option | Long | Description |
|--------|------|-------------|
| `-p` | `--position` | Set world position (x,y,z) |
| `-P` | `--local-position` | Set local position (x,y,z) |
| `-r` | `--rotation` | Set world rotation in euler angles (x,y,z) |
| `-R` | `--local-rotation` | Set local rotation in euler angles (x,y,z) |
| `-s` | `--scale` | Set local scale (x,y,z or single value for uniform) |
| | `--parent` | Set parent (use `/`, `null`, or `none` to unparent) |
| `-w` | `--world` | Maintain world position when changing parent (default: true) |

### Vector Format

Vectors can be specified as:
- `x,y,z` - Three components
- `x,y` - Two components (z = 0)
- `n` - Single value (applied to all components)

### Examples

```bash
# Display transform info
transform /Player

# Set world position
transform /Player -p 10,0,5

# Set local position
transform /Player -P 0,1,0

# Set world rotation
transform /Player -r 0,90,0

# Set local rotation
transform /Player -R 45,0,0

# Set uniform scale
transform /Player -s 2

# Set non-uniform scale
transform /Player -s 1,2,1

# Set parent
transform /Child --parent /NewParent

# Unparent (move to root)
transform /Child --parent /
transform /Child --parent null

# Set multiple properties
transform /Player -p 0,1,0 -r 0,180,0 -s 1.5,1.5,1.5
```

### Output

**Information display:**
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

**Modification display:**
```
Transform: Player
  Position: (0.00, 0.00, 0.00) -> (10.00, 0.00, 5.00)
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | Invalid vector format |
| 2 | GameObject or parent not found, circular reference |

---

## component

Manage GameObject components.

### Synopsis

```bash
component <subcommand> <path> [arguments] [-a] [-v] [-i] [-n namespace]
```

### Options (Global)

| Option | Long | Description |
|--------|------|-------------|
| `-a` | `--all` | Remove all matching components / include all members |
| `-v` | `--verbose` | Show full type names |
| `-i` | `--immediate` | Use DestroyImmediate for removal |
| `-n` | `--namespace` | Namespace for type resolution |

### Subcommands

#### list

List components on a GameObject.

```bash
component list <path> [-v]
```

**Output:**
```
Components on Player (5):
  [0] Transform
  [1] Rigidbody (enabled)
  [2] CapsuleCollider (enabled)
  [3] PlayerController (enabled)
  [4] Animator (disabled)
```

**Verbose output (`-v`):**
```
Components on Player (5):
  [0] Transform                    UnityEngine.Transform
  [1] Rigidbody                    UnityEngine.Rigidbody
  ...
```

#### add

Add a component to a GameObject.

```bash
component add <path> <type>
```

**Examples:**
```bash
component add /Player Rigidbody
component add /Player BoxCollider
component add /Canvas "UnityEngine.UI.Image"
```

#### remove

Remove a component from a GameObject.

```bash
component remove <path> <type|index> [-a] [-i]
```

**Examples:**
```bash
# Remove by type name
component remove /Player Rigidbody

# Remove by index
component remove /Player 2

# Remove all of type
component remove /Player BoxCollider -a

# Remove immediately
component remove /Player Rigidbody --immediate
```

**Note:** Transform cannot be removed.

#### info

Display detailed component information.

```bash
component info <path> <type|index>
```

**Output:**
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

Enable or disable a component.

```bash
component enable <path> <type|index>
component disable <path> <type|index>
```

**Examples:**
```bash
component enable /Player Rigidbody
component disable /Player 3
```

**Note:** Only Behaviour, Collider, and Renderer components support enable/disable.

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | Missing arguments or index out of range |
| 2 | GameObject not found, component type not found, cannot add/remove |

---

## property

Get or set component property values via reflection.

### Synopsis

```bash
property <subcommand> <path> <component> [property] [value] [-a] [-s] [-n namespace]
```

### Options

| Option | Long | Description |
|--------|------|-------------|
| `-a` | `--all` | Include private fields |
| `-s` | `--serialized` | Show only SerializeField members |
| `-n` | `--namespace` | Component namespace for resolution |

### Subcommands

#### list

List all properties of a component.

```bash
property list <path> <component> [-a] [-s]
```

**Output:**
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

Get one or more property values.

```bash
property get <path> <component> <property[,property2,...]>
```

**Examples:**
```bash
# Single property
property get /Player Rigidbody mass

# Multiple properties
property get /Player Transform position,rotation,localScale

# Array element
property get /Renderer MeshRenderer materials[0]
```

**Output:**
```
Rigidbody.mass = 1 (float)
```

#### set

Set a property value.

```bash
property set <path> <component> <property> <value>
```

### Supported Value Types

| Type | Format | Example |
|------|--------|---------|
| int, float, double | Number | `10`, `3.14` |
| bool | true/false | `true`, `false` |
| string | Text | `"Hello World"` |
| Vector2 | x,y | `1.5,2.0` |
| Vector3 | x,y,z | `1,2,3` |
| Vector4 | x,y,z,w | `1,2,3,4` |
| Color | r,g,b,a (0-1) | `1,0,0,1` (red) |
| Quaternion | x,y,z,w | `0,0,0,1` |
| Enum | Name or value | `ForceMode.Impulse`, `1` |
| Array element | name[index] | `materials[0]` |

### Examples

```bash
# Numeric values
property set /Player Rigidbody mass 10
property set /Player Rigidbody drag 0.5

# Boolean
property set /Player Rigidbody useGravity false
property set /Player Rigidbody isKinematic true

# Vector3
property set /Player Transform position 0,1,0
property set /Player Transform localScale 2,2,2

# Color (RGBA, 0-1 range)
property set /Sprite SpriteRenderer color 1,0,0,1

# String
property set /Text TextMesh text "Hello World"

# Enum
property set /Player Rigidbody interpolation Interpolate

# Array element
property set /Renderer MeshRenderer materials[0] MyMaterial
```

### Exit Codes

| Code | Description |
|------|-------------|
| 0 | Success |
| 1 | Invalid value format or type conversion failed |
| 2 | GameObject, component, or property not found; property is read-only |

---

## Practical Examples

### Scene Setup Script

```bash
# Create game structure
go create GameManager
go create Player --primitive Capsule --position 0,1,0
go create Ground --primitive Plane
transform /Ground -s 10,1,10

# Add physics
component add /Player Rigidbody
component add /Player CapsuleCollider
component add /Ground MeshCollider

# Configure player physics
property set /Player Rigidbody mass 1
property set /Player Rigidbody drag 0.5
property set /Player Rigidbody angularDrag 0.5

# Create enemies
go create EnemySpawner
go create Enemy --primitive Cube --position 5,1,0 --tag Enemy
go clone /Enemy --count 4
```

### Debug and Inspection

```bash
# Find all Rigidbodies
hierarchy -r -c Rigidbody

# Inspect player state
go info /Player
component list /Player -v
property list /Player Rigidbody

# Check transform hierarchy
hierarchy /Player -r -l

# Find inactive objects
hierarchy -r -a | grep "\[-\]"

# Export scene structure
hierarchy -r -l > scene_structure.txt
```

### Runtime Modifications

```bash
# Toggle object visibility
go active /UI/PauseMenu --toggle

# Reset player position
transform /Player -p 0,1,0 -r 0,0,0

# Disable all enemy AI
go find -t Enemy -c EnemyAI | component disable EnemyAI

# Change material color
property set /Player MeshRenderer materials[0].color 0,1,0,1
```
