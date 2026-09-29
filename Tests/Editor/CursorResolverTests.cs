using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Void2610.Arinn.Tests
{
    public sealed class CursorResolverTests : ArinnTestFixture
    {
        private ListCursor _cursor;
        private CursorResolver _resolver;

        [SetUp]
        public void SetUpCursor()
        {
            var area = CreateRoot("Area");
            area.sizeDelta = new Vector2(300f, 300f);
            _cursor = new ListCursor(3);
            _resolver = new CursorResolver(_cursor, area);
        }

        [TearDown]
        public void TearDownCursor()
        {
            _resolver.Dispose();
            _cursor.Dispose();
        }

        [Test]
        public void アンカーが決定を受けるとOnSubmittedが発火する()
        {
            var submitted = 0;
            using var subscription = _cursor.OnSubmitted.Subscribe(_ => submitted++);

            ExecuteEvents.Execute(_resolver.Anchor.gameObject, new BaseEventData(EventSystem), ExecuteEvents.submitHandler);

            Assert.That(submitted, Is.EqualTo(1));
        }

        [Test]
        public void アンカーが操作できなければ決定を無視する()
        {
            var submitted = 0;
            using var subscription = _cursor.OnSubmitted.Subscribe(_ => submitted++);
            _resolver.Anchor.interactable = false;

            ExecuteEvents.Execute(_resolver.Anchor.gameObject, new BaseEventData(EventSystem), ExecuteEvents.submitHandler);

            Assert.That(submitted, Is.Zero);
        }

        [Test]
        public void アンカーの選択と選択解除でフォーカスを通知する()
        {
            var changes = new List<bool>();
            using var subscription = _cursor.OnFocusChanged.Subscribe(changes.Add);

            EventSystem.SetSelectedGameObject(_resolver.Anchor.gameObject);
            Assert.That(_cursor.IsFocused, Is.True);

            EventSystem.SetSelectedGameObject(Create("Other"));
            Assert.That(_cursor.IsFocused, Is.False);
            Assert.That(changes, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void アンカーは範囲の全面に置かれ見た目を持たない()
        {
            var rect = (RectTransform)_resolver.Anchor.transform;

            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(_resolver.Anchor.targetGraphic, Is.Null);
            Assert.That(_resolver.Anchor.GetComponent<UnityEngine.UI.Graphic>(), Is.Null, "Raycast に当たらない");
        }

        [Test]
        public void Disposeでアンカーを破棄する()
        {
            var anchor = _resolver.Anchor.gameObject;

            _resolver.Dispose();

            Assert.That(anchor == null, Is.True);
        }
    }
}
