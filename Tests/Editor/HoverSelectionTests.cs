using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Void2610.Arinn.Tests
{
    public sealed class HoverSelectionTests : ArinnTestFixture
    {
        private FakePointer _pointer;
        private NavigationController _navigation;
        private readonly List<GameObject> _hits = new();

        [SetUp]
        public void SetUpHover()
        {
            _hits.Clear();
            _pointer = new FakePointer();
            _navigation = CreateNavigation(new FakeNavigationInput());
            _navigation.Raycaster = new FixedRaycaster(_hits);
            _navigation.EnableHoverSelection(_pointer);
        }

        [Test]
        public void ポインタが動いたときだけ選ぶ()
        {
            var button = CreateButton("Button", CreateRoot("Root"), Vector2.zero);
            _hits.Add(button.gameObject);

            NextNavigationFrame();
            NextNavigationFrame();
            Assert.That(Selected, Is.Null, "最初の位置と止まっている間は選ばない");

            MovePointer();

            Assert.That(Selected, Is.EqualTo(button.gameObject));
        }

        [Test]
        public void スコープの外の要素は選ばない()
        {
            var window = CreateWindow("Window");
            var inside = CreateButton("Inside", window.transform, Vector2.zero);
            var outside = CreateButton("Outside", CreateRoot("Background"), new Vector2(300f, 0f));
            window.Default = inside.gameObject;
            Manager.ShowWindow(window);
            NextNavigationFrame();
            _hits.Add(outside.gameObject);

            MovePointer();

            Assert.That(Selected, Is.EqualTo(inside.gameObject));
        }

        [Test]
        public void 除外したオブジェクトは貫通して下の要素を選ぶ()
        {
            var overlay = Create("Overlay");
            var button = CreateButton("Button", CreateRoot("Root"), Vector2.zero);
            _navigation.EnableHoverSelection(_pointer, hit => hit == overlay);
            _hits.Add(overlay);
            _hits.Add(button.gameObject);

            MovePointer();

            Assert.That(Selected, Is.EqualTo(button.gameObject));
        }

        [Test]
        public void 選択できないものに当たったら下の要素は選ばない()
        {
            var overlay = Create("Overlay");
            var button = CreateButton("Button", CreateRoot("Root"), Vector2.zero);
            _hits.Add(overlay);
            _hits.Add(button.gameObject);

            MovePointer();

            Assert.That(Selected, Is.Null);
        }

        [Test]
        public void 操作できない要素に当たったら下の要素へ貫通しない()
        {
            var root = CreateRoot("Root");
            var disabled = CreateButton("Disabled", root, Vector2.zero);
            disabled.interactable = false;
            var below = CreateButton("Below", root, Vector2.zero);
            _hits.Add(disabled.gameObject);
            _hits.Add(below.gameObject);

            MovePointer();

            Assert.That(Selected, Is.Null);
        }

        [Test]
        public void 子に当たっても親のSelectableを選ぶ()
        {
            var button = CreateButton("Button", CreateRoot("Root"), Vector2.zero);
            var label = Create("Label");
            label.transform.SetParent(button.transform, false);
            _hits.Add(label);

            MovePointer();

            Assert.That(Selected, Is.EqualTo(button.gameObject));
        }

        [Test]
        public void ホバーで変わった選択はSourceがHoverになる()
        {
            var button = CreateButton("Button", CreateRoot("Root"), Vector2.zero);
            _hits.Add(button.gameObject);
            NavigationSelectionChange? last = null;
            using var subscription = _navigation.SelectionChanged.Subscribe(change => last = change);

            MovePointer();

            Assert.That(last?.Source, Is.EqualTo(SelectionChangeSource.Hover));
            Assert.That(last?.Target, Is.EqualTo(button.gameObject));
        }

        [Test]
        public void ホバーで変わった選択にはスクロールを追従させない()
        {
            var window = CreateWindow("Window");
            var (scrollRect, content, visible, hidden) = ScrollFixtures.CreateList(this, window.transform);
            window.Default = visible.gameObject;
            _navigation.Register(window, new NavigationScope(window.transform).WithScrollIntoView(scrollRect));
            Manager.ShowWindow(window);
            NextNavigationFrame();
            var before = content.anchoredPosition;
            _hits.Add(hidden.gameObject);

            MovePointer();

            Assert.That(Selected, Is.EqualTo(hidden.gameObject));
            Assert.That(content.anchoredPosition, Is.EqualTo(before));
        }

        [Test]
        public void ポインタが無ければ選ばない()
        {
            var button = CreateButton("Button", CreateRoot("Root"), Vector2.zero);
            _hits.Add(button.gameObject);
            _pointer.Position = null;

            NextNavigationFrame();
            NextNavigationFrame();

            Assert.That(Selected, Is.Null);
        }

        [Test]
        public void DisableHoverSelectionのあとは選ばない()
        {
            var button = CreateButton("Button", CreateRoot("Root"), Vector2.zero);
            _hits.Add(button.gameObject);
            _navigation.DisableHoverSelection();

            MovePointer();

            Assert.That(Selected, Is.Null);
        }

        // 最初の位置を覚えさせてから動かす
        private void MovePointer()
        {
            NextNavigationFrame();
            _pointer.Position = (_pointer.Position ?? Vector2.zero) + Vector2.one;
            NextNavigationFrame();
        }
    }

    /// <summary>
    /// 決めたオブジェクトに手前から順に当たったことにする当たり判定。
    /// </summary>
    internal sealed class FixedRaycaster : IUIRaycaster
    {
        private readonly List<GameObject> _hits;

        public FixedRaycaster(List<GameObject> hits) => _hits = hits;

        public void RaycastAll(EventSystem eventSystem, PointerEventData pointerData, List<RaycastResult> results)
        {
            foreach (var hit in _hits) results.Add(new RaycastResult { gameObject = hit });
        }
    }

    /// <summary>
    /// 見えている項目と、ビューポートの下に隠れた項目を持つ縦のスクロール一覧。
    /// </summary>
    internal static class ScrollFixtures
    {
        public const float VIEWPORT_HEIGHT = 200f;
        public const float CONTENT_HEIGHT = 1000f;

        /// <summary>
        /// ビューポートは親の中央に 200×200、コンテンツは上端を揃えた高さ 1000。
        /// visible はビューポートの上のほう、hidden はビューポートの下端より 550 下にある。
        /// </summary>
        public static (ScrollRect ScrollRect, RectTransform Content, Button Visible, Button Hidden) CreateList(ArinnTestFixture fixture, Transform parent)
        {
            var viewport = fixture.CreateRootFor("Scroll", parent);
            viewport.sizeDelta = new Vector2(200f, VIEWPORT_HEIGHT);
            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();

            var content = fixture.CreateRootFor("Content", viewport);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(200f, CONTENT_HEIGHT);
            content.anchoredPosition = Vector2.zero;

            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // コンテンツの中央はビューポートの中心から 400 下。子の位置はコンテンツの中央から測る
            var visible = fixture.CreateButtonFor("Visible", content, new Vector2(0f, 450f));
            var hidden = fixture.CreateButtonFor("Hidden", content, new Vector2(0f, -250f));
            return (scrollRect, content, visible, hidden);
        }
    }
}
