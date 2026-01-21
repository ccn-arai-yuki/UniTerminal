# Unity Commands

Commands for manipulating Unity GameObjects, Transforms, and Components.

## hierarchy

Display the scene hierarchy.

```bash
# Show root objects
hierarchy

# Recursive display
hierarchy -r

# With details (active state, tag, layer)
hierarchy -l

# Limit depth
hierarchy -r -d 3

# Filter by name (supports wildcards)
hierarchy -n "Player*"
hierarchy -n "*Enemy*"

# Filter by component
hierarchy -c Rigidbody
hierarchy -c "UnityEngine.UI.Image"

# Filter by tag
hierarchy -t Player

# Filter by layer
hierarchy -y 8
hierarchy -y "UI"

# Show inactive objects
hierarchy -a

# Include sibling index
hierarchy -s
```

**Options:**

| Option | Description |
|--------|-------------|
| `-r` | Recursive (show children) |
| `-d <depth>` | Maximum depth |
| `-l` | Long format (show details) |
| `-a` | Include inactive objects |
| `-s` | Show sibling index |
| `-n <pattern>` | Filter by name |
| `-c <type>` | Filter by component type |
| `-t <tag>` | Filter by tag |
| `-y <layer>` | Filter by layer (name or number) |

## go

GameObject operations.

### Create

```bash
# Create empty GameObject
go create MyObject

# Create at position
go create MyObject -P 1,2,3

# Create as child
go create Child -t /Parent

# Create primitive
go create Cube --primitive=Cube
go create Sphere --primitive=Sphere
```

### Delete

```bash
# Delete by path
go delete /MyObject

# Delete by name pattern
go delete -n "Temp*"
```

### Find

```bash
# Find by name
go find -n "Player"
go find -n "*Enemy*"

# Find by tag
go find -t Player

# Find by component
go find -c Rigidbody
```

### Clone

```bash
# Clone object
go clone /Original

# Clone with new name
go clone /Original -n Clone

# Clone multiple
go clone /Template --count 5

# Clone as sibling
go clone /Original -s
```

### Active State

```bash
# Set active
go active /MyObject true
go active /MyObject false

# Toggle
go active /MyObject --toggle
```

**Options:**

| Option | Description |
|--------|-------------|
| `--primitive` | Primitive type (Cube, Sphere, etc.) |
| `-P <pos>` | Position (x,y,z) |
| `-t <parent>` | Parent transform path |
| `-n <name>` | Object name / name pattern |
| `-c <type>` | Component type filter |
| `--count` | Number of clones |
| `-s` | Clone as sibling |
| `--toggle` | Toggle active state |

## transform

Transform manipulation.

```bash
# Get transform info
transform /MyObject

# Set world position
transform /MyObject -p 1,2,3

# Set local position
transform /MyObject -P 0,1,0

# Set world rotation (euler angles)
transform /MyObject -r 0,90,0

# Set local rotation
transform /MyObject -R 45,0,0

# Set scale
transform /MyObject -s 2,2,2

# Set parent
transform /Child --parent /NewParent

# Unparent (move to root)
transform /Child --parent ""

# World space flag
transform /MyObject -w -p 10,0,10
```

**Options:**

| Option | Description |
|--------|-------------|
| `-p <pos>` | World position (x,y,z) |
| `-P <pos>` | Local position (x,y,z) |
| `-r <rot>` | World rotation euler (x,y,z) |
| `-R <rot>` | Local rotation euler (x,y,z) |
| `-s <scale>` | Local scale (x,y,z) |
| `--parent` | Set parent by path |
| `-w` | Use world space |

## component

Component management.

### List Components

```bash
# List all components
component list /MyObject

# Verbose output
component list /MyObject -v
```

### Add Component

```bash
# Add by type name
component add /MyObject Rigidbody
component add /MyObject BoxCollider

# Add by full type name
component add /MyObject "UnityEngine.UI.Image"
```

### Remove Component

```bash
# Remove component
component remove /MyObject Rigidbody
```

### Enable/Disable

```bash
# Enable component
component enable /MyObject BoxCollider

# Disable component
component disable /MyObject BoxCollider
```

**Options:**

| Option | Description |
|--------|-------------|
| `-v` | Verbose output |
| `-a` | Include all (inherited) members |
| `-i` | Case-insensitive type matching |
| `-n <index>` | Component index (when multiple) |

## property

Property value operations via reflection.

### List Properties

```bash
# List component properties
property list /MyObject Rigidbody

# Include all members
property list /MyObject Transform -a
```

### Get Property

```bash
# Get property value
property get /MyObject Rigidbody mass
property get /MyObject Transform position
```

### Set Property

```bash
# Set simple value
property set /MyObject Rigidbody mass 10
property set /MyObject Rigidbody useGravity true

# Set Vector3
property set /MyObject Transform position 1,2,3
property set /MyObject Transform localScale 2,2,2

# Set Color
property set /MyObject SpriteRenderer color 1,0,0,1

# Set string
property set /MyObject TextMesh text "Hello World"
```

**Options:**

| Option | Description |
|--------|-------------|
| `-a` | Show all members |
| `-s` | Static members only |
| `-n <index>` | Component index |

## Examples

### Scene Setup Script

```bash
# Create game structure
go create GameManager
go create Player --primitive=Capsule -P 0,1,0
go create Ground --primitive=Plane -P 0,0,0
transform /Ground -s 10,1,10

# Add components
component add /Player Rigidbody
component add /Player CapsuleCollider
component add /Ground BoxCollider

# Configure physics
property set /Player Rigidbody mass 1
property set /Player Rigidbody drag 0.5
```

### Find and Modify Objects

```bash
# Find all enemies and disable
hierarchy -r -c EnemyAI | grep Enemy

# Batch transform
go find -t Enemy | transform -s 2,2,2
```

### Debug Inspection

```bash
# Inspect object
hierarchy /Player -l
component list /Player -v
property list /Player Rigidbody

# Export hierarchy
hierarchy -r -l > scene_dump.txt
```
