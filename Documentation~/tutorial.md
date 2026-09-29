# arinn チュートリアル

このチュートリアルは、arinn を既存の uGUI プロジェクトへ組み込み、ウィンドウの開閉とゲームパッドのナビゲーションを任せるまでを順に説明する。
各節のコードは、その節までの設定が済んでいる前提で書いている。
API の一覧は [README](../README.md) にある。

## 1. 組み込み

arinn は UniTask、R3、VContainer に依存する。
git パッケージは `package.json` の依存を解決できないため、この 3 つは利用側で先に入れておく。

```json
"com.void2610.arinn": "https://github.com/void2610/arinn.git"
```

コアの asmdef（`Void2610.Arinn`）は `autoReferenced: false` なので、arinn を使うアセンブリの asmdef の `references` に `Void2610.Arinn` を足す。
Input System 用の実装を使うなら `Void2610.Arinn.InputSystem` も足す。

### VContainer で登録する

`UIFocusManager` と `NavigationController` は毎フレーム `Tick` する必要がある。
VContainer のエントリポイントとして登録すれば、Tick と Dispose をコンテナに任せられる。

```csharp
public sealed class RootLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterArinn(manager =>
        {
            manager.DefaultTransition = new FadeWindowTransition(0.15f);
            manager.SetSubmitHoldProbe(new InputSystemSubmitHoldProbe());
        });
        builder.RegisterArinnNavigation(navigation => navigation.UseInputSystem());
    }
}
```

シーンを跨いでウィンドウを扱うなら、シーンを跨いで生きる親の LifetimeScope に登録する。
`RegisterArinnNavigation` は `UIFocusManager` をコンストラクタで受け取るので、`RegisterArinn` と同じスコープか、その子に登録する。

### Presenter で画面を繋ぐ

View（ウィンドウと基底画面）はコンテナに登録しない。
シーンの LifetimeScope には Presenter だけを登録し、Presenter がコンストラクタで `FindFirstObjectByType` を使って View を取得する。

```csharp
public sealed class BattleLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder) => builder.RegisterEntryPoint<BattlePresenter>();
}

public sealed class BattlePresenter : IStartable, IDisposable
{
    private readonly UIFocusManager _focusManager;
    private readonly BattleView _battleView;
    private readonly PauseView _pauseView;
    private readonly CompositeDisposable _disposables = new();

    public BattlePresenter(UIFocusManager focusManager)
    {
        _focusManager = focusManager;
        _battleView = UnityEngine.Object.FindFirstObjectByType<BattleView>();
        _pauseView = UnityEngine.Object.FindFirstObjectByType<PauseView>();
    }

    public void Start()
    {
        _focusManager.SwitchBase(_battleView);
        _battleView.OnPauseClicked.Subscribe(_ => _pauseView.Open()).AddTo(_disposables);
    }

    public void Dispose() => _disposables.Dispose();
}
```

View が見つからないときの null チェックはしない。
見つからなければ最初のアクセスで null 参照の例外になり、原因がすぐ分かるからだ。
View は互いを参照せず、押されたことを Observable で知らせるだけにする。
どのウィンドウを開くかは Presenter が決める。
同梱のサンプル（`Samples~/Minimal`）がこの構成で書いてある。

ウィンドウの `Open()` と `Close()` は、`UIFocusManager.Instance`（`RegisterArinn` で生成したインスタンス）を使う。
ウィンドウを VContainer で注入する構成にした場合は、注入された `UIFocusManager` を優先して使う。

### VContainer を使わない場合

自分で生成し、毎フレーム `Tick`、終わりに `Dispose` を呼ぶ。

```csharp
public sealed class UIRoot : MonoBehaviour
{
    private UIFocusManager _focusManager;
    private NavigationController _navigation;

    private void Awake()
    {
        _focusManager = new UIFocusManager();
        _navigation = new NavigationController(_focusManager).UseInputSystem();
    }

    private void Update()
    {
        _focusManager.Tick();
        _navigation.Tick();
    }

    private void OnDestroy()
    {
        _navigation.Dispose();
        _focusManager.Dispose();
    }
}
```

この場合も、ウィンドウの `Open()` と `Close()` は `UIFocusManager.Instance` を使う。
`Instance` は最後に生成したインスタンスを指し、Dispose すると null に戻る。

### EventSystem の入力モジュール

`UseInputSystem` は EventSystem の `InputSystemUIInputModule` の move を読む。
旧 Input Manager の `StandaloneInputModule` では方向入力を読めないため、ナビゲーションは Unity の移動のまま動く（第 15 節で独自の入力を差し込める）。

## 2. 基底画面を決める

ウィンドウの下には、戦闘画面やタイトル画面のような画面が常にある。
arinn はこれを **基底画面** と呼び、ウィンドウが 1 枚もないときのフォーカスの戻り先に使う。

基底画面は `IFocusSource` を実装するだけでよい。
MonoBehaviour である必要はない。

```csharp
public sealed class BattleView : MonoBehaviour, IFocusSource
{
    [SerializeField] private Button endTurnButton;

    public GameObject DefaultFocusElement => endTurnButton.gameObject;
}
```

`DefaultFocusElement` はフォーカスする時点で評価される。
動的に生成した要素（手札の先頭のカードなど）を返してもよい。

画面を切り替えるときは `SwitchBase` を呼ぶ。
開いているウィンドウをすべて閉じ、新しい基底画面の既定要素へフォーカスを移す。
既定要素が非アクティブなら、アクティブになるまで最大 120 フレーム待つ。

```csharp
manager.SwitchBase(battleView);
```

フォーカスを動かさずに戻り先だけを差し替えたいときは `SetBaseFocusSource` を使う。
ゲームのステート（`GameState` など）と基底画面の対応は、アプリ側で持つ。
arinn はステートを知らない。

## 3. ウィンドウを作る

ポーズやショップのように、基底画面の上に重ねて開く画面は `WindowBase` を継承する。
`WindowBase` は CanvasGroup で表示と入力の受付を切り替えるので、ウィンドウの GameObject には CanvasGroup が自動で付く。

```csharp
public sealed class PauseView : WindowBase
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button titleButton;

    protected override void Awake()
    {
        base.Awake();
        SetDefaultFocusElement(resumeButton);
        SetCloseButton(resumeButton);
    }
}
```

既定要素と閉じるボタンは、Inspector の `defaultFocusElement` と `closeButton` でも指定できる。
コードで指定する API（`SetDefaultFocusElement` と `SetCloseButton`）を必ず用意してあるので、Inspector を使わずに完結させてもよい。
既定要素を開くたびに決めたいなら `DefaultFocusElement` をオーバーライドする。

`base.Awake()` は必ず呼ぶ。
ここでウィンドウを閉じた状態（alpha 0、入力を受け付けない）にするためだ。

### 開いて閉じる

ウィンドウは `Open()` で開き、`Close()` で閉じる。

```csharp
pauseView.Open();
pauseView.Close();
pauseView.Toggle();
```

中身は `UIFocusManager` の `ShowWindow`、`HideWindow`、`ToggleWindow` で、`manager.ShowWindow(pauseView)` と書いても同じである。
表示とフォーカスと入力の受付を同時に変えるため、開閉は必ずこの経路を通す。
`WindowBase` の `Show` と `Hide`（見た目と入力の受付だけを切り替える）は `protected internal` で、外からは呼べない。
`SetActive` や CanvasGroup を直接触って開くと、見えているのにフォーカスが背面に残る、という状態を作れてしまう。

開く前に準備が要るウィンドウ（確認ダイアログのメッセージなど）は、準備と `Open()` をまとめたメソッドを用意すると呼び出し側が短くなる。

```csharp
public void Open(string message, Action onYes)
{
    _message.text = message;
    _onYes = onYes;
    Open();
}
```

`Open()` は、開く前に選択していた要素を一緒に積む。
既定要素へのフォーカスは次のフレームで当たる。
同じフレームで他の処理がフォーカスを上書きするのを避け、開いた直後に生成される要素も拾えるようにするためだ。
同じウィンドウを二度開いても、スタックには一度しか積まない。

### 閉じたときの通知

`OnWindowClosed` は、閉じるボタン、Cancel、コードからの `HideWindow` のどれで閉じても発火する。
購読側で別のウィンドウを開いてもよい。

```csharp
resultView.OnWindowClosed.Subscribe(_ => rewardView.Open()).AddTo(this);
```

`Close()` は、閉じる演出の完了を待たずに入力を切る。
フェードの途中で演出が止まっても、見えないウィンドウがクリックを受け続けることはない。

### Cancel で閉じさせないウィンドウ

チュートリアルの強制進行のように、Cancel で閉じてはいけないウィンドウは `IsClosableByCancelInput` をオーバーライドする。

```csharp
public override bool IsClosableByCancelInput => false;
```

## 4. フォーカスが戻る先

ウィンドウを閉じたときのフォーカスの戻り先は、次の規則で決まる。

- **上のウィンドウを閉じた**：開く前に選択していた要素へ戻る。その要素が消えているか操作できなければ、新しく最前面になったウィンドウの既定要素へ移る。
- **下のウィンドウを閉じた**：最前面のフォーカスは動かない。
- **最後のウィンドウを閉じた**：基底画面があれば、その既定要素へ戻る。基底画面がなければ、開く前に選択していた要素へ戻る。

最後のウィンドウだけ開く前の要素へ戻さないのは、戻り先を 1 つに決めるためだ。
ポーズを開いたときにたまたまマウスで触れていた要素へ戻ると、パッドの利用者は現在地を見失う。

### フォーカスが消えたとき

`currentSelectedGameObject` は、要素の非アクティブ化や破棄、マウスでの空クリックで日常的に null になる。
arinn はこれを防ごうとせず、消えたら戻す。

- 直前に選択していた要素がまだ操作できれば、すぐに戻す。
- 直前の要素も消えていれば、`FocusRecoveryDelaySeconds`（既定 0.5 秒）待ってから、最前面のウィンドウの既定要素へ戻す。ウィンドウがなければ基底画面の既定要素へ戻す。

待つのは、画面の切り替え中に一瞬だけ選択が外れる場合に、無関係な要素へ飛ばないようにするためだ。

## 5. ゲームプレイの入力を止める

ウィンドウが開いている間は、キャラクターの操作などのゲームプレイの入力を止めたいことが多い。
`IInputScopeGate` を実装して渡すと、最初のウィンドウが開いたときと最後のウィンドウが閉じたときに呼ばれる。

```csharp
public sealed class ActionMapGate : IInputScopeGate
{
    private readonly PlayerInput _playerInput;

    public ActionMapGate(PlayerInput playerInput) => _playerInput = playerInput;

    public void OnFirstWindowOpened() => _playerInput.actions.FindActionMap("Gameplay").Disable();

    public void OnLastWindowClosed() => _playerInput.actions.FindActionMap("Gameplay").Enable();
}

manager.SetInputScopeGate(new ActionMapGate(playerInput));
```

2 枚目以降のウィンドウを開いても、途中のウィンドウを閉じても呼ばれない。
アクティブなシーンが切り替わると設定は外れるので、新しいシーンで設定し直す。

## 6. 常時表示 UI にフォーカスを借りる

HUD のボタンのように画面に出しっぱなしの UI へ、ショートカットでフォーカスを移したいことがある。
ウィンドウとして積むと Cancel やスタックの規則が効いてしまうので、arinn はスタックに積まずにフォーカスだけを借りる手段を持つ。

```csharp
// LB で HUD へ移り、もう一度 LB で元へ戻る
manager.TogglePersistentUIFocus(hudFirstButton, hudRoot);
```

第 2 引数の owner は、同じ UI かどうかの判定に使う。
同じ owner なら借りる前の要素へ戻り、別の owner なら借り直す。
`EnterPersistentUIFocus` と `ExitPersistentUIFocus` で個別に呼んでもよい。

決定ボタンとの同時押し（LB + A など）で借りると、A を離した瞬間に移動先のボタンが押されてしまう。
`SetSubmitHoldProbe` で決定の押下を調べる手段を渡しておくと、決定が離されるまでフォーカスを移さない。

```csharp
manager.SetSubmitHoldProbe(new InputSystemSubmitHoldProbe());                 // South、Space、Enter を見る
manager.SetSubmitHoldProbe(new InputSystemSubmitHoldProbe(submitAction));     // 決定の Action を見る
```

借りている間は、フォーカスが消えたときの自動復帰は働かない。
借りた側の制御に任せるためだ。

## 7. Cancel の順序を組む

Cancel（Esc や B）の処理の順序はプロジェクトごとに違うので、arinn は順序を持たない。
部品として `TryCloseTopWindow`、`ExitPersistentUIFocus`、そして両方を順に試す `TryPopScope` を出す。

```csharp
private void OnCancel(InputAction.CallbackContext context)
{
    if (_focusManager.TryPopScope()) return;          // ウィンドウを閉じる、なければ常時表示 UI から戻る
    if (_shop.IsOpen) { _shop.HandleCancel(); return; } // 画面固有の処理
    if (context.control.device is Keyboard) _pauseView.Open();
}
```

`TryPopScope` は、Cancel で閉じないウィンドウが最前面にあれば何もせず false を返す。

## 8. 開閉の演出

見た目の出し入れは `IWindowTransition` で差し替える。
既定は即時の切り替えで、LitMotion が入っていれば `FadeWindowTransition` が使える。

```csharp
manager.DefaultTransition = new FadeWindowTransition(0.2f);
```

ウィンドウごとに変えるなら `Transition` をオーバーライドする。

```csharp
protected override IWindowTransition Transition => _slideIn;
```

遷移は alpha などの見た目だけを扱う。
入力の受付は `WindowBase` が遷移の開始時点で切り替えるので、遷移の実装で `interactable` や `blocksRaycasts` を触る必要はない。
キャンセルされたら途中の状態で止まってよい（次の遷移が現在の値から始める）。

## 9. ナビゲーションを有効にする

ここまでの設定で `RegisterArinnNavigation(navigation => navigation.UseInputSystem())` を済ませていれば、ナビゲーションはもう動いている。
最前面のウィンドウが開いている間、`NavigationController` は EventSystem の move を止め、そのウィンドウの中だけから移動先を決める。

Unity の Automatic ナビゲーションは、開いているウィンドウを突き抜けて背面の UI へ飛ぶ。
arinn はウィンドウの外の候補をそもそも返さないので、背面へは飛ばない。
移動先は、入力の時点の RectTransform の位置から決める。
Inspector の Navigation 設定（Automatic や Explicit）は読まない。

この範囲を **スコープ** と呼ぶ。
ウィンドウのスコープは、既定でそのウィンドウの transform を根にする。
何も書かなくても、ウィンドウの中に閉じ込められる。

基底画面は `IFocusSource` なので、arinn からは根を決められない。
基底画面でもナビゲーションを任せたいなら、`INavigationScopeSource` を実装してスコープを宣言する。

```csharp
public sealed class BattleView : MonoBehaviour, IFocusSource, INavigationScopeSource
{
    public GameObject DefaultFocusElement => endTurnButton.gameObject;

    public NavigationScope CreateNavigationScope() => new(transform);
}
```

`CreateNavigationScope` は、その画面が最初に今の画面になったときに一度だけ呼ばれる。
Awake や Start で登録する必要はなく、コンテナの構築の順番も気にしなくてよい。

### 移動先の決め方

候補は、スコープの根の配下で、アクティブで、操作できる Selectable である。
Scrollbar はドラッグ用なので候補にしない。
候補の中から、進行方向に最も近く、直交する軸で揃っている要素を選ぶ。
直交する軸で重ならない候補は、中心のずれが進行方向の距離より小さいものだけを対象にする（段の違う端から斜めの別の段へ飛ばないため）。

選択がスコープの外にあるとき（背面の UI を選んだままウィンドウを開いた直後など）に方向を押すと、動かす代わりにスコープの既定要素へ戻す。

## 10. スコープで振る舞いを宣言する

端の挙動、候補の除外、スクロールの追従は、ウィンドウの `CreateNavigationScope` をオーバーライドして宣言する。

```csharp
public override NavigationScope CreateNavigationScope() => base.CreateNavigationScope()
    .OnEdge(NavigationDirection.Down, EdgePolicy.Exit(closeButton))
    .OnEdge(NavigationDirection.Left, EdgePolicy.WrapRow)
    .OnEdge(NavigationDirection.Right, EdgePolicy.WrapRow)
    .Exclude(selectable => _tabButtons.Contains(selectable))
    .WithScrollIntoView(scrollRect);
```

端（その方向に候補がない状態）の挙動は 3 種類ある。

- **`EdgePolicy.Stop`**：その場に留まる。宣言しなかった方向の既定。
- **`EdgePolicy.Exit(target)`**：指定した要素へ抜ける。抜け先がスコープの外、操作できない、非アクティブ、選択中の要素自身のどれかなら止まる。
- **`EdgePolicy.WrapRow`**：同じ行（上下の移動なら列）の反対側の端へ回り込む。

`Exclude` は端の挙動とは別の宣言で、候補そのものから外す。
LB と RB で切り替えるタブのように、十字キーでは選ばせたくない要素に使う。
ホバーでは外さないので、マウスでは選べる。

### 移動先を明示する

見た目の並びと操作の順番を意図的にずらしたい箇所（重なった手札の端、装飾で位置がずれたボタン、タブと中身の行き来など）は、`Link` で移動先を明示する。

```csharp
public override NavigationScope CreateNavigationScope() => base.CreateNavigationScope()
    .Link(tabButton, NavigationDirection.Down, firstItem)
    .Link(firstItem, NavigationDirection.Up, tabButton);
```

`Link` は、位置からの導出と端の宣言より優先する。
向きごとに 1 つずつ宣言するので、行き来させたいなら両方向を書く。
移動先が非アクティブや操作できないときは、明示した移動先で止めずに位置からの導出に戻る（売り切れの枠などで行き止まりにしないため）。
移動先がスコープの外なら、封じ込めを優先して無視する。
外すときは `Unlink` を呼ぶ。

### 外からスコープを結び付ける

画面のクラスを変えられない場合や、常時表示 UI（HUD）のように今の画面にならない UI には、外から `Register` でスコープを結び付ける。
`Register` したスコープは、画面が宣言したスコープより優先する。

```csharp
var registration = navigation.Register(hudView, new NavigationScope(hudRoot));
```

`Register` は画面ごとに 1 つのスコープを結び付け、登録し直すと置き換える。
画面が MonoBehaviour なら、破棄されたときに登録は自動で外れる。
MonoBehaviour でない画面は、返り値の `IDisposable` を Dispose するか、`Unregister` を呼んで外す。
登録し直した後に古い返り値を Dispose しても、新しいスコープは外れない。

### 移動を Unity に任せる

`CreateNavigationScope` で `new NavigationScope(transform, resolvesMove: false)` を返すと、そのウィンドウでは EventSystem の move を止めず、移動を Unity に任せる。
スクロールの追従とホバーの範囲は、スコープの宣言どおりに効く。
Explicit ナビゲーションを手で組んだ既存の画面を、段階的に移すときに使う。

## 11. 修飾ボタンと十字キーを両立させる

LB を押しながらの十字キーで別の操作（モードの切り替えなど）をするゲームでは、その間の十字キーを移動として扱いたくない。
`SetMoveBlocker` に条件を渡すと、成り立つ間は選択を動かさない。

```csharp
navigation.SetMoveBlocker(() => Gamepad.current?.leftShoulder.isPressed == true);
```

条件が成り立つ間は、スコープがなくても EventSystem の move を有効へ戻さない。
アプリ側で LB の押下中に move を止めている場合に、その停止を arinn が勝手に解かないためだ。
十字キーを押したまま LB を離すと、その時点で新しい押下として 1 回動く。

## 12. 選択の変化に反応する

選択の演出（グローや効果音）は見た目なので、arinn は持たない。
代わりに `SelectionChanged` で選択の変化を通知する。

```csharp
navigation.SelectionChanged
    .Where(change => change.ByInput)
    .Subscribe(_ => _se.Play("cursor"))
    .AddTo(this);
```

`Source` は、方向入力による移動（`Input`）、ポインタのホバー（`Hover`）、それ以外（`Program`）を区別する。
ウィンドウを開いたときに既定要素へ移るのは `Program` なので、上の例では開いただけで効果音は鳴らない。

## 13. スクロールとホバー

### 選択に合わせたスクロール

`WithScrollIntoView(scrollRect)` を宣言すると、選択した要素が ScrollRect の content の子孫なら、見える位置までスクロールする。
はみ出した側の端へ揃え、コンテンツの端がビューポートの内側へ入らないよう制限する。
ホバーで変わった選択にはスクロールを追従させない（ポインタを乗せただけで一覧が動くのを防ぐため）。

Selectable でない対象（仮想カーソルのマスなど）は、`ScrollIntoView.EnsureVisible(scrollRect, rect)` を直接呼ぶ。

### ホバーで選択する

`UseInputSystem()` は既定でホバー選択も有効にする。
ポインタが動いたときだけ、最前面に当たった Selectable を選択する。
ポインタが止まっている間は、パッドの操作をポインタの位置で上書きしない。

- 子の Graphic（ボタンのラベルなど）に当たっても、親の Selectable を選ぶ。
- 最前面に当たったものが Selectable でないか、操作できない Selectable なら、その下は選ばない（奥の要素へ貫通させない）。
- 今のスコープの外にある要素は選ばない。

特定のオブジェクトを貫通させたいときは、除外の条件を渡す。
条件に当てはまるオブジェクトは無視して、その下を見る。

```csharp
navigation.UseInputSystem(hoverIgnore: hit => hit.CompareTag("IgnoreHoverSelection"));
```

ホバーを使わないなら `UseInputSystem(hoverSelection: false)` にする。

## 14. 仮想カーソル

タイルのマス、扇状に重なった手札、栽培のスロットのように、Selectable で表せない対象の上をカーソルで動かしたいことがある。
これまでは、フォーカスだけを受ける空の Selectable を置き、Navigate を自前で購読して凌ぐことが多かった。
arinn はこれを **仮想カーソル** として扱う。

```csharp
public sealed class ShopView : WindowBase
{
    [SerializeField] private RectTransform slotArea;
    [SerializeField] private Button closeButton;

    private GridCursor _cursor;
    private CursorResolver _resolver;

    protected override void Awake()
    {
        base.Awake();
        SetCloseButton(closeButton);

        _cursor = new GridCursor(5, 3, cell => !IsSoldOut(cell))
            .OnEdge(NavigationDirection.Left, EdgePolicy.WrapRow)
            .OnEdge(NavigationDirection.Right, EdgePolicy.WrapRow)
            .OnEdge(NavigationDirection.Down, EdgePolicy.Exit(closeButton));
        _cursor.OnMoved.Subscribe(Highlight).AddTo(this);
        _cursor.OnFocusChanged.Subscribe(ShowCursor).AddTo(this);
        _cursor.OnSubmitted.Subscribe(_ => Buy(_cursor.Position)).AddTo(this);

        _resolver = new CursorResolver(_cursor, slotArea);
        SetDefaultFocusElement(_resolver.Anchor);
    }

    public override NavigationScope CreateNavigationScope() => base.CreateNavigationScope().UseResolver(_resolver);

    protected override void OnDestroy()
    {
        _cursor.Dispose();
        base.OnDestroy();
    }
}
```

`CursorResolver` は、`slotArea` の全面に見えない Selectable（**アンカー**）を置く。
EventSystem のフォーカスはアンカーが受ける。
アンカーが選択されている間の方向入力はカーソルへ渡り、決定はアンカーを経由して `OnSubmitted` に届く。
Cancel は通常の経路のままなので、`TryPopScope` でウィンドウを閉じられる。

アンカーは `slotArea` と同じ位置と大きさを持つ。
閉じるボタンからカーソルのある方向へ押せば、第 9 節の規則でアンカーが選ばれ、カーソルへ戻れる。
アンカーは Graphic を持たないので Raycast に当たらず、ホバーで選ばれることもない。

### GridCursor と ListCursor

- **`GridCursor`**：格子状のマス。位置は (列, 行) で、左上が (0, 0)、行は下へ増える。止まれないマス（`isNavigable` が false）は飛ばして、その先で止まれるマスへ進む。
- **`ListCursor`**：一列に並んだ項目。`isHorizontal` が true なら左右に並び、false なら上下に並ぶ。並びと直交する方向の入力は端として扱うので、手札の下に置いたボタンへ `Exit` で抜けられる。

要素の数が変わったら `SetSize` か `SetCount` を呼ぶ。
今の位置は範囲内へ収める。
位置をコードから決めるなら `SetPosition` か `SetIndex` を使う（止まれない位置かどうかは見ない）。

### 1 画面に複数のカーソルを置く

`CursorResolver` は、アンカー以外が選択されているときの解決を `fallback`（既定は座標で解決する `SpatialResolver`）に任せる。
`fallback` に別の `CursorResolver` を渡して重ねると、1 つのスコープに複数のカーソルを置ける。

```csharp
// Awake で作る（アンカーを既定要素にするため、スコープより先に要る）
_boardResolver = new CursorResolver(_boardCursor, boardArea);
_handResolver = new CursorResolver(_handCursor, handArea, fallback: _boardResolver);
SetDefaultFocusElement(_handResolver.Anchor);

public override NavigationScope CreateNavigationScope() => base.CreateNavigationScope().UseResolver(_handResolver);
```

## 15. 独自の入力と解決器

### 入力を差し替える

Input System 以外の入力を使うなら `INavigationInput` を実装して `SetInput` に渡す。

```csharp
public sealed class RewiredNavigationInput : INavigationInput
{
    public Vector2 ReadMove() => new(_player.GetAxis("UIHorizontal"), _player.GetAxis("UIVertical"));
    public float RepeatDelay => 0.4f;
    public float RepeatRate => 0.08f;
    public void SuppressUnityMove() { /* EventSystem 側の移動を止める */ }
    public void RestoreUnityMove() { /* 止めていれば戻す */ }
}
```

`SuppressUnityMove` は、スコープが移動を解決している間、毎フレーム呼ばれる。
他の処理が move を有効に戻しても、止め直すためだ。
`RestoreUnityMove` は、止めていなければ何もしない実装にする。
渡した入力が `IDisposable` なら、差し替えたときと `NavigationController` を破棄したときに Dispose される。

ホバーのポインタの位置は `IPointerPositionSource` で差し替える。

### 解決器を差し替える

移動先の決め方は、スコープごとに `INavigationResolver` で差し替えられる。
`Resolve` が返した要素がスコープの外か操作できなければ、`NavigationController` が移動を取り消すので、解決器の実装が誤っても封じ込めは破れない。

```csharp
public sealed class TabOrderResolver : INavigationResolver
{
    public Selectable Resolve(NavigationScope scope, Selectable current, NavigationDirection direction)
    {
        var candidates = scope.CollectCandidates(current);
        // 生成順で前後へ動く、など
        return null;
    }
}
```

`scope.CollectCandidates` は、除外や操作の可否を適用した候補を返す。
端の挙動を自前の解決器でも使うなら、`scope.GetEdge(direction)` と `scope.ResolveExit(edge, current)` を呼ぶ。

## 16. E2E テスト

arinn は、テストのために production の型へ入口を持たせない。
UI の状態は UI から観測し、操作は実入力の経路で行う。
その役割は [noema](https://github.com/void2610/noema) と [LiminalPalette](https://github.com/void2610/liminal-palette) が持つ。

### 十字キーで動かして選択を確かめる

noema の `Ui/Navigate` は、仮想ゲームパッドの十字キーを押して離す。
EventSystem へ Move イベントを直接送るのではなく入力デバイスへ流すので、arinn が move を止めて自前で解決する経路まで含めて通る。
押している時間はリピートが始まるより十分短いので、1 回の押下は 1 回の移動になる。

```
Ui/Navigate Right 2       → navigated: Right x2
Ui/Focused                → InventoryView/slots[2]
```

### arinn の状態を観測する

LiminalPalette が入っていれば、`Void2610.Arinn.LiminalPalette` が Editor と Development Build で有効になり、次のコマンドが使える。

| コマンド | 返り値 |
|---|---|
| `Arinn/TopWindow` | 最前面のウィンドウの GameObject 名。なければ `(none)` |
| `Arinn/WindowCount` | 開いているウィンドウの数 |
| `Arinn/IsFocusOnDefault` | フォーカスが最前面のウィンドウ（なければ基底画面）の既定要素にあるか |
| `Arinn/IsInPersistentUIMode` | 常時表示 UI にフォーカスを借りているか |
| `Arinn/Selected` | 選択中の GameObject 名 |
| `Arinn/ActiveScope` | 有効なスコープの根の GameObject 名 |
| `Arinn/IsSelectedInScope` | 選択中の要素が有効なスコープの中にあるか |

`Arinn/IsSelectedInScope` を各シナリオの後で確かめると、背面の UI へフォーカスが抜けていないことを継続的に検証できる。

## 17. つまずきやすいところ

- **`Open()` が `InvalidOperationException` を投げる**：`UIFocusManager` がまだ生成されていない。`RegisterArinn` を登録した LifetimeScope が先に構築されているか確かめる（Awake から `Open()` を呼んでいないかも確かめる）。
- **`CreateNavigationScope` の変更が効かない**：スコープは最初に今の画面になったときに一度だけ作る。開くたびに変えたい宣言（候補の除外など）は、条件の中で今の状態を読むように書く。
- **ナビゲーションが背面へ抜ける**：`InputSystemUIInputModule` を使っていないか、`SetInput` を呼んでいない可能性がある。入力がなければ、移動は Unity に任される。基底画面は、スコープを登録しない限り Unity の移動のままになる。
- **Inspector の Navigation 設定が効かない**：arinn が移動を解決しているスコープでは読まない。端の挙動は `OnEdge` で宣言する。Explicit のまま使いたい画面は `resolvesMove: false` で登録する。
- **シーンを切り替えたらウィンドウが開かなくなった**：アクティブなシーンが変わると、スタック、基底画面、`IInputScopeGate`、常時表示 UI の状態を捨てる。新しいシーンで `SwitchBase` と `SetInputScopeGate` を呼び直す。
- **ウィンドウを開いたのにフォーカスが移らない**：既定要素へのフォーカスは次のフレームで当たる。同じフレームで判定していないか確かめる。既定要素が非アクティブなら移らない。
- **閉じたウィンドウのボタンが押せてしまう**：`SetActive` や CanvasGroup を直接触って開閉していないか確かめる。開閉は `UIFocusManager` から行う。
