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
    }
}
