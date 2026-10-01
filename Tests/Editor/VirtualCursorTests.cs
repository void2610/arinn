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
        public void GridCursor_skipsBlockedがfalseなら止まれないマスの手前で止まる()
        {
            using var cursor = new GridCursor(4, 1, p => p.x != 1, skipsBlocked: false);

            Assert.That(cursor.TryMove(NavigationDirection.Right), Is.False);
            Assert.That(cursor.Position, Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void GridCursor_斜めは範囲外へ動かない()
        {
            using var cursor = new GridCursor(3, 3);

            Assert.That(cursor.TryMoveDiagonal(NavigationDirection.Left, NavigationDirection.Down), Is.False);
            Assert.That(cursor.Position, Is.EqualTo(Vector2Int.zero));
            Assert.That(cursor.TryMoveDiagonal(NavigationDirection.Right, NavigationDirection.Down), Is.True);
            Assert.That(cursor.Position, Is.EqualTo(new Vector2Int(1, 1)));
        }

        [Test]
        public void ListCursor_skipsBlockedがfalseなら回り込み先が止まれないとき動かない()
        {
            using var cursor = new ListCursor(3, isNavigable: i => i != 0, skipsBlocked: false).OnEdge(NavigationDirection.Right, EdgePolicy.WrapRow);
            cursor.SetIndex(2);

            Assert.That(cursor.TryMove(NavigationDirection.Right), Is.False);
            Assert.That(cursor.Index, Is.EqualTo(2));
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
        public void GridCursor_SetPositionは範囲の外を範囲内へ収めて通知する()
        {
            using var cursor = new GridCursor(3, 2);
            var moved = new Vector2Int(-1, -1);
            using var subscription = cursor.OnMoved.Subscribe(position => moved = position);

            cursor.SetPosition(new Vector2Int(10, -5));

            Assert.That(cursor.Position, Is.EqualTo(new Vector2Int(2, 0)));
            Assert.That(moved, Is.EqualTo(new Vector2Int(2, 0)));
        }

        [Test]
        public void GridCursor_同じ位置へのSetPositionは通知しない()
        {
            using var cursor = new GridCursor(3, 2);
            var count = 0;
            using var subscription = cursor.OnMoved.Subscribe(_ => count++);

            cursor.SetPosition(Vector2Int.zero);

            Assert.That(count, Is.Zero);
        }

        [Test]
        public void ListCursor_SetCountで範囲の外になった位置は収める()
        {
            using var cursor = new ListCursor(5);
            cursor.SetIndex(4);

            cursor.SetCount(2);

            Assert.That(cursor.Index, Is.EqualTo(1));
        }

        [Test]
        public void ListCursor_SetCountで0にすると位置はマイナス1になる()
        {
            using var cursor = new ListCursor(3);

            cursor.SetCount(0);

            Assert.That(cursor.Index, Is.EqualTo(-1));
        }

        [Test]
        public void ListCursor_SetIndexは範囲の外を範囲内へ収める()
        {
            using var cursor = new ListCursor(3);

            cursor.SetIndex(-4);
            Assert.That(cursor.Index, Is.Zero);

            cursor.SetIndex(9);
            Assert.That(cursor.Index, Is.EqualTo(2));
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
