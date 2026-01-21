# FlyweightScrollView

A high-performance virtual scrolling component for displaying large amounts of data efficiently.

## Overview

FlyweightScrollView uses the **flyweight pattern** to render only visible items, making it ideal for:

- Terminal output with thousands of lines
- Log viewers
- Large lists and tables
- Any scrollable content with many items

## Features

- **Virtual scrolling** - Only renders visible items
- **Efficient memory usage** - Reuses UI elements
- **CircularBuffer** - Fixed-size buffer with automatic old entry removal
- **Vertical & Horizontal** - Supports both scroll directions
- **Data binding** - Works with ObservableCollection

## Components

### FlyweightScrollView

The main scroll view component.

| Component | Description |
|-----------|-------------|
| `FlyweightVerticalScrollView` | Vertical scrolling |
| `FlyweightHorizontalScrollView` | Horizontal scrolling |

### CircularBuffer

A fixed-size buffer that automatically removes old entries when full.

```csharp
using Xeon.Common.FlyweightScrollView.Model;

// Create buffer with max 1000 items
var buffer = new CircularBuffer<string>(1000);

// Add items
buffer.Add("Line 1");
buffer.Add("Line 2");

// When full, oldest items are automatically removed
for (int i = 0; i < 2000; i++)
{
    buffer.Add($"Line {i}");  // Only last 1000 remain
}

// Access items
string first = buffer[0];
int count = buffer.Count;  // Max 1000
```

## Setup

### 1. Create Item View

Create a script that inherits from `FlyweightScrollViewItemBase`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using Xeon.Common.FlyweightScrollView;

public class LogItemView : FlyweightScrollViewItemBase<string>
{
    [SerializeField] private Text _text;

    public override void Bind(string data)
    {
        _text.text = data;
    }

    public override void Unbind()
    {
        _text.text = string.Empty;
    }
}
```

### 2. Create Item Prefab

1. Create a UI element (e.g., Panel with Text)
2. Add your item view script
3. Set the RectTransform size (this determines item height/width)
4. Save as prefab

### 3. Setup ScrollView

```csharp
using UnityEngine;
using Xeon.Common.FlyweightScrollView;
using Xeon.Common.FlyweightScrollView.Model;

public class TerminalDisplay : MonoBehaviour
{
    [SerializeField] private FlyweightVerticalScrollView _scrollView;
    [SerializeField] private LogItemView _itemPrefab;

    private CircularBuffer<string> _logBuffer;

    void Start()
    {
        // Create buffer (max 1000 lines)
        _logBuffer = new CircularBuffer<string>(1000);

        // Initialize scroll view
        _scrollView.Initialize<string, LogItemView>(_itemPrefab, _logBuffer);
    }

    public void AddLog(string message)
    {
        _logBuffer.Add(message);

        // Scroll to bottom (optional)
        _scrollView.ScrollToEnd();
    }
}
```

## Integration with Terminal

### Basic Terminal Display

```csharp
using UnityEngine;
using Xeon.UniTerminal;
using Xeon.Common.FlyweightScrollView;
using Xeon.Common.FlyweightScrollView.Model;
using System.Threading;

public class TerminalUI : MonoBehaviour
{
    [SerializeField] private FlyweightVerticalScrollView _scrollView;
    [SerializeField] private LogItemView _itemPrefab;
    [SerializeField] private InputField _inputField;

    private Terminal _terminal;
    private CircularBuffer<string> _outputBuffer;

    void Start()
    {
        _outputBuffer = new CircularBuffer<string>(1000);
        _scrollView.Initialize<string, LogItemView>(_itemPrefab, _outputBuffer);

        _terminal = new Terminal(
            Application.dataPath,
            Application.dataPath,
            true
        );

        _inputField.onEndEdit.AddListener(OnCommandSubmit);
    }

    async void OnCommandSubmit(string command)
    {
        if (string.IsNullOrEmpty(command)) return;

        _inputField.text = "";
        _outputBuffer.Add($"> {command}");

        var stdout = new ListTextWriter();
        var stderr = new ListTextWriter();

        await _terminal.ExecuteAsync(
            command,
            stdout,
            stderr,
            destroyCancellationToken
        );

        // Add output lines
        foreach (var line in stdout.Lines)
        {
            _outputBuffer.Add(line);
        }

        // Add error lines
        foreach (var line in stderr.Lines)
        {
            _outputBuffer.Add($"[Error] {line}");
        }

        _scrollView.ScrollToEnd();
        _inputField.ActivateInputField();
    }
}
```

### With UniTask

```csharp
#if UNI_TERMINAL_UNI_TASK_SUPPORT
using Cysharp.Threading.Tasks;

async UniTaskVoid ExecuteCommand(string command)
{
    var stdout = new UniTaskListTextWriter();
    var stderr = new UniTaskListTextWriter();

    await _terminal.ExecuteUniTaskAsync(command, stdout, stderr);

    foreach (var line in stdout.Lines)
    {
        _outputBuffer.Add(line);
    }

    _scrollView.ScrollToEnd();
}
#endif
```

## API Reference

### FlyweightScrollViewBase

| Method | Description |
|--------|-------------|
| `Initialize<TData, TView>(prefab, collection)` | Initialize with data source |
| `ScrollToEnd()` | Scroll to the last item |
| `ScrollToStart()` | Scroll to the first item |
| `ScrollToIndex(int index)` | Scroll to specific index |
| `Refresh()` | Force refresh visible items |

### CircularBuffer<T>

| Property/Method | Description |
|-----------------|-------------|
| `Capacity` | Maximum number of items |
| `Count` | Current number of items |
| `Add(T item)` | Add item (removes oldest if full) |
| `Clear()` | Remove all items |
| `this[int index]` | Get item at index |

### FlyweightScrollViewItemBase<T>

| Method | Description |
|--------|-------------|
| `Bind(T data)` | Called when item becomes visible |
| `Unbind()` | Called when item becomes hidden |

## Performance Tips

1. **Set appropriate buffer size** - Balance memory vs history length
2. **Keep item views simple** - Minimize components per item
3. **Use object pooling** - FlyweightScrollView handles this automatically
4. **Batch additions** - Add multiple items, then call Refresh once

```csharp
// Good: Batch additions
foreach (var line in lines)
{
    _buffer.Add(line);
}
_scrollView.ScrollToEnd();  // Single refresh

// Avoid: Refresh after each addition
foreach (var line in lines)
{
    _buffer.Add(line);
    _scrollView.ScrollToEnd();  // Multiple refreshes
}
```

## Prefabs

UniTerminal includes ready-to-use prefabs:

- `FlyweightVerticalScrollView.prefab`
- `FlyweightHorizontalScrollView.prefab`

Located in: `Packages/jp.xeon.uni-terminal/Runtime/Prefabs/`
