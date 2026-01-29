# FlyweightScrollView

大量のデータを効率的に表示するための高パフォーマンス仮想スクロールコンポーネントです。

## 概要

FlyweightScrollViewは**フライウェイトパターン**を使用して、表示されているアイテムのみをレンダリングします。以下の用途に最適です:

- 数千行のターミナル出力
- ログビューア
- 大きなリストやテーブル
- 多数のアイテムを持つスクロール可能なコンテンツ

## 特徴

- **仮想スクロール** - 表示されているアイテムのみをレンダリング
- **効率的なメモリ使用** - UI要素を再利用
- **CircularBuffer** - 古いエントリを自動削除する固定サイズバッファ
- **垂直・水平** - 両方のスクロール方向をサポート
- **データバインディング** - ObservableCollectionと連携

## コンポーネント

### FlyweightScrollView

メインのスクロールビューコンポーネントです。

| コンポーネント | 説明 |
|-----------|------|
| `FlyweightVerticalScrollView` | 垂直スクロール |
| `FlyweightHorizontalScrollView` | 水平スクロール |

### CircularBuffer

満杯になると自動的に古いエントリを削除する固定サイズバッファです。

```csharp
using Xeon.Common.FlyweightScrollView.Model;

// 最大1000アイテムのバッファを作成
var buffer = new CircularBuffer<string>(1000);

// アイテムを追加
buffer.Add("Line 1");
buffer.Add("Line 2");

// 満杯になると、最も古いアイテムが自動的に削除される
for (int i = 0; i < 2000; i++)
{
    buffer.Add($"Line {i}");  // 最後の1000行のみ残る
}

// アイテムにアクセス
string first = buffer[0];
int count = buffer.Count;  // 最大1000
```

## セットアップ

### 1. アイテムビューの作成

`FlyweightScrollViewItemBase` を継承するスクリプトを作成:

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

### 2. アイテムプレハブの作成

1. UI要素（例: TextのあるPanel）を作成
2. アイテムビュースクリプトを追加
3. RectTransformのサイズを設定（アイテムの高さ/幅を決定）
4. プレハブとして保存

### 3. ScrollViewのセットアップ

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
        // バッファを作成（最大1000行）
        _logBuffer = new CircularBuffer<string>(1000);

        // スクロールビューを初期化
        _scrollView.Initialize<string, LogItemView>(_itemPrefab, _logBuffer);
    }

    public void AddLog(string message)
    {
        _logBuffer.Add(message);

        // 末尾にスクロール（オプション）
        _scrollView.ScrollToEnd();
    }
}
```

## Terminalとの統合

### 基本的なターミナル表示

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

        // 出力行を追加
        foreach (var line in stdout.Lines)
        {
            _outputBuffer.Add(line);
        }

        // エラー行を追加
        foreach (var line in stderr.Lines)
        {
            _outputBuffer.Add($"[Error] {line}");
        }

        _scrollView.ScrollToEnd();
        _inputField.ActivateInputField();
    }
}
```

### UniTaskを使用する場合

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

## APIリファレンス

### FlyweightScrollViewBase

| メソッド | 説明 |
|--------|------|
| `Initialize<TData, TView>(prefab, collection)` | データソースで初期化 |
| `ScrollToEnd()` | 最後のアイテムまでスクロール |
| `ScrollToStart()` | 最初のアイテムまでスクロール |
| `ScrollToIndex(int index)` | 特定のインデックスまでスクロール |
| `Refresh()` | 表示アイテムを強制リフレッシュ |

### CircularBuffer<T>

| プロパティ/メソッド | 説明 |
|-----------------|------|
| `Capacity` | アイテムの最大数 |
| `Count` | 現在のアイテム数 |
| `Add(T item)` | アイテムを追加（満杯なら最も古いものを削除） |
| `Clear()` | すべてのアイテムを削除 |
| `this[int index]` | インデックスのアイテムを取得 |

### FlyweightScrollViewItemBase<T>

| メソッド | 説明 |
|--------|------|
| `Bind(T data)` | アイテムが表示されるときに呼び出される |
| `Unbind()` | アイテムが非表示になるときに呼び出される |

## パフォーマンスのヒント

1. **適切なバッファサイズを設定** - メモリと履歴の長さのバランス
2. **アイテムビューはシンプルに** - アイテムごとのコンポーネントを最小限に
3. **オブジェクトプーリングを使用** - FlyweightScrollViewが自動的に処理
4. **追加をバッチ処理** - 複数アイテムを追加してから一度だけリフレッシュ

```csharp
// 良い例: バッチ追加
foreach (var line in lines)
{
    _buffer.Add(line);
}
_scrollView.ScrollToEnd();  // 一度だけリフレッシュ

// 避ける: 追加ごとにリフレッシュ
foreach (var line in lines)
{
    _buffer.Add(line);
    _scrollView.ScrollToEnd();  // 複数回リフレッシュ
}
```

## プレハブ

UniTerminalには使用可能なプレハブが含まれています:

- `FlyweightVerticalScrollView.prefab`
- `FlyweightHorizontalScrollView.prefab`

場所: `Packages/jp.xeon.uni-terminal/Runtime/Prefabs/`
