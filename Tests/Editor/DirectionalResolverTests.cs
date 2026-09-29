using NUnit.Framework;
using UnityEngine;

namespace Void2610.Arinn.Tests
{
    public sealed class DirectionalResolverTests
    {
        [Test]
        public void FindNext_同じ行の隣を選ぶ()
        {
            var candidates = new[] { At(200f, 0f), At(400f, 0f) };

            Assert.That(DirectionalResolver.FindNext(At(0f, 0f), candidates, NavigationDirection.Right), Is.EqualTo(0));
        }

        [Test]
        public void FindNext_反対側の候補は選ばない()
        {
            var candidates = new[] { At(-200f, 0f) };

            Assert.That(DirectionalResolver.FindNext(At(0f, 0f), candidates, NavigationDirection.Right), Is.EqualTo(-1));
        }

        [Test]
        public void FindNext_整列した候補を斜めの近い候補より優先する()
        {
            var candidates = new[] { At(150f, 100f), At(300f, 0f) };

            Assert.That(DirectionalResolver.FindNext(At(0f, 0f), candidates, NavigationDirection.Right), Is.EqualTo(1));
        }

        [Test]
        public void FindNext_直交軸で重ならず大きくずれた候補へは飛ばない()
        {
            // 下の段の、右へ少しだけずれた候補。右移動で別の段へ斜めに飛ばない
            var candidates = new[] { At(100f, -300f) };

            Assert.That(DirectionalResolver.FindNext(At(0f, 0f), candidates, NavigationDirection.Right), Is.EqualTo(-1));
        }

        [Test]
        public void FindNext_上はYが大きい側()
        {
            var candidates = new[] { At(0f, -100f), At(0f, 100f) };

            Assert.That(DirectionalResolver.FindNext(At(0f, 0f), candidates, NavigationDirection.Up), Is.EqualTo(1));
        }

        [Test]
        public void FindWrapTarget_同じ行の反対側で一番遠い候補を選ぶ()
        {
            var candidates = new[] { At(-200f, 0f), At(-400f, 0f), At(-400f, 200f) };

            Assert.That(DirectionalResolver.FindWrapTarget(At(0f, 0f), candidates, NavigationDirection.Right), Is.EqualTo(1));
        }

        private static Rect At(float x, float y)
        {
            return new(x - 50f, y - 25f, 100f, 50f);
        }
    }
}
