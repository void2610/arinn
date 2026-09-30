using NUnit.Framework;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn.Tests
{
    public sealed class NavigationControllerTests : ArinnTestFixture
    {
        private FakeNavigationInput _input;
        private NavigationController _navigation;

        [SetUp]
        public void SetUpNavigation()
        {
            _input = new FakeNavigationInput();
            _navigation = CreateNavigation(_input);
        }

        [Test]
        public void 右を押すと同じ行の右の要素へ移る()
        {
            var window = CreateWindow("Window");
            var left = CreateButton("Left", window.transform, new Vector2(0f, 0f));
            var right = CreateButton("Right", window.transform, new Vector2(200f, 0f));
            OpenWithScope(window, left, new NavigationScope(window.transform));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(right.gameObject));
        }

        [Test]
        public void スコープの外の要素へは移らない()
        {
            var window = CreateWindow("Window");
            var inside = CreateButton("Inside", window.transform, new Vector2(0f, 0f));
            CreateButton("Outside", CreateRoot("Background"), new Vector2(200f, 0f));
            OpenWithScope(window, inside, new NavigationScope(window.transform));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(inside.gameObject));
        }

        [Test]
        public void 選択がスコープの外にあれば移動の代わりに既定要素へ戻す()
        {
            var window = CreateWindow("Window");
            var inside = CreateButton("Inside", window.transform, new Vector2(0f, 0f));
            var outside = CreateButton("Outside", CreateRoot("Background"), new Vector2(-200f, 0f));
            OpenWithScope(window, inside, new NavigationScope(window.transform));
            EventSystem.SetSelectedGameObject(outside.gameObject);

            Press(Vector2.left);

            Assert.That(Selected, Is.EqualTo(inside.gameObject));
        }

        [Test]
        public void スコープを登録していないウィンドウも背面の要素へは移らない()
        {
            var window = CreateWindow("Window");
            var inside = CreateButton("Inside", window.transform, new Vector2(0f, 0f));
            CreateButton("Outside", CreateRoot("Background"), new Vector2(200f, 0f));
            window.Default = inside.gameObject;
            Manager.ShowWindow(window);
            NextNavigationFrame();

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(inside.gameObject));
            Assert.That(_input.IsSuppressed, Is.True, "Unity の Automatic ナビゲーションにも任せない");
        }

        [Test]
        public void スコープを登録していないウィンドウはウィンドウの範囲がホバーの対象になる()
        {
            var window = CreateWindow("Window");
            window.Default = CreateButton("Inside", window.transform, Vector2.zero).gameObject;
            Manager.ShowWindow(window);
            NextNavigationFrame();

            Assert.That(_navigation.ActiveScope?.Root, Is.EqualTo(window.transform));
        }

        [Test]
        public void 移動を解決しないスコープを登録したウィンドウはUnityの移動に任せる()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, Vector2.zero);
            OpenWithScope(window, item, new NavigationScope(window.transform, resolvesMove: false));

            Assert.That(_input.IsSuppressed, Is.False);
            Assert.That(_navigation.ActiveScope?.Root, Is.EqualTo(window.transform), "ホバーの範囲はウィンドウに絞ったまま");
        }

        [Test]
        public void ウィンドウが宣言したスコープに従う()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, Vector2.zero);
            var close = CreateButton("Close", window.transform, new Vector2(600f, 400f));
            window.ScopeFactory = w => new NavigationScope(w.transform).OnEdge(NavigationDirection.Down, EdgePolicy.Exit(close));
            window.Default = item.gameObject;
            window.Open();
            NextNavigationFrame();

            Press(Vector2.down);

            Assert.That(Selected, Is.EqualTo(close.gameObject));
        }

        [Test]
        public void ウィンドウのスコープは最初に最前面になったときに一度だけ作る()
        {
            var window = CreateWindow("Window");
            window.Default = CreateButton("Item", window.transform, Vector2.zero).gameObject;
            Assert.That(window.ScopeCreatedCount, Is.Zero, "開くまでは作らない");

            window.Open();
            NextNavigationFrame();
            NextNavigationFrame();
            window.Close();
            window.Open();
            NextNavigationFrame();

            Assert.That(window.ScopeCreatedCount, Is.EqualTo(1));
        }

        [Test]
        public void Registerしたスコープはウィンドウの宣言より優先する()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, Vector2.zero);
            var registered = new NavigationScope(window.transform);
            _navigation.Register(window, registered);
            window.Default = item.gameObject;
            window.Open();
            NextNavigationFrame();

            Assert.That(_navigation.ActiveScope, Is.EqualTo(registered));
            Assert.That(window.ScopeCreatedCount, Is.Zero);
        }

        [Test]
        public void 基底画面がスコープを宣言すれば登録しなくても効く()
        {
            var root = CreateRoot("Base");
            var left = CreateButton("Left", root, new Vector2(0f, 0f));
            var right = CreateButton("Right", root, new Vector2(200f, 0f));
            var baseScreen = new TestScopedFocusSource { Default = left.gameObject, ScopeFactory = () => new NavigationScope(root) };
            Manager.SwitchBase(baseScreen);
            NextNavigationFrame();

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(right.gameObject));
        }

        [Test]
        public void 宣言したスコープがnullならUnityの移動に任せる()
        {
            var root = CreateRoot("Base");
            var left = CreateButton("Left", root, Vector2.zero);
            Manager.SwitchBase(new TestScopedFocusSource { Default = left.gameObject });
            NextNavigationFrame();

            Assert.That(_navigation.ActiveScope, Is.Null);
            Assert.That(_input.IsSuppressed, Is.False);
        }

        [Test]
        public void Linkで明示した移動先は位置からの導出より優先する()
        {
            var window = CreateWindow("Window");
            var from = CreateButton("From", window.transform, new Vector2(0f, 0f));
            CreateButton("Near", window.transform, new Vector2(200f, 0f));
            var linked = CreateButton("Linked", window.transform, new Vector2(0f, -300f));
            OpenWithScope(window, from, new NavigationScope(window.transform).Link(from, NavigationDirection.Right, linked));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(linked.gameObject));
        }

        [Test]
        public void Linkの移動先が操作できなければ位置からの導出に戻る()
        {
            var window = CreateWindow("Window");
            var from = CreateButton("From", window.transform, new Vector2(0f, 0f));
            var near = CreateButton("Near", window.transform, new Vector2(200f, 0f));
            var linked = CreateButton("Linked", window.transform, new Vector2(0f, -300f));
            linked.interactable = false;
            OpenWithScope(window, from, new NavigationScope(window.transform).Link(from, NavigationDirection.Right, linked));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(near.gameObject));
        }

        [Test]
        public void Linkの移動先がスコープの外なら無視する()
        {
            var window = CreateWindow("Window");
            var from = CreateButton("From", window.transform, new Vector2(0f, 0f));
            var near = CreateButton("Near", window.transform, new Vector2(200f, 0f));
            var outside = CreateButton("Outside", CreateRoot("Background"), new Vector2(0f, -300f));
            OpenWithScope(window, from, new NavigationScope(window.transform).Link(from, NavigationDirection.Right, outside));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(near.gameObject));
        }

        [Test]
        public void Unlinkで位置からの導出に戻る()
        {
            var window = CreateWindow("Window");
            var from = CreateButton("From", window.transform, new Vector2(0f, 0f));
            var near = CreateButton("Near", window.transform, new Vector2(200f, 0f));
            var linked = CreateButton("Linked", window.transform, new Vector2(0f, -300f));
            var scope = new NavigationScope(window.transform).Link(from, NavigationDirection.Right, linked);
            OpenWithScope(window, from, scope);

            scope.Unlink(from, NavigationDirection.Right);
            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(near.gameObject));
        }

        [Test]
        public void Linkは端の宣言より優先する()
        {
            var window = CreateWindow("Window");
            var from = CreateButton("From", window.transform, new Vector2(0f, 0f));
            var linked = CreateButton("Linked", window.transform, new Vector2(0f, 300f));
            var exit = CreateButton("Exit", window.transform, new Vector2(0f, -300f));
            var scope = new NavigationScope(window.transform)
                .OnEdge(NavigationDirection.Right, EdgePolicy.Exit(exit))
                .Link(from, NavigationDirection.Right, linked);
            OpenWithScope(window, from, scope);

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(linked.gameObject));
        }

        [Test]
        public void IncludeNonInteractableなら操作できない要素へも移る()
        {
            var window = CreateWindow("Window");
            var first = CreateButton("First", window.transform, new Vector2(0f, 0f));
            var disabled = CreateButton("Disabled", window.transform, new Vector2(200f, 0f));
            disabled.interactable = false;
            OpenWithScope(window, first, new NavigationScope(window.transform).IncludeNonInteractable());

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(disabled.gameObject));
        }

        [Test]
        public void IncludeNonInteractableでも親のCanvasGroupで止められた要素へは移らない()
        {
            var window = CreateWindow("Window");
            var first = CreateButton("First", window.transform, new Vector2(0f, 0f));
            var panel = CreateRootFor("Panel", window.transform);
            panel.gameObject.AddComponent<CanvasGroup>().interactable = false;
            CreateButton("Blocked", panel, new Vector2(200f, 0f));
            OpenWithScope(window, first, new NavigationScope(window.transform).IncludeNonInteractable());

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(first.gameObject));
        }

        [Test]
        public void WithoutRepeatなら押しっぱなしでも1歩だけ動く()
        {
            var window = CreateWindow("Window");
            var first = CreateButton("First", window.transform, new Vector2(0f, 0f));
            var second = CreateButton("Second", window.transform, new Vector2(200f, 0f));
            CreateButton("Third", window.transform, new Vector2(400f, 0f));
            OpenWithScope(window, first, new NavigationScope(window.transform).WithoutRepeat());

            _input.Move = Vector2.right;
            NextNavigationFrame();
            NextNavigationFrame(1f);
            NextNavigationFrame(1f);

            Assert.That(Selected, Is.EqualTo(second.gameObject));
        }

        [Test]
        public void HorizontalOnlyなら垂直の成分が大きい入力でも左右に動く()
        {
            var window = CreateWindow("Window");
            var first = CreateButton("First", window.transform, new Vector2(0f, 0f));
            var right = CreateButton("Right", window.transform, new Vector2(200f, 0f));
            CreateButton("Up", window.transform, new Vector2(0f, 300f));
            OpenWithScope(window, first, new NavigationScope(window.transform).WithInputMode(NavigationInputMode.HorizontalOnly));

            Press(new Vector2(0.6f, 0.8f));

            Assert.That(Selected, Is.EqualTo(right.gameObject));
        }

        [Test]
        public void EightWayなら仮想カーソルが斜めに1歩動く()
        {
            var window = CreateWindow("Window");
            var area = CreateArea(window.transform);
            using var cursor = new GridCursor(3, 3);
            using var resolver = new CursorResolver(cursor, area);
            OpenWithScope(window, resolver.Anchor, new NavigationScope(window.transform).UseResolver(resolver).WithInputMode(NavigationInputMode.EightWay));

            Press(new Vector2(0.7f, -0.7f));

            Assert.That(cursor.Position, Is.EqualTo(new Vector2Int(1, 1)));
        }

        [Test]
        public void PassMoveToElementの軸は移動せず要素のOnMoveへ渡す()
        {
            var window = CreateWindow("Window");
            var sliderRect = CreateRootFor("Slider", window.transform);
            sliderRect.sizeDelta = new Vector2(200f, 40f);
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            CreateButton("Right", window.transform, new Vector2(300f, 0f));
            var below = CreateButton("Below", window.transform, new Vector2(0f, -200f));
            var scope = new NavigationScope(window.transform).PassMoveToElement(selectable => selectable is Slider, NavigationAxis.Horizontal);
            OpenWithScope(window, slider, scope);

            Press(Vector2.right);
            Assert.That(Selected, Is.EqualTo(slider.gameObject), "左右では要素から動かない");
            Assert.That(slider.value, Is.GreaterThan(0f), "左右は Slider の値の変更になる");

            Press(Vector2.down);
            Assert.That(Selected, Is.EqualTo(below.gameObject), "渡さない軸は移動する");
        }

        [Test]
        public void ClearLinksで明示した移動先をすべて外す()
        {
            var window = CreateWindow("Window");
            var from = CreateButton("From", window.transform, new Vector2(0f, 0f));
            var near = CreateButton("Near", window.transform, new Vector2(200f, 0f));
            var linked = CreateButton("Linked", window.transform, new Vector2(0f, -300f));
            var scope = new NavigationScope(window.transform).Link(from, NavigationDirection.Right, linked);
            OpenWithScope(window, from, scope);

            scope.ClearLinks();
            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(near.gameObject));
        }

        [Test]
        public void Blockした方向へは隣があっても動かない()
        {
            var window = CreateWindow("Window");
            var from = CreateButton("From", window.transform, new Vector2(0f, 0f));
            CreateButton("Near", window.transform, new Vector2(200f, 0f));
            OpenWithScope(window, from, new NavigationScope(window.transform).Block(from, NavigationDirection.Right));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(from.gameObject));
        }

        [Test]
        public void IncludeScrollbarsならScrollbarへも移る()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, Vector2.zero);
            var bar = CreateRootFor("Scrollbar", window.transform);
            bar.anchoredPosition = new Vector2(200f, 0f);
            bar.sizeDelta = new Vector2(20f, 200f);
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            OpenWithScope(window, item, new NavigationScope(window.transform).IncludeScrollbars());

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(scrollbar.gameObject));
        }

        [Test]
        public void EightWayでも選択が変わったら2歩目は動かさない()
        {
            var window = CreateWindow("Window");
            var first = CreateButton("First", window.transform, new Vector2(0f, 0f));
            var right = CreateButton("Right", window.transform, new Vector2(200f, 0f));
            CreateButton("Below", window.transform, new Vector2(200f, -200f));
            OpenWithScope(window, first, new NavigationScope(window.transform).WithInputMode(NavigationInputMode.EightWay));

            Press(new Vector2(0.7f, -0.7f));

            Assert.That(Selected, Is.EqualTo(right.gameObject));
        }

        [Test]
        public void 操作できない要素にはPassMoveToElementでもOnMoveを送らない()
        {
            var window = CreateWindow("Window");
            var sliderRect = CreateRootFor("Slider", window.transform);
            sliderRect.sizeDelta = new Vector2(200f, 40f);
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.interactable = false;
            var scope = new NavigationScope(window.transform)
                .IncludeNonInteractable()
                .PassMoveToElement(selectable => selectable is Slider, NavigationAxis.Horizontal);
            OpenWithScope(window, slider, scope);

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(slider.gameObject));
            Assert.That(slider.value, Is.Zero);
        }

        [Test]
        public void WithRepeatでリピートを戻せる()
        {
            var scope = new NavigationScope(CreateRoot("Root")).WithoutRepeat();

            scope.WithRepeat();

            Assert.That(scope.Repeats, Is.True);
        }

        [Test]
        public void ReturnsToAnchorを指定するとアンカー以外が選択されていてもカーソルを動かしてアンカーへ戻す()
        {
            var window = CreateWindow("Window");
            var area = CreateArea(window.transform);
            var hud = CreateButton("Hud", window.transform, new Vector2(0f, -400f));
            using var cursor = new ListCursor(3);
            using var resolver = new CursorResolver(cursor, area, returnsToAnchor: true);
            OpenWithScope(window, resolver.Anchor, new NavigationScope(window.transform).UseResolver(resolver));
            EventSystem.SetSelectedGameObject(hud.gameObject);

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(resolver.Anchor.gameObject));
            Assert.That(cursor.Index, Is.EqualTo(1));
        }

        [Test]
        public void 常時表示UIのスコープが入れ子ならいちばん内側を使う()
        {
            var baseRoot = CreateRoot("Base");
            var baseButton = CreateButton("BaseButton", baseRoot, Vector2.zero);
            var baseScreen = new TestFocusSource { Default = baseButton.gameObject };
            _navigation.Register(baseScreen, new NavigationScope(baseRoot));
            Manager.SwitchBase(baseScreen);

            var hudRoot = CreateRootFor("Hud", baseRoot);
            var hudButton = CreateButton("HudButton", hudRoot, new Vector2(0f, 300f));
            _navigation.Register(new TestFocusSource { Default = hudButton.gameObject }, new NavigationScope(hudRoot));
            Manager.EnterPersistentUIFocus(hudButton.gameObject, hudRoot.gameObject);
            NextNavigationFrame();

            Assert.That(_navigation.ActiveScope?.Root, Is.EqualTo((Transform)hudRoot));
        }

        [Test]
        public void 操作できない要素は飛ばす()
        {
            var window = CreateWindow("Window");
            var first = CreateButton("First", window.transform, new Vector2(0f, 0f));
            var disabled = CreateButton("Disabled", window.transform, new Vector2(200f, 0f));
            var last = CreateButton("Last", window.transform, new Vector2(400f, 0f));
            disabled.interactable = false;
            OpenWithScope(window, first, new NavigationScope(window.transform));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(last.gameObject));
        }

        [Test]
        public void 端でExitを宣言した方向は宣言した要素へ抜ける()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, new Vector2(0f, 0f));
            var close = CreateButton("Close", window.transform, new Vector2(600f, 400f));
            var scope = new NavigationScope(window.transform).OnEdge(NavigationDirection.Down, EdgePolicy.Exit(close));
            OpenWithScope(window, item, scope);

            Press(Vector2.down);

            Assert.That(Selected, Is.EqualTo(close.gameObject));
        }

        [Test]
        public void 除外した要素へは移らない()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, new Vector2(0f, 0f));
            var tab = CreateButton("Tab", window.transform, new Vector2(200f, 0f));
            var scope = new NavigationScope(window.transform).Exclude(selectable => selectable == tab);
            OpenWithScope(window, item, scope);

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(item.gameObject));
        }

        [Test]
        public void 移動を止める条件が成り立つ間は動かさない()
        {
            var window = CreateWindow("Window");
            var left = CreateButton("Left", window.transform, new Vector2(0f, 0f));
            CreateButton("Right", window.transform, new Vector2(200f, 0f));
            OpenWithScope(window, left, new NavigationScope(window.transform));
            _navigation.SetMoveBlocker(() => true);

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(left.gameObject));
        }

        [Test]
        public void スコープが無くなったらEventSystemの移動を戻す()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, Vector2.zero);
            OpenWithScope(window, item, new NavigationScope(window.transform));
            Assert.That(_input.IsSuppressed, Is.True);

            Manager.HideWindow(window);
            NextNavigationFrame();

            Assert.That(_input.IsSuppressed, Is.False);
        }

        [Test]
        public void 移動を止める条件が成り立つ間はスコープが無くてもEventSystemの移動を戻さない()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, Vector2.zero);
            OpenWithScope(window, item, new NavigationScope(window.transform));
            _navigation.SetMoveBlocker(() => true);

            Manager.HideWindow(window);
            NextNavigationFrame();

            Assert.That(_input.IsSuppressed, Is.True);
        }

        [Test]
        public void 移動を止める条件が成り立つ間はスコープが無い画面でもEventSystemの移動を止める()
        {
            var blocked = true;
            _navigation.SetMoveBlocker(() => blocked);

            NextNavigationFrame();
            Assert.That(_input.IsSuppressed, Is.True, "スコープが無くても、止める条件の間は Unity の移動を止める");

            blocked = false;
            NextNavigationFrame();
            Assert.That(_input.IsSuppressed, Is.False, "条件が外れたら、スコープが無い画面では Unity の移動に戻す");
        }

        [Test]
        public void 基底画面のスコープはウィンドウが無いときに効く()
        {
            var root = CreateRoot("Base");
            var left = CreateButton("Left", root, new Vector2(0f, 0f));
            var right = CreateButton("Right", root, new Vector2(200f, 0f));
            var baseScreen = new TestFocusSource { Default = left.gameObject };
            _navigation.Register(baseScreen, new NavigationScope(root));
            Manager.SwitchBase(baseScreen);
            NextNavigationFrame();

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(right.gameObject));
        }

        [Test]
        public void 選択の変化を入力とそれ以外で区別して通知する()
        {
            var window = CreateWindow("Window");
            var left = CreateButton("Left", window.transform, new Vector2(0f, 0f));
            CreateButton("Right", window.transform, new Vector2(200f, 0f));
            NavigationSelectionChange? last = null;
            using var subscription = _navigation.SelectionChanged.Subscribe(change => last = change);

            OpenWithScope(window, left, new NavigationScope(window.transform));
            Assert.That(last?.Source, Is.EqualTo(SelectionChangeSource.Program));

            Press(Vector2.right);
            Assert.That(last?.Source, Is.EqualTo(SelectionChangeSource.Input));
        }

        [Test]
        public void 登録を破棄するとスコープが外れる()
        {
            var window = CreateWindow("Window");
            var registration = _navigation.Register(window, new NavigationScope(window.transform));

            registration.Dispose();

            Assert.That(_navigation.GetScope(window), Is.Null);
        }

        [Test]
        public void 仮想カーソルはアンカーが選択されている間の方向入力で動く()
        {
            var window = CreateWindow("Window");
            var area = CreateArea(window.transform);
            using var cursor = new ListCursor(3);
            using var resolver = new CursorResolver(cursor, area);
            OpenWithScope(window, resolver.Anchor, new NavigationScope(window.transform).UseResolver(resolver));
            Assert.That(cursor.IsFocused, Is.True);

            Press(Vector2.right);

            Assert.That(cursor.Index, Is.EqualTo(1));
            Assert.That(Selected, Is.EqualTo(resolver.Anchor.gameObject));
        }

        [Test]
        public void 仮想カーソルの端でExitを宣言した方向はカーソルを抜ける()
        {
            var window = CreateWindow("Window");
            var area = CreateArea(window.transform);
            var close = CreateButton("Close", window.transform, new Vector2(0f, -400f));
            using var cursor = new ListCursor(3).OnEdge(NavigationDirection.Down, EdgePolicy.Exit(close));
            using var resolver = new CursorResolver(cursor, area);
            OpenWithScope(window, resolver.Anchor, new NavigationScope(window.transform).UseResolver(resolver));

            Press(Vector2.down);

            Assert.That(Selected, Is.EqualTo(close.gameObject));
            Assert.That(cursor.IsFocused, Is.False);
        }

        [Test]
        public void 周りのボタンからは位置に向かって仮想カーソルへ入れる()
        {
            var window = CreateWindow("Window");
            var area = CreateArea(window.transform);
            var below = CreateButton("Below", window.transform, new Vector2(0f, -400f));
            using var cursor = new ListCursor(3);
            using var resolver = new CursorResolver(cursor, area);
            OpenWithScope(window, below, new NavigationScope(window.transform).UseResolver(resolver));

            Press(Vector2.up);

            Assert.That(Selected, Is.EqualTo(resolver.Anchor.gameObject));
        }

        [Test]
        public void Exitの抜け先がスコープの外なら止まる()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, Vector2.zero);
            var outside = CreateButton("Outside", CreateRoot("Background"), new Vector2(0f, -300f));
            OpenWithScope(window, item, new NavigationScope(window.transform).OnEdge(NavigationDirection.Down, EdgePolicy.Exit(outside)));

            Press(Vector2.down);

            Assert.That(Selected, Is.EqualTo(item.gameObject));
        }

        [Test]
        public void Exitの抜け先が操作できなければ止まる()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, Vector2.zero);
            var close = CreateButton("Close", window.transform, new Vector2(600f, 400f));
            close.interactable = false;
            OpenWithScope(window, item, new NavigationScope(window.transform).OnEdge(NavigationDirection.Down, EdgePolicy.Exit(close)));

            Press(Vector2.down);

            Assert.That(Selected, Is.EqualTo(item.gameObject));
        }

        [Test]
        public void Exitの抜け先が非アクティブなら止まる()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, Vector2.zero);
            var close = CreateButton("Close", window.transform, new Vector2(600f, 400f));
            close.gameObject.SetActive(false);
            OpenWithScope(window, item, new NavigationScope(window.transform).OnEdge(NavigationDirection.Down, EdgePolicy.Exit(close)));

            Press(Vector2.down);

            Assert.That(Selected, Is.EqualTo(item.gameObject));
        }

        [Test]
        public void ResolveExitは自分自身を指定したらnullを返す()
        {
            var root = CreateRoot("Root");
            var item = CreateButton("Item", root, Vector2.zero);
            var scope = new NavigationScope(root);

            Assert.That(scope.ResolveExit(EdgePolicy.Exit(item), item), Is.Null);
        }

        [Test]
        public void 解決器がスコープの外の要素を返しても移らない()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, Vector2.zero);
            var outside = CreateButton("Outside", CreateRoot("Background"), new Vector2(200f, 0f));
            OpenWithScope(window, item, new NavigationScope(window.transform).UseResolver(new FixedResolver(outside)));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(item.gameObject));
        }

        [Test]
        public void 解決器が操作できない要素を返しても移らない()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, Vector2.zero);
            var disabled = CreateButton("Disabled", window.transform, new Vector2(200f, 0f));
            disabled.interactable = false;
            OpenWithScope(window, item, new NavigationScope(window.transform).UseResolver(new FixedResolver(disabled)));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(item.gameObject));
        }

        [Test]
        public void 常時表示UIを借りている間は選択を含むスコープで動く()
        {
            var baseRoot = CreateRoot("Base");
            var baseButton = CreateButton("BaseButton", baseRoot, Vector2.zero);
            CreateButton("BaseRight", baseRoot, new Vector2(200f, 0f));
            var baseScreen = new TestFocusSource { Default = baseButton.gameObject };
            _navigation.Register(baseScreen, new NavigationScope(baseRoot));
            Manager.SwitchBase(baseScreen);

            var hudRoot = CreateRoot("Hud");
            hudRoot.anchoredPosition = new Vector2(0f, 500f);
            var hudLeft = CreateButton("HudLeft", hudRoot, Vector2.zero);
            var hudRight = CreateButton("HudRight", hudRoot, new Vector2(200f, 0f));
            _navigation.Register(new TestFocusSource { Default = hudLeft.gameObject }, new NavigationScope(hudRoot));
            Manager.EnterPersistentUIFocus(hudLeft.gameObject, hudRoot.gameObject);
            NextNavigationFrame();
            Assert.That(Selected, Is.EqualTo(hudLeft.gameObject));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(hudRight.gameObject));
            Assert.That(_navigation.ActiveScope?.Root, Is.EqualTo((Transform)hudRoot));
        }

        [Test]
        public void 常時表示UIを借りている間に選択がどのスコープにも無ければUnityの移動に任せる()
        {
            var baseRoot = CreateRoot("Base");
            var baseButton = CreateButton("BaseButton", baseRoot, Vector2.zero);
            var baseScreen = new TestFocusSource { Default = baseButton.gameObject };
            _navigation.Register(baseScreen, new NavigationScope(baseRoot));
            Manager.SwitchBase(baseScreen);
            NextNavigationFrame();
            Assert.That(_input.IsSuppressed, Is.True);

            var hud = CreateButton("Hud", CreateRoot("Hud"), Vector2.zero);
            Manager.EnterPersistentUIFocus(hud.gameObject);
            NextNavigationFrame();
            NextNavigationFrame();

            Assert.That(_navigation.ActiveScope, Is.Null);
            Assert.That(_input.IsSuppressed, Is.False);
        }

        [Test]
        public void WrapRowを宣言した方向は同じ行の反対側の端へ回り込む()
        {
            var window = CreateWindow("Window");
            var first = CreateButton("First", window.transform, new Vector2(0f, 0f));
            CreateButton("Middle", window.transform, new Vector2(200f, 0f));
            var last = CreateButton("Last", window.transform, new Vector2(400f, 0f));
            CreateButton("OtherRow", window.transform, new Vector2(-200f, 300f));
            OpenWithScope(window, last, new NavigationScope(window.transform).OnEdge(NavigationDirection.Right, EdgePolicy.WrapRow));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(first.gameObject));
        }

        [Test]
        public void 宣言しない方向の端では止まる()
        {
            var window = CreateWindow("Window");
            CreateButton("First", window.transform, new Vector2(0f, 0f));
            var last = CreateButton("Last", window.transform, new Vector2(200f, 0f));
            OpenWithScope(window, last, new NavigationScope(window.transform));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(last.gameObject));
        }

        [Test]
        public void 方向入力で選んだ要素にスクロールが追従する()
        {
            var window = CreateWindow("Window");
            var (scrollRect, content, visible, hidden) = ScrollFixtures.CreateList(this, window.transform);
            OpenWithScope(window, visible, new NavigationScope(window.transform).WithScrollIntoView(scrollRect));

            Press(Vector2.down);

            Assert.That(Selected, Is.EqualTo(hidden.gameObject));
            Assert.That(content.anchoredPosition.y, Is.EqualTo(575f).Within(0.01f));
        }

        [Test]
        public void Scrollbarは移動先の候補にしない()
        {
            var window = CreateWindow("Window");
            var item = CreateButton("Item", window.transform, Vector2.zero);
            var bar = CreateRootFor("Scrollbar", window.transform);
            bar.anchoredPosition = new Vector2(200f, 0f);
            bar.sizeDelta = new Vector2(20f, 200f);
            bar.gameObject.AddComponent<Scrollbar>();
            OpenWithScope(window, item, new NavigationScope(window.transform));

            Press(Vector2.right);

            Assert.That(Selected, Is.EqualTo(item.gameObject));
        }

        [Test]
        public void 破棄された画面の登録は自動で外れる()
        {
            var window = CreateWindow("Window");
            _navigation.Register(window, new NavigationScope(window.transform));

            Object.DestroyImmediate(window);
            NextNavigationFrame();

            Assert.That(_navigation.GetScope(window), Is.Null);
        }

        [Test]
        public void 登録し直したあとに古い登録を破棄しても新しいスコープは外れない()
        {
            var window = CreateWindow("Window");
            var oldRegistration = _navigation.Register(window, new NavigationScope(window.transform));
            var newScope = new NavigationScope(window.transform);
            _navigation.Register(window, newScope);

            oldRegistration.Dispose();

            Assert.That(_navigation.GetScope(window), Is.EqualTo(newScope));
        }

        [Test]
        public void Unregisterで登録が外れる()
        {
            var window = CreateWindow("Window");
            _navigation.Register(window, new NavigationScope(window.transform));

            _navigation.Unregister(window);

            Assert.That(_navigation.GetScope(window), Is.Null);
        }

        [Test]
        public void Disposeで移動を戻して入力を破棄する()
        {
            var input = new DisposableNavigationInput();
            _navigation.SetInput(input);
            var window = CreateWindow("Window");
            OpenWithScope(window, CreateButton("Item", window.transform, Vector2.zero), new NavigationScope(window.transform));
            Assert.That(input.IsSuppressed, Is.True);

            _navigation.Dispose();

            Assert.That(input.IsSuppressed, Is.False);
            Assert.That(input.IsDisposed, Is.True);
            Assert.That(NavigationController.Instance, Is.Null);
        }

        [Test]
        public void 入力を差し替えると前の入力の移動を戻して破棄する()
        {
            var input = new DisposableNavigationInput();
            _navigation.SetInput(input);
            var window = CreateWindow("Window");
            OpenWithScope(window, CreateButton("Item", window.transform, Vector2.zero), new NavigationScope(window.transform));

            _navigation.SetInput(new FakeNavigationInput());

            Assert.That(input.IsSuppressed, Is.False);
            Assert.That(input.IsDisposed, Is.True);
        }

        [Test]
        public void 押し続けると遅延のあと間隔ごとに動く()
        {
            var window = CreateWindow("Window");
            var first = CreateButton("First", window.transform, new Vector2(0f, 0f));
            CreateButton("Second", window.transform, new Vector2(200f, 0f));
            var third = CreateButton("Third", window.transform, new Vector2(400f, 0f));
            OpenWithScope(window, first, new NavigationScope(window.transform));

            _input.Move = Vector2.right;
            NextNavigationFrame();
            NextNavigationFrame(0.3f);
            Assert.That(Selected, Is.Not.EqualTo(third.gameObject), "遅延の間はリピートしない");

            NextNavigationFrame(0.3f);

            Assert.That(Selected, Is.EqualTo(third.gameObject));
        }

        private void OpenWithScope(TestWindow window, Selectable defaultElement, NavigationScope scope)
        {
            window.Default = defaultElement.gameObject;
            _navigation.Register(window, scope);
            Manager.ShowWindow(window);
            NextNavigationFrame();
        }

        /// <summary>
        /// 押して離す。押したフレームで 1 回だけ移動する。
        /// </summary>
        private void Press(Vector2 move)
        {
            _input.Move = move;
            NextNavigationFrame();
            _input.Move = Vector2.zero;
            NextNavigationFrame();
        }

        private RectTransform CreateArea(Transform parent)
        {
            var area = CreateRoot("Area");
            area.SetParent(parent, false);
            area.sizeDelta = new Vector2(300f, 300f);
            return area;
        }

        /// <summary>
        /// 方向に関係なく決まった要素を返す解決器（封じ込めの防御を通すため）。
        /// </summary>
        private sealed class FixedResolver : INavigationResolver
        {
            private readonly Selectable _target;

            public FixedResolver(Selectable target) => _target = target;

            public Selectable Resolve(NavigationScope scope, Selectable current, NavigationDirection direction) => _target;
        }
    }
}
