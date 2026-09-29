# arinn

Unity uGUI の UI フォーカスとナビゲーションを管理するライブラリ。
ウィンドウを開くときにフォーカスを預かり、閉じたら返す。
フォーカスが消えたら自動で戻す。
方向入力は今開いている画面の中だけで解決し、ウィンドウの背面の UI へは飛ばさない。

名前は古ノルド語の *arinn*（炉床）に由来する。
ラテン語の *focus* も元は「炉床」を意味する。

## ドキュメント

| 文書 | 内容 |
|---|---|
| [チュートリアル](Documentation~/tutorial.md) | 組み込みから、ウィンドウ、ナビゲーション、仮想カーソル、E2E テストまでを順に説明する |
| [移行ガイド](Documentation~/migration.md) | 「庭小人の庭」と void-red の旧実装との対応表と、置き換える順番 |
| [設計メモ](Documentation~/design.html) | 抽出の計画、不変条件、決定記録、計画と実装の差分 |
| [CHANGELOG](CHANGELOG.md) | 版ごとの変更 |

この README は、機能と API の一覧である。

## 守っていること

| | 不変条件 | 仕組み |
|---|---|---|
| I1 | 表示とフォーカスと入力の受付は同時にしか変わらない | `WindowBase.Show` と `Hide` は `protected internal`。開閉は `UIFocusManager` からだけ行う |
| I2 | フォーカスは借りたら返す | ウィンドウを開くときに今の選択を一緒に積み、閉じたらそこへ戻す |
| I3 | フォーカスは常にどこかにある | 選択が消えたら、直前の要素、最前面のウィンドウの既定要素、基底画面の既定要素の順に戻す |
| I4 | テスト用の入口を production の型に持たせない | E2E 用の注入口はない。LiminalPalette へは観測だけを公開する |
| I5 | ナビゲーションはスコープの外へ出ない | スコープの中の候補だけから移動先を決める。スコープを登録していないウィンドウにも既定のスコープを使う |

## インストール

`Packages/manifest.json` に追加する。

```json
"com.void2610.arinn": "https://github.com/void2610/arinn.git"
```

UniTask、R3、VContainer が必要である（git パッケージのため `package.json` の依存には書いていない）。
コアの asmdef は `autoReferenced: false` なので、使う側の asmdef の `references` に `Void2610.Arinn` を足す。

| アセンブリ | 有効になる条件 | 中身 |
|---|---|---|
| `Void2610.Arinn` | 常に | フォーカス、ウィンドウ、ナビゲーション、仮想カーソル |
| `Void2610.Arinn.InputSystem` | Input System がある | `InputSystemNavigationInput`、`InputSystemPointerPosition`、`InputSystemSubmitHoldProbe`、`UseInputSystem` |
| `Void2610.Arinn.LitMotion` | LitMotion がある | `FadeWindowTransition` |
| `Void2610.Arinn.LiminalPalette` | LiminalPalette があり、Editor か Development Build | `Arinn/*` の観測コマンド |

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
        SetCloseButton(resumeButton);
    }
}

manager.SwitchBase(battleView);   // 基底画面（IFocusSource）を切り替える
manager.ShowWindow(pauseView);    // 開く。ナビゲーションは pauseView の中に閉じ込められる
manager.HideWindow(pauseView);    // 閉じて、開く前のフォーカスへ戻す
if (manager.TryPopScope()) return; // Cancel の既定の処理
```

VContainer を使わない場合は、`new UIFocusManager()` と `new NavigationController(manager)` で作り、毎フレーム `Tick()`、終わりに `Dispose()` を呼ぶ。
DI を通さない View からは `UIFocusManager.Instance` と `NavigationController.Instance` で引ける。
詳しくは[チュートリアルの第 1 節](Documentation~/tutorial.md#1-組み込み)にある。

## フォーカスとウィンドウ

- **ウィンドウ**：`WindowBase` の派生。スタックに積まれ、開いている間はフォーカスを預かる。
- **基底画面**：ウィンドウの下にある画面（戦闘画面やタイトル画面）。`IFocusSource` を実装したものなら何でもよく、ウィンドウが 1 枚もないときのフォーカスの戻り先になる。
- **常時表示 UI**：画面に出しっぱなしの UI（HUD のボタンなど）。スタックに積まず、フォーカスだけを借りる。
- **既定要素**：`IFocusSource.DefaultFocusElement`。フォーカスする時点で評価されるので、動的に作った要素も返せる。

### WindowBase

| メンバー | 説明 |
|---|---|
| `closeButton`、`SetCloseButton` | 押すと閉じるボタン。SerializeField でもコードでも指定できる |
| `defaultFocusElement`、`SetDefaultFocusElement` | 開いたときの既定要素。`DefaultFocusElement` をオーバーライドすれば開くたびに決められる |
| `IsClosableByCancelInput` | false にすると Cancel（`TryCloseTopWindow`、`TryPopScope`）で閉じない |
| `Transition` | このウィンドウだけ遷移を変えるときにオーバーライドする |
| `OnWindowClosed` | 閉じたときに発火する。購読側で別のウィンドウを開いてよい |
| `IsVisible` | 最後に開いたか閉じたか（フェード中の見た目ではない） |

### UIFocusManager

| メンバー | 説明 |
|---|---|
| `ShowWindow`、`HideWindow`、`ToggleWindow` | 開閉する。既定要素へのフォーカスは次のフレームで当たる。二重に開いても積み直さない |
| `TryCloseTopWindow` | 最前面のウィンドウを閉じる |
| `TryPopScope` | ウィンドウがあれば閉じ、なければ常時表示 UI から戻る |
| `CloseAll` | すべて閉じて基底画面へ戻す |
| `SwitchBase` | 基底画面を切り替える。ウィンドウをすべて閉じ、新しい基底画面の既定要素へ移す |
| `SetBaseFocusSource` | 基底画面だけを差し替える。フォーカスは動かさない |
| `EnterPersistentUIFocus`、`ExitPersistentUIFocus`、`TogglePersistentUIFocus` | 常時表示 UI へフォーカスを借りる、返す |
| `RegisterToggleAction` | 入力（R3 の Observable）でウィンドウを開閉する。他のウィンドウが開いている間は開かない |
| `SetInputScopeGate` | 最初のウィンドウが開いたときと最後のウィンドウが閉じたときに、ゲームプレイの入力を止める、戻す |
| `SetSubmitHoldProbe` | 決定が押されたままかを調べる手段 |
| `DefaultTransition` | 遷移を指定していないウィンドウの遷移。既定は即時 |
| `FocusRecoveryDelaySeconds` | 直前の要素が消えていたとき、既定要素へ戻すまでの待ち時間（既定 0.5 秒） |
| `TopWindow`、`WindowCount`、`HasOpenWindows`、`IsInPersistentUIMode`、`IsFocusOnDefaultElement` | 観測用 |

フォーカスの戻り先の規則は[チュートリアルの第 4 節](Documentation~/tutorial.md#4-フォーカスが戻る先)にある。
アクティブなシーンが変わると、スタック、基底画面、`IInputScopeGate`、常時表示 UI の状態を捨てる。

## ナビゲーション

最前面のウィンドウ（なければ、スコープを登録した基底画面）の中で、`NavigationController` が EventSystem の move を止めて移動先を決める。
移動先は入力の時点の RectTransform の位置から決め、Inspector の Navigation 設定は読まない。

```csharp
var scope = new NavigationScope(transform)                    // この配下の Selectable が候補
    .OnEdge(NavigationDirection.Down, EdgePolicy.Exit(closeButton))
    .OnEdge(NavigationDirection.Right, EdgePolicy.WrapRow)
    .Exclude(selectable => tabButtons.Contains(selectable))
    .WithScrollIntoView(scrollRect);
var registration = NavigationController.Instance.Register(this, scope); // Start で登録する
```

| 宣言 | 動き |
|---|---|
| `EdgePolicy.Stop` | 端でその場に留まる。宣言しなかった方向の既定 |
| `EdgePolicy.Exit(target)` | 端から指定した要素へ抜ける。抜け先がスコープの外、操作できない、非アクティブ、自分自身なら止まる |
| `EdgePolicy.WrapRow` | 同じ行（上下の移動なら列）の反対側の端へ回り込む |
| `Exclude(predicate)` | 候補から外す。ホバーでは外さない |
| `WithScrollIntoView(scrollRect)` | 選択した要素が見える位置までスクロールする。ホバーでの選択には追従しない |
| `UseResolver(resolver)` | 移動先の決め方を差し替える |
| `new NavigationScope(root, resolvesMove: false)` | 移動を Unity に任せる。スクロールとホバーの範囲は効く |

| NavigationController のメンバー | 説明 |
|---|---|
| `Register`、`Unregister`、`GetScope` | 画面にスコープを結び付ける。MonoBehaviour の画面なら破棄で自動で外れる |
| `SetInput` | 方向入力。`UseInputSystem()` がまとめて設定する |
| `SetMoveBlocker` | 条件が成り立つ間は方向入力を移動として扱わない（LB を押しながらの十字キーなど） |
| `EnableHoverSelection`、`DisableHoverSelection` | ポインタが動いたときだけ、最前面に当たった Selectable を選ぶ。スコープの外は選ばない |
| `SelectionChanged` | 選択の変化。`Source` で方向入力（`Input`）、ホバー（`Hover`）、それ以外（`Program`）を区別する |
| `ActiveScope` | 直前の Tick で有効だったスコープ |

## 仮想カーソル

Selectable で表せない対象（タイルのマス、手札、スロットなど）の上を動くカーソル。
`CursorResolver` が対象の範囲の全面に見えないアンカー（Selectable）を置き、EventSystem のフォーカスはアンカーが受ける。
アンカーが選択されている間の方向入力はカーソルへ渡り、決定はアンカーを経由してカーソルへ届く。

```csharp
var cursor = new GridCursor(5, 3, cell => !IsSoldOut(cell))    // 止まれないマスは飛ばす
    .OnEdge(NavigationDirection.Down, EdgePolicy.Exit(closeButton));
cursor.OnMoved.Subscribe(Highlight).AddTo(this);
cursor.OnSubmitted.Subscribe(_ => Buy(cursor.Position)).AddTo(this);
var resolver = new CursorResolver(cursor, gridArea);
SetDefaultFocusElement(resolver.Anchor);                        // 開いたらカーソルから始める
NavigationController.Instance.Register(this, new NavigationScope(transform).UseResolver(resolver));
```

| 型 | 説明 |
|---|---|
| `GridCursor` | 格子。位置は (列, 行) で、左上が (0, 0) |
| `ListCursor` | 一列。並びと直交する方向の入力は端として扱う |
| `CursorResolver` | アンカー以外が選択されているときは `fallback`（既定は `SpatialResolver`）に任せる。重ねれば 1 画面に複数のカーソルを置ける |
| `IVirtualCursor`、`VirtualCursor` | 独自のカーソルを作るときの窓口と基底クラス |

## 拡張点

| インターフェイス | 用途 | 付属の実装 |
|---|---|---|
| `IFocusSource` | 既定要素を返す画面 | `WindowBase` |
| `IWindowTransition` | ウィンドウの見た目の出し入れ | `InstantWindowTransition`、`FadeWindowTransition` |
| `IInputScopeGate` | ウィンドウの開閉に合わせたゲームプレイ入力の停止 | なし（アプリ側で Action Map を切り替える） |
| `ISubmitHoldProbe` | 決定が押されたままか | `InputSystemSubmitHoldProbe` |
| `INavigationInput` | 方向入力と EventSystem の move の停止 | `InputSystemNavigationInput` |
| `IPointerPositionSource` | ホバー選択のポインタの位置 | `InputSystemPointerPosition` |
| `INavigationResolver` | スコープの中での移動先の決め方 | `SpatialResolver`、`CursorResolver` |

## E2E テストと LiminalPalette

UI の操作は実入力の経路で行う。
十字キーは noema の `Ui/Navigate`（仮想ゲームパッド）で押し、選択位置は `Ui/Focused` で観測する。
arinn の状態は、LiminalPalette の次のコマンドで観測できる。

| コマンド | 返り値 |
|---|---|
| `Arinn/TopWindow` | 最前面のウィンドウの GameObject 名。なければ `(none)` |
| `Arinn/WindowCount` | 開いているウィンドウの数 |
| `Arinn/IsFocusOnDefault` | フォーカスが最前面のウィンドウ（なければ基底画面）の既定要素にあるか |
| `Arinn/IsInPersistentUIMode` | 常時表示 UI にフォーカスを借りているか |
| `Arinn/Selected` | 選択中の GameObject 名 |
| `Arinn/ActiveScope` | 有効なスコープの根の GameObject 名 |
| `Arinn/IsSelectedInScope` | 選択中の要素が有効なスコープの中にあるか |

## ライブラリが持たないもの

- **Cancel の優先順位**：部品（`TryCloseTopWindow`、`ExitPersistentUIFocus`、`TryPopScope`）だけを出す。ポーズを開くなどの順序はアプリで組む。
- **選択の演出**：`SelectionChanged` を購読して各プロジェクトで書く。
- **入力ガイド**（デバイスごとのアイコン）：アセットへの依存が入るので、別パッケージの範囲とする。
- **マーカーコンポーネントと Inspector での振る舞いの宣言**：既定要素、端の挙動、除外、スコープは、すべてコードで宣言する。

## サンプル

Package Manager の arinn のページの Samples から **Minimal** を Import する。
空のシーンの GameObject に `MinimalSample` を付けて再生すると、次の構成が UI ごとコードで組み立てられる（Input System が必要）。

- **基底画面**（`MenuScreen`）：上下の端で回り込むメニュー。
- **設定**（`SettingsWindow`）：選択に合わせてスクロールする一覧。右端から右上の閉じるボタンへ抜ける。
- **持ち物**（`InventoryWindow`）：`GridCursor` のグリッド。鍵のかかったマスを飛ばし、左右は回り込み、下端から閉じるボタンへ抜ける。
- **確認ダイアログ**（`ConfirmDialog`）：どのウィンドウの上にも重ねて開け、閉じると開いた元のボタンへフォーカスが戻る。

## テストと CI

`Tests/Editor` に EditMode テストを、`Tests/Runtime/InputSystem` に Input System 連携の PlayMode テストを同梱している。
利用側の `Packages/manifest.json` の `testables` に入れると、利用側のテストと一緒に実行される。

```json
"testables": ["com.void2610.arinn"]
```

このリポジトリの CI（`.github/workflows/test.yml`）は、`.ci/UnityProject` の検証用プロジェクトで EditMode と PlayMode のテストを回す。
`Samples~` は Unity が読まないので、テストの前にサンプルを Assets へ写し、型の崩れをコンパイルで検出する。
CI を動かすには、リポジトリの Secrets に `UNITY_LICENSE`、`UNITY_EMAIL`、`UNITY_PASSWORD` を登録する（game-ci の要件）。
