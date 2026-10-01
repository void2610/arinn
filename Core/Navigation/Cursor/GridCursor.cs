using System;
using R3;
using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// 格子状のマスの上を動く仮想カーソル。位置は (列, 行) で、列は左が 0、行は上が 0。
    /// 止まれないマス（isNavigable が false）は飛ばして、その先で止まれるマスへ進む。
    /// </summary>
    public sealed class GridCursor : VirtualCursor, IDiagonalCursor
    {
        /// <summary>
        /// 位置が変わったときに発火する（方向入力でも <see cref="SetPosition"/> でも）。
        /// </summary>
        public Observable<Vector2Int> OnMoved => _onMoved;

        public int Columns { get; private set; }

        public int Rows { get; private set; }

        /// <summary>
        /// 今の位置（x が列、y が行）。
        /// </summary>
        public Vector2Int Position { get; private set; }

        private readonly Subject<Vector2Int> _onMoved = new();
        private readonly Func<Vector2Int, bool> _isNavigable;

        /// <param name="columns">列の数</param>
        /// <param name="rows">行の数</param>
        /// <param name="isNavigable">止まれるマスか。null ならすべてのマスに止まれる。移動のたびに評価される</param>
        /// <param name="skipsBlocked">止まれないマスを飛び越えてその先へ進むなら true、手前で止まるなら false</param>
        public GridCursor(int columns, int rows, Func<Vector2Int, bool> isNavigable = null, bool skipsBlocked = true)
            : base(skipsBlocked)
        {
            Columns = Mathf.Max(0, columns);
            Rows = Mathf.Max(0, rows);
            _isNavigable = isNavigable;
        }

        /// <summary>
        /// 指定方向の端での挙動を宣言する。宣言しない方向は <see cref="EdgePolicy.Stop"/>。
        /// </summary>
        public GridCursor OnEdge(NavigationDirection direction, EdgePolicy policy)
        {
            Edges.Set(direction, policy);
            return this;
        }

        /// <summary>
        /// マスの数を変える。今の位置は範囲内へ収める。
        /// </summary>
        public void SetSize(int columns, int rows)
        {
            Columns = Mathf.Max(0, columns);
            Rows = Mathf.Max(0, rows);
            SetPosition(Position);
        }

        /// <summary>
        /// 位置をコードから決める。範囲外は範囲内へ収める。止まれないマスかどうかは見ない。
        /// </summary>
        public void SetPosition(Vector2Int position)
        {
            var clamped = new Vector2Int(
                Mathf.Clamp(position.x, 0, Mathf.Max(0, Columns - 1)),
                Mathf.Clamp(position.y, 0, Mathf.Max(0, Rows - 1)));
            if (clamped == Position) return;
            Position = clamped;
            _onMoved.OnNext(clamped);
        }

        public override bool TryMove(NavigationDirection direction)
        {
            if (Columns == 0 || Rows == 0) return false;

            var horizontal = direction is NavigationDirection.Left or NavigationDirection.Right;
            var step = direction is NavigationDirection.Right or NavigationDirection.Down ? 1 : -1;
            var current = horizontal ? Position.x : Position.y;
            var count = horizontal ? Columns : Rows;
            bool CanStop(int i) => IsNavigable(horizontal ? new Vector2Int(i, Position.y) : new Vector2Int(Position.x, i));

            var next = FindAlongLine(current, step, count, CanStop);
            if (next < 0 && GetEdge(direction).Kind == EdgePolicyKind.WrapRow) next = FindWrap(current, step, count, CanStop);
            if (next < 0) return false;

            Position = horizontal ? new Vector2Int(next, Position.y) : new Vector2Int(Position.x, next);
            _onMoved.OnNext(Position);
            return true;
        }

        public bool TryMoveDiagonal(NavigationDirection horizontal, NavigationDirection vertical)
        {
            var target = Position + new Vector2Int(horizontal == NavigationDirection.Right ? 1 : -1, vertical == NavigationDirection.Down ? 1 : -1);
            if (target.x < 0 || target.x >= Columns || target.y < 0 || target.y >= Rows || !IsNavigable(target)) return false;

            Position = target;
            _onMoved.OnNext(Position);
            return true;
        }

        private bool IsNavigable(Vector2Int position)
        {
            return _isNavigable == null || _isNavigable(position);
        }

        public override void Dispose()
        {
            base.Dispose();
            _onMoved.Dispose();
        }
    }
}
