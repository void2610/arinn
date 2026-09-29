# arinn

Unity uGUI の UI フォーカスとナビゲーションを管理するライブラリ。ウィンドウを開くときにフォーカスを預かり、閉じたら返す。フォーカスが消えたら自動で戻す。

名前は古ノルド語の *arinn*（炉床）に由来する。ラテン語の *focus* も元は「炉床」を意味する。

## 機能

- **ウィンドウスタック**: `UIFocusManager.ShowWindow` / `HideWindow` で、表示・フォーカス・入力受付をまとめて切り替える。二重に開いても積み直さない。閉じ始めた時点で入力を切る
- **フォーカスの貸し借り**: 開く前のフォーカスを預かり、閉じたら返す。最後のウィンドウを閉じたら基底画面（`SwitchBase` / `SetBaseFocusSource`）の既定要素へ戻す
- **フォーカス監視**: フォーカスが消えたら、直前の要素がまだ操作できればすぐ戻す。消えていれば 0.5 秒待ってから、最前面のウィンドウ（なければ基底画面）の既定要素へ戻す
- **常時表示 UI**: スタックに積まずにフォーカスだけを借りる（`EnterPersistentUIFocus` / `ExitPersistentUIFocus`）
- **Cancel**: `TryPopScope` がウィンドウを閉じ、なければ常時表示 UI から戻る
- **遷移**: 見た目の出し入れは `IWindowTransition` で差し替える。既定は即時、LitMotion があれば `FadeWindowTransition` が使える
- **ナビゲーション**: 画面ごとに `NavigationScope` を登録すると、`NavigationController` が EventSystem の移動を止めて、スコープの中の候補から移動先を決める。スコープの外（ウィンドウの背面の UI）へは飛ばない。Inspector の Navigation 設定は読まず、見えている RectTransform の位置から毎回決める
- **端の挙動**: `Stop`（既定）/ `Exit(target)` / `WrapRow` を方向ごとに宣言する。候補から外すものは `Exclude`
- **仮想カーソル**: Selectable で表せない対象（タイル・手札など）は `GridCursor` / `ListCursor` を `CursorResolver` に渡す。フォーカスはライブラリ内部のアンカーが受ける
- **選択追従スクロール・ホバー選択**: `WithScrollIntoView` で選択に ScrollRect を追従させる。ポインタが動いたときだけホバーで選択する（パッドの操作を上書きしない）

## インストール

`Packages/manifest.json` に追加する:

```json
"com.void2610.arinn": "https://github.com/void2610/arinn.git"
```

UniTask・R3・VContainer が必要（git パッケージのため `package.json` の依存には書いていない）。

- `Void2610.Arinn.LitMotion`（`FadeWindowTransition`）は LitMotion があるときだけ有効になる
- `Void2610.Arinn.InputSystem`（`InputSystemSubmitHoldProbe` / `InputSystemNavigationInput` / `UseInputSystem`）は Input System があるときだけ有効になる
- `Void2610.Arinn.LiminalPalette`（`Arinn/*` の観測コマンド）は LiminalPalette があり、Editor か Development Build のときだけ有効になる

## 使い方

```csharp
// LifetimeScope（シーンを跨ぐなら親のスコープ）
builder.RegisterArinn(manager => manager.DefaultTransition = new FadeWindowTransition(0.2f));

// 画面遷移
manager.SwitchBase(battleView);   // IFocusSource

// ウィンドウ
manager.ShowWindow(pauseView);    // WindowBase の派生
manager.HideWindow(pauseView);

// Cancel 入力
if (manager.TryPopScope()) return;
```

DI を通さない View からは `UIFocusManager.Instance` で引ける。

### ナビゲーション

```csharp
// LifetimeScope（RegisterArinn と同じスコープか、その子）
builder.RegisterArinnNavigation(navigation => navigation
    .UseInputSystem()                                              // InputSystemUIInputModule の move を読む・ホバー選択
    .SetMoveBlocker(() => Gamepad.current?.leftShoulder.isPressed == true)); // LB + 十字キーは別の操作

// ウィンドウ（Awake など）
var scope = new NavigationScope(transform)
    .OnEdge(NavigationDirection.Down, EdgePolicy.Exit(closeButton))
    .Exclude(selectable => tabButtons.Contains(selectable))
    .WithScrollIntoView(scrollRect);
_registration = NavigationController.Instance.Register(this, scope); // 画面を破棄したら自動で外れる

// 仮想カーソル
var cursor = new GridCursor(5, 3, cell => !IsSoldOut(cell)).OnEdge(NavigationDirection.Down, EdgePolicy.Exit(closeButton));
cursor.OnMoved.Subscribe(cell => Highlight(cell));
cursor.OnSubmitted.Subscribe(_ => Buy(cursor.Position));
var resolver = new CursorResolver(cursor, gridArea);             // gridArea の全面にアンカーを置く
NavigationController.Instance.Register(this, new NavigationScope(transform).UseResolver(resolver));
SetDefaultFocusElement(resolver.Anchor);                         // 開いたらカーソルから始める
```

- 今のスコープは、最前面のウィンドウ（なければ基底画面）に登録したもの。スコープを登録していない画面では Unity の移動に任せる
- `SelectionChanged` で選択の変化を受け取れる。きっかけ（方向入力・ホバー・それ以外）を `Source` で区別できるので、選択の演出は利用側で書く
- 入力の仕組みは `INavigationInput`、ポインタの位置は `IPointerPositionSource` で差し替える（Core は Input System を知らない）

## テスト

`Tests/Editor` に EditMode テストを同梱している。利用側の `Packages/manifest.json` の `testables` に入れると実行される:

```json
"testables": ["com.void2610.arinn"]
```
