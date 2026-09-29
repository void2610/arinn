using NUnit.Framework;
using UnityEngine;

namespace Void2610.Arinn.Tests
{
    public sealed class ScrollIntoViewTests
    {
        [Test]
        public void CalculateAxisDelta_見えている要素は動かさない()
        {
            Assert.That(ScrollIntoView.CalculateAxisDelta(-50f, 50f, -100f, 100f, -500f, 100f), Is.Zero);
        }

        [Test]
        public void CalculateAxisDelta_下へはみ出した要素は下端へ揃える()
        {
            Assert.That(ScrollIntoView.CalculateAxisDelta(-180f, -120f, -100f, 100f, -500f, 100f), Is.EqualTo(80f));
        }

        [Test]
        public void CalculateAxisDelta_コンテンツの端をビューポートの内側へ入れない()
        {
            // 揃えるには 150 動かす必要があるが、コンテンツの下端（-220）がビューポートの下端に来るところで止める
            Assert.That(ScrollIntoView.CalculateAxisDelta(-250f, -200f, -100f, 100f, -220f, 100f), Is.EqualTo(120f));
        }

        [Test]
        public void CalculateAxisDelta_ビューポートに収まるコンテンツはスクロールしない()
        {
            Assert.That(ScrollIntoView.CalculateAxisDelta(-250f, -200f, -100f, 100f, -90f, 90f), Is.Zero);
        }
    }

    public sealed class ScrollIntoViewEnsureVisibleTests : ArinnTestFixture
    {
        [Test]
        public void EnsureVisible_隠れた要素がビューポートの下端に来るまでコンテンツを動かす()
        {
            var (scrollRect, content, _, hidden) = ScrollFixtures.CreateList(this, CreateRoot("Root"));

            ScrollIntoView.EnsureVisible(scrollRect, (RectTransform)hidden.transform);

            Assert.That(content.anchoredPosition.y, Is.EqualTo(575f).Within(0.01f));
        }

        [Test]
        public void EnsureVisible_見えている要素では動かさない()
        {
            var (scrollRect, content, visible, _) = ScrollFixtures.CreateList(this, CreateRoot("Root"));

            ScrollIntoView.EnsureVisible(scrollRect, (RectTransform)visible.transform);

            Assert.That(content.anchoredPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void EnsureVisible_コンテンツの子孫でなければ動かさない()
        {
            var (scrollRect, content, _, _) = ScrollFixtures.CreateList(this, CreateRoot("Root"));
            var outside = CreateButton("Outside", CreateRoot("Other"), new Vector2(0f, -1000f));

            ScrollIntoView.EnsureVisible(scrollRect, (RectTransform)outside.transform);

            Assert.That(content.anchoredPosition, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void EnsureVisible_スクロールしない軸は動かさない()
        {
            var (scrollRect, content, _, hidden) = ScrollFixtures.CreateList(this, CreateRoot("Root"));
            scrollRect.vertical = false;

            ScrollIntoView.EnsureVisible(scrollRect, (RectTransform)hidden.transform);

            Assert.That(content.anchoredPosition, Is.EqualTo(Vector2.zero));
        }
    }
}
