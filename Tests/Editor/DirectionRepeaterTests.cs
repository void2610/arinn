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
            Assert.That(DirectionRepeater.Quantize(new Vector2(0.4f, -0.9f)), Is.EqualTo(NavigationDirection.Down));
            Assert.That(DirectionRepeater.Quantize(new Vector2(-0.7f, 0.7f)), Is.EqualTo(NavigationDirection.Left));
        }

        [Test]
        public void Advance_押した瞬間に1回出て遅延のあとは間隔ごとに出る()
        {
            var repeater = new DirectionRepeater();

            Assert.That(repeater.Advance(Vector2.right, 0f, DELAY, RATE), Is.EqualTo(NavigationDirection.Right));
            Assert.That(repeater.Advance(Vector2.right, 0.4f, DELAY, RATE), Is.Null);
            Assert.That(repeater.Advance(Vector2.right, 0.5f, DELAY, RATE), Is.EqualTo(NavigationDirection.Right));
            Assert.That(repeater.Advance(Vector2.right, 0.55f, DELAY, RATE), Is.Null);
            Assert.That(repeater.Advance(Vector2.right, 0.65f, DELAY, RATE), Is.EqualTo(NavigationDirection.Right));
        }

        [Test]
        public void Advance_方向を変えたら新しい押下として扱う()
        {
            var repeater = new DirectionRepeater();
            repeater.Advance(Vector2.right, 0f, DELAY, RATE);

            Assert.That(repeater.Advance(Vector2.down, 0.1f, DELAY, RATE), Is.EqualTo(NavigationDirection.Down));
        }

        [Test]
        public void Reset_押したままでも次は新しい押下になる()
        {
            var repeater = new DirectionRepeater();
            repeater.Advance(Vector2.right, 0f, DELAY, RATE);

            repeater.Reset();

            Assert.That(repeater.Advance(Vector2.right, 0.1f, DELAY, RATE), Is.EqualTo(NavigationDirection.Right));
        }
    }
}
