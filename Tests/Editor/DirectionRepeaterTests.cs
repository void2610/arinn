using NUnit.Framework;
using UnityEngine;

namespace Void2610.Arinn.Tests
{
    public sealed class DirectionRepeaterTests
    {
        private const float DELAY = 0.5f;
        private const float RATE = 0.1f;

        [Test]
        public void Quantize_デッドゾーンの内側は方向にしない() => Assert.That(DirectionRepeater.Quantize(new Vector2(0.3f, 0.3f)), Is.Null);

        [Test]
        public void Quantize_斜めは大きい軸を採り同じなら水平を優先する()
        {
            Assert.That(DirectionRepeater.Quantize(new Vector2(0.4f, -0.9f))?.Primary, Is.EqualTo(NavigationDirection.Down));
            Assert.That(DirectionRepeater.Quantize(new Vector2(-0.7f, 0.7f))?.Primary, Is.EqualTo(NavigationDirection.Left));
        }

        [Test]
        public void Quantize_FourWayPreferVerticalは同じなら垂直を優先する()
        {
            var step = DirectionRepeater.Quantize(new Vector2(0.7f, 0.7f), NavigationInputMode.FourWayPreferVertical);

            Assert.That(step, Is.EqualTo(new NavigationStep(NavigationDirection.Up)));
        }

        [Test]
        public void Quantize_HorizontalOnlyは垂直の成分が大きくても左右として扱う()
        {
            Assert.That(DirectionRepeater.Quantize(new Vector2(0.6f, 0.8f), NavigationInputMode.HorizontalOnly), Is.EqualTo(new NavigationStep(NavigationDirection.Right)));
            Assert.That(DirectionRepeater.Quantize(new Vector2(0.2f, 0.9f), NavigationInputMode.HorizontalOnly), Is.Null);
        }

        [Test]
        public void Quantize_VerticalOnlyは垂直の成分だけを見る()
        {
            Assert.That(DirectionRepeater.Quantize(new Vector2(0.9f, -0.6f), NavigationInputMode.VerticalOnly), Is.EqualTo(new NavigationStep(NavigationDirection.Down)));
            Assert.That(DirectionRepeater.Quantize(new Vector2(0.9f, 0.2f), NavigationInputMode.VerticalOnly), Is.Null);
        }

        [Test]
        public void Quantize_EightWayは両方の軸があれば水平と垂直の2方向にする()
        {
            Assert.That(DirectionRepeater.Quantize(new Vector2(0.7f, -0.7f), NavigationInputMode.EightWay), Is.EqualTo(new NavigationStep(NavigationDirection.Right, NavigationDirection.Down)));
            Assert.That(DirectionRepeater.Quantize(new Vector2(0f, 0.9f), NavigationInputMode.EightWay), Is.EqualTo(new NavigationStep(NavigationDirection.Up)));
        }

        [Test]
        public void Advance_押した瞬間に1回出て遅延のあとは間隔ごとに出る()
        {
            var repeater = new DirectionRepeater();

            Assert.That(repeater.Advance(Vector2.right, 0f, DELAY, RATE)?.Primary, Is.EqualTo(NavigationDirection.Right));
            Assert.That(repeater.Advance(Vector2.right, 0.4f, DELAY, RATE), Is.Null);
            Assert.That(repeater.Advance(Vector2.right, 0.5f, DELAY, RATE)?.Primary, Is.EqualTo(NavigationDirection.Right));
            Assert.That(repeater.Advance(Vector2.right, 0.55f, DELAY, RATE), Is.Null);
            Assert.That(repeater.Advance(Vector2.right, 0.65f, DELAY, RATE)?.Primary, Is.EqualTo(NavigationDirection.Right));
        }

        [Test]
        public void Advance_リピートしない指定なら押しっぱなしでは出ない()
        {
            var repeater = new DirectionRepeater();

            Assert.That(repeater.Advance(Vector2.right, 0f, DELAY, RATE, repeats: false)?.Primary, Is.EqualTo(NavigationDirection.Right));
            Assert.That(repeater.Advance(Vector2.right, 0.6f, DELAY, RATE, repeats: false), Is.Null);
            Assert.That(repeater.Advance(Vector2.right, 2f, DELAY, RATE, repeats: false), Is.Null);
            repeater.Advance(Vector2.zero, 2.1f, DELAY, RATE, repeats: false);
            Assert.That(repeater.Advance(Vector2.right, 2.2f, DELAY, RATE, repeats: false)?.Primary, Is.EqualTo(NavigationDirection.Right), "押し直せば出る");
        }

        [Test]
        public void Advance_方向を変えたら新しい押下として扱う()
        {
            var repeater = new DirectionRepeater();
            repeater.Advance(Vector2.right, 0f, DELAY, RATE);

            Assert.That(repeater.Advance(Vector2.down, 0.1f, DELAY, RATE)?.Primary, Is.EqualTo(NavigationDirection.Down));
        }

        [Test]
        public void Reset_押したままでも次は新しい押下になる()
        {
            var repeater = new DirectionRepeater();
            repeater.Advance(Vector2.right, 0f, DELAY, RATE);

            repeater.Reset();

            Assert.That(repeater.Advance(Vector2.right, 0.1f, DELAY, RATE)?.Primary, Is.EqualTo(NavigationDirection.Right));
        }
    }
}
