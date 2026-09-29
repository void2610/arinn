# arinn

Unity uGUI の UI フォーカスとナビゲーションを管理するライブラリ。ウィンドウを開くときにフォーカスを預かり、閉じたら返す。フォーカスが消えたら自動で戻す。方向入力は今開いている画面の中だけで解決し、ウィンドウの背面の UI へは飛ばさない。

名前は古ノルド語の *arinn*（炉床）に由来する。ラテン語の *focus* も元は「炉床」を意味する。

## 守っていること

| | 不変条件 | 仕組み |
|---|---|---|
| I1 | 表示・フォーカス・入力受付は同時にしか変わらない | `WindowBase.Show` / `Hide` は `protected internal`。開閉は `UIFocusManager` からだけ |
| I2 | フォーカスは借りたら返す | ウィンドウを開くときに今の選択を一緒に積み、閉じたらそこへ戻す |
| I3 | フォーカスは常にどこかにある | 選択が消えたら監視が戻す（直前の要素 → 最前面のウィンドウ → 基底画面の既定要素） |
| I4 | テスト用の口を production の型に持たせない | E2E 用の注入口は無い。LiminalPalette へは観測だけを公開する |
| I5 | ナビゲーションはスコープの外へ出ない | スコープの中の候補だけから移動先を決める |

設計の経緯と決定記録は [`Documentation~/design.html`](Documentation~/design.html) にある。

## インストール

`Packages/manifest.json` に追加する:

```json
"com.void2610.arinn": "https://github.com/void2610/arinn.git"
```

UniTask・R3・VContainer が必要（git パッケージのため `package.json` の依存には書いていない）。

| アセンブリ | 有効になる条件 | 中身 |
|---|---|---|
| `Void2610.Arinn` | 常に | コア（フォーカス・ウィンドウ・ナビゲーション・仮想カーソル） |
| `Void2610.Arinn.LitMotion` | LitMotion がある | `FadeWindowTransition` |
| `Void2610.Arinn.InputSystem` | Input System がある | `InputSystemSubmitHoldProbe` / `InputSystemNavigationInput` / `InputSystemPointerPosition` / `UseInputSystem` |
| `Void2610.Arinn.LiminalPalette` | LiminalPalette があり、Editor か Development Build | `Arinn/*` の観測コマンド |

コアの asmdef は `autoReferenced: false` なので、使う側の asmdef の `references` に `Void2610.Arinn` を足す。

## クイックスタート

```csharp
// LifetimeScope（シーンを跨ぐなら親のスコープ）
builder.RegisterArinn(manager =>
{
    manager.DefaultTransition = new FadeWindowTransition(0.2f);
    manager.SetSubmitHoldProbe(new InputSystemSubmitHoldProbe());
});
builder.RegisterArinnNavigation(navigation => navigation.UseInputSystem());
```

```csharp
public sealed class PauseView : WindowBase
{
    [SerializeField] private Button resumeButton;

    protected override void Awake()
    {
        base.Awake();
        SetDefaultFocusElement(resumeButton);
    }

    // Awake の時点ではコンテナの構築が済んでいないことがあるので、Start で登録する
    private void Start() => NavigationController.Instance.Register(this, new NavigationScope(transform));
}

// 画面遷移（基底画面を切り替える）
manager.SwitchBase(battleView);   // IFocusSource

// ウィンドウ
manager.ShowWindow(pauseView);
manager.HideWindow(pauseView);

// Cancel 入力
if (manager.TryPopScope()) return;
```

VContainer を使わない場合は `new UIFocusManager()` / `new NavigationController(manager)` で作り、毎フレーム `Tick()`、終わりに `Dispose()` を呼ぶ（[サンプル](#サンプル) がこの形）。DI を通さない View からは `UIFocusManager.Instance` / `NavigationController.Instance` で引ける。

## フォーカスとウィンドウ

### 用語

- **ウィンドウ**: `WindowBase` の派生。スタックに積まれ、開いている間はフォーカスを預かる。CanvasGroup で表示と入力受付を切り替える
- **基底画面**: ウィンドウの下にある画面（戦闘画面・タイトル画面など）。`IFocusSource` を実装した何か。ウィンドウが 1 枚も無いときのフォーカスの戻り先になる
- **常時表示 UI**: 画面に出しっぱなしの UI（HUD のボタンなど）。スタックに積まず、フォーカスだけを借りる
- **既定要素**: `IFocusSource.DefaultFocusElement`。フォーカスする時点で評価されるので、動的に作った要素も返せる

### WindowBase

| メンバー | 説明 |
|---|---|
| `closeButton` / `SetCloseButton` | 押すと閉じるボタン。SerializeField でもコードでも指定できる |
| `defaultFocusElement` / `SetDefaultFocusElement` | 開いたときの既定要素。`DefaultFocusElement` をオーバーライドすれば動的に決められる |
| `IsClosableByCancelInput` | false にすると Cancel（`TryCloseTopWindow` / `TryPopScope`）で閉じない |
| `Transition` | このウィンドウだけ遷移を変える場合にオーバーライドする |
| `OnWindowClosed` | 閉じたときに発火する。購読側で別のウィンドウを開いてよい |
| `IsVisible` | 最後に開いたか閉じたか（フェード中の見た目ではない） |

閉じ始めた時点で入力を切る。フェードが途中で止まっても、見えないウィンドウがクリックを吸い続けることはない。

### UIFocusManager

| メンバー | 説明 |
|---|---|
| `ShowWindow` / `HideWindow` / `ToggleWindow` | 開閉。開いたフォーカスは次のフレームで既定要素へ移る（同じフレームの上書きを避け、動的に作る要素も拾う）。二重に開いても積み直さない |
| `TryCloseTopWindow` | 最前面のウィンドウを閉じる |
| `TryPopScope` | ウィンドウがあれば閉じ、なければ常時表示 UI から戻る。Cancel の既定の処理 |
| `CloseAll` | すべて閉じて基底画面へ戻す |
| `SwitchBase` | 基底画面を切り替える。ウィンドウをすべて閉じ、新しい基底画面の既定要素へ移す（非アクティブならアクティブになるまで待つ） |
| `SetBaseFocusSource` | 基底画面だけを差し替える。フォーカスは動かさない |
| `EnterPersistentUIFocus` / `ExitPersistentUIFocus` / `TogglePersistentUIFocus` | 常時表示 UI へフォーカスを借りる・返す |
| `RegisterToggleAction` | 入力（R3 の Observable）でウィンドウを開閉するショートカット。他のウィンドウが開いている間は開かない |
| `SetInputScopeGate` | 最初のウィンドウが開いた・最後のウィンドウが閉じたときに、ゲームプレイ側の入力を止める・戻す |
| `SetSubmitHoldProbe` | 決定が押されたままかを調べる手段 |
| `DefaultTransition` | 遷移を指定していないウィンドウの遷移。既定は即時 |
| `FocusRecoveryDelaySeconds` | 直前の要素が消えていたとき、既定要素へ戻すまでの待ち時間（既定 0.5 秒） |
| `TopWindow` / `WindowCount` / `HasOpenWindows` / `IsInPersistentUIMode` / `IsFocusOnDefaultElement` | 観測用 |

### フォーカスの戻り先

- 上のウィンドウを閉じると、開く前に選択していた要素へ戻る。消えていれば新しい最前面のウィンドウの既定要素へ
- 最後のウィンドウを閉じると、基底画面があれば基底画面の既定要素へ戻る（開く前の要素ではなく、戻り先を 1 つに決めるため）。基底画面が無ければ開く前の要素へ
- 下のウィンドウを閉じても、最前面のフォーカスは動かない
- 選択が消えたら、直前の要素がまだ操作できればすぐ戻す。消えていれば `FocusRecoveryDelaySeconds` 待ってから最前面のウィンドウ（なければ基底画面）の既定要素へ戻す

### 常時表示 UI

LB などのショートカットで HUD のボタンへフォーカスを移すときに使う。`SetSubmitHoldProbe` を設定しておくと、決定が離されるまでフォーカスを移さない（決定との同時押しで移ったとき、決定の離しで移動先のボタンが押されるのを防ぐ）。

```csharp
manager.TogglePersistentUIFocus(hudFirstButton, hudRoot); // 同じ owner なら戻り、別なら借り直す
```

### シーン切り替え

アクティブシーンが変わると、スタック・基底画面・`IInputScopeGate`・常時表示 UI の状態を捨てる。新しいシーンで設定し直す。

## ナビゲーション

ウィンドウ（と、スコープを登録した基底画面）では、`NavigationController` が EventSystem の移動を止め、スコープの中の候補から移動先を決める。Inspector の Navigation 設定は読まない。

### スコープ

```csharp
var scope = new NavigationScope(transform)                    // この配下の Selectable が候補
    .OnEdge(NavigationDirection.Down, EdgePolicy.Exit(closeButton))
    .OnEdge(NavigationDirection.Right, EdgePolicy.WrapRow)
    .Exclude(selectable => tabButtons.Contains(selectable))
    .WithScrollIntoView(scrollRect);
var registration = NavigationController.Instance.Register(this, scope);
```

- 今のスコープは、最前面のウィンドウ（なければ基底画面）に登録したもの。常時表示 UI を借りている間は、今の選択を含むスコープ
- スコープを登録していないウィンドウは、そのウィンドウの `transform` を根とする既定のスコープで封じ込める（登録は端の挙動・除外・解決器を変えたいときだけでよい）。封じ込めを外すなら `resolvesMove: false` のスコープを登録する。スコープを登録していない基底画面は Unity の移動に任せる
- 候補は Root の配下で、アクティブで、操作できる Selectable。Scrollbar と `Exclude` に当てはまるものは外す
- 移動先は、入力の時点の RectTransform の位置から決める。進行方向に最も近く、直交する軸で揃っている候補を優先する。段の違う端から斜めの別の段へは飛ばない
- 選択がスコープの外にあるときに方向を押すと、動かす代わりにスコープの既定要素へ戻す
- `new NavigationScope(root, resolvesMove: false)` にすると、移動は Unity に任せて選択追従スクロールだけを行う。ホバー選択はスコープの中に絞ったまま
- 登録は MonoBehaviour の画面なら破棄で自動で外れる。それ以外は返り値の `Dispose` か `Unregister` で外す

### 端の挙動

| 宣言 | 動き |
|---|---|
| `EdgePolicy.Stop` | その場に留まる。宣言しなかった方向の既定 |
| `EdgePolicy.Exit(target)` | 指定した要素へ抜ける。スコープ外・操作不可・自分自身なら止まる |
| `EdgePolicy.WrapRow` | 同じ行（上下なら列）の反対側の端へ回り込む |
| `scope.Exclude(predicate)` | 候補そのものから外す（端の挙動とは別） |

### 入力

```csharp
navigation
    .UseInputSystem()                                                     // move を読む・ホバー選択を有効にする
    .SetMoveBlocker(() => Gamepad.current?.leftShoulder.isPressed == true); // LB + 十字キーは別の操作
```

- `UseInputSystem` は EventSystem の `InputSystemUIInputModule` の move を複製して読み、スコープが移動を解決している間は module の move を止める。リピートの遅延と間隔は module の `moveRepeatDelay` / `moveRepeatRate` に従う
- `SetMoveBlocker` の条件が成り立つ間は選択を動かさない。EventSystem の移動も戻さない（アプリ側で止めている move を勝手に有効へ戻さないため）
- 別の入力の仕組みを使う場合は `INavigationInput` を実装して `SetInput` に渡す

### 選択の変化

```csharp
navigation.SelectionChanged
    .Where(change => change.ByInput)
    .Subscribe(change => PlayMoveSound());
```

`Source` は `Input`（方向入力）・`Hover`（ポインタ）・`Program`（それ以外）。選択の演出は利用側で書く。

### ホバー選択

`EnableHoverSelection(pointer, ignore)`（`UseInputSystem` なら既定で有効）。ポインタが動いたときだけ、最前面に当たった Selectable を選択する。止まっている間はパッドの操作を上書きしない。スコープの外の要素は選ばず、ホバーで変わった選択にはスクロールを追従させない。

### 選択追従スクロール

`WithScrollIntoView(scrollRect)` を宣言すると、選択が ScrollRect の content の子孫なら、見える位置までスクロールする。Selectable でない対象は `ScrollIntoView.EnsureVisible(scrollRect, rect)` を直接呼ぶ。

## 仮想カーソル

タイルのマス・手札・スロットなど、Selectable で表せない対象の上を動くカーソル。`CursorResolver` が対象の範囲の全面に見えないアンカー（Selectable）を置き、EventSystem のフォーカスはアンカーが受ける。アンカーが選択されている間の方向入力はカーソルへ、決定はアンカー経由でカーソルへ届く。Cancel は通常の経路のまま。

```csharp
var cursor = new GridCursor(5, 3, cell => !IsSoldOut(cell))   // 止まれないマスは飛ばす
    .OnEdge(NavigationDirection.Left, EdgePolicy.WrapRow)
    .OnEdge(NavigationDirection.Down, EdgePolicy.Exit(closeButton));
cursor.OnMoved.Subscribe(Highlight).AddTo(this);
cursor.OnFocusChanged.Subscribe(ShowCursor).AddTo(this);
cursor.OnSubmitted.Subscribe(_ => Buy(cursor.Position)).AddTo(this);

var resolver = new CursorResolver(cursor, gridArea);           // gridArea はスコープの Root の配下
NavigationController.Instance.Register(this, new NavigationScope(transform).UseResolver(resolver));
SetDefaultFocusElement(resolver.Anchor);                       // 開いたらカーソルから始める
```

| 型 | 説明 |
|---|---|
| `GridCursor` | 格子。位置は (列, 行) で左上が (0, 0)。`SetSize` / `SetPosition` |
| `ListCursor` | 一列。`isHorizontal` で左右か上下か。並びと直交する方向は端として扱う。`SetCount` / `SetIndex` |
| `CursorResolver` | カーソルを持つスコープの解決器。アンカー以外が選択されているときは `fallback`（既定は座標で解決）に任せるので、周りのボタンからはカーソルのある方向へ押せば入れる |
| `IVirtualCursor` | 独自のカーソルを作る場合の窓口 |

1 画面に複数のカーソルを置く場合は、`CursorResolver` の `fallback` に別の `CursorResolver` を渡して重ねる。

## 拡張点

| インターフェイス | 用途 | 付属の実装 |
|---|---|---|
| `IFocusSource` | 既定要素を返す画面 | `WindowBase` |
| `IWindowTransition` | ウィンドウの見た目の出し入れ | `InstantWindowTransition` / `FadeWindowTransition` |
| `IInputScopeGate` | ウィンドウの開閉に合わせたゲームプレイ入力の停止 | なし（アプリ側で Action Map を切り替える） |
| `ISubmitHoldProbe` | 決定が押されたままか | `InputSystemSubmitHoldProbe` |
| `INavigationInput` | 方向入力と EventSystem の移動の停止 | `InputSystemNavigationInput` |
| `IPointerPositionSource` | ホバー選択のポインタ位置 | `InputSystemPointerPosition` |
| `INavigationResolver` | スコープの中での移動先の決め方 | `SpatialResolver` / `CursorResolver` |

## LiminalPalette

| コマンド | 返り値 |
|---|---|
| `Arinn/TopWindow` | 最前面のウィンドウの名前。無ければ `(none)` |
| `Arinn/WindowCount` | 開いているウィンドウの数 |
| `Arinn/IsFocusOnDefault` | フォーカスが最前面のウィンドウ（なければ基底画面）の既定要素にあるか |
| `Arinn/IsInPersistentUIMode` | 常時表示 UI にフォーカスを借りているか |
| `Arinn/Selected` | 選択中の要素の名前 |
| `Arinn/ActiveScope` | 有効なスコープの Root の名前 |
| `Arinn/IsSelectedInScope` | 選択中の要素が有効なスコープの中にあるか |

観測だけで、操作の注入口は持たない。UI の操作は実入力の経路（noema など）で行う。

## ライブラリが持たないもの

- **Cancel の優先順位**: `TryCloseTopWindow` / `ExitPersistentUIFocus` / `TryPopScope` という部品だけを出す。ポーズを開く・画面固有の処理へ委ねるといった順序はアプリで組む
- **選択の演出**: `SelectionChanged` を購読して各プロジェクトで書く
- **入力ガイド**（デバイスごとのアイコン）: アセットへの依存が入るので別パッケージの範囲
- **マーカーコンポーネント・Inspector での振る舞いの宣言**: 既定要素・端の挙動・除外・スコープはすべてコードで宣言する

## サンプル

Package Manager の arinn のページの Samples から **Minimal** を Import する。空のシーンの GameObject に `MinimalSample` を付けて再生すると、次の構成が UI ごとコードで組み立てられる（Input System が必要）。

- **基底画面**（`MenuScreen`）: 上下の端で回り込むメニュー
- **設定**（`SettingsWindow`）: 選択に追従してスクロールする一覧。右端から右上の閉じるボタンへ抜ける
- **持ち物**（`InventoryWindow`）: `GridCursor` のグリッド。鍵のかかったマスを飛ばし、左右は回り込み、下端から閉じるボタンへ抜ける
- **確認ダイアログ**（`ConfirmDialog`）: どのウィンドウの上にも重ねて開け、閉じると開いた元のボタンへフォーカスが戻る

## テスト

`Tests/Editor` に EditMode テストを同梱している。利用側の `Packages/manifest.json` の `testables` に入れると実行される:

```json
"testables": ["com.void2610.arinn"]
```
