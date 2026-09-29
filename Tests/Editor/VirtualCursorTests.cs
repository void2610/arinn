using NUnit.Framework;
using R3;
using UnityEngine;

namespace Void2610.Arinn.Tests
{
    public sealed class VirtualCursorTests
    {
        [Test]
        public void GridCursor_下は行が増える側()
        {
            using var cursor = new GridCursor(3, 3);

            Assert.That(cursor.TryMove(NavigationDirection.Down), Is.True);
            Assert.That(cursor.Position, Is.EqualTo(new Vector2Int(0, 1)));
        }

        [Test]
        public void GridCursor_止まれないマスは飛ばす()
        {
            using var cursor = new GridCursor(4, 1, p => p.x != 1);

            cursor.TryMove(NavigationDirection.Right);

            Assert.That(cursor.Position, Is.EqualTo(new Vector2Int(2, 0)));
        }

        [Test]
        public void GridCursor_端では動かずfalseを返す()
        {
            using var cursor = new GridCursor(2, 2);

            Assert.That(cursor.TryMove(NavigationDirection.Left), Is.False);
            Assert.That(cursor.Position, Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void GridCursor_WrapRowを宣言した方向は反対側の端へ回り込む()
        {
            using var cursor = new GridCursor(4, 2).OnEdge(NavigationDirection.Left, EdgePolicy.WrapRow);
            cursor.SetPosition(new Vector2Int(0, 1));

            Assert.That(cursor.TryMove(NavigationDirection.Left), Is.True);
            Assert.That(cursor.Position, Is.EqualTo(new Vector2Int(3, 1)));
        }

        [Test]
        public void GridCursor_SetSizeで範囲の外になった位置は収める()
        {
            using var cursor = new GridCursor(5, 5);
            cursor.SetPosition(new Vector2Int(4, 4));

            cursor.SetSize(2, 3);

            Assert.That(cursor.Position, Is.EqualTo(new Vector2Int(1, 2)));
        }

        [Test]
        public void ListCursor_並びと直交する方向は端として扱う()
        {
            using var cursor = new ListCursor(3);

            Assert.That(cursor.TryMove(NavigationDirection.Up), Is.False);
            Assert.That(cursor.TryMove(NavigationDirection.Right), Is.True);
            Assert.That(cursor.Index, Is.EqualTo(1));
        }

        [Test]
        public void ListCursor_WrapRowで末尾から先頭へ回り込む()
        {
            using var cursor = new ListCursor(3, isHorizontal: false).OnEdge(NavigationDirection.Down, EdgePolicy.WrapRow);
            cursor.SetIndex(2);

            Assert.That(cursor.TryMove(NavigationDirection.Down), Is.True);
            Assert.That(cursor.Index, Is.EqualTo(0));
        }

        [Test]
        public void ListCursor_項目が無ければ位置はマイナス1で動かない()
        {
            using var cursor = new ListCursor(0);

            Assert.That(cursor.Index, Is.EqualTo(-1));
            Assert.That(cursor.TryMove(NavigationDirection.Right), Is.False);
        }

        [Test]
        public void ListCursor_移動を通知する()
        {
            using var cursor = new ListCursor(3);
            var moved = -1;
            using var subscription = cursor.OnMoved.Subscribe(index => moved = index);

            cursor.TryMove(NavigationDirection.Right);

            Assert.That(moved, Is.EqualTo(1));
        }
    }
}
