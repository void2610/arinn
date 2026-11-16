using System;
using R3;
using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// 行ごとに列の数が違う格子の上を動く仮想カーソル（行ごとに選べる語の数が違う文章の一覧など）。
    /// 位置は (列, 行) で、左上が (0, 0)。上下で別の行へ移ると、列はその行の右端までに詰める。
    /// 列の無い行は止まれない行として、<see cref="VirtualCursor.SkipsBlocked"/> に従って飛ばすか手前で止まる。
    /// </summary>
    public sealed class JaggedGridCursor : VirtualCursor
    {
        /// <summary>
        /// 位置が変わったときに発火する（方向入力でも <see cref="SetPosition"/> でも）。
        /// </summary>
        public Observable<Vector2Int> OnMoved => _onMoved;

        /// <summary>
        /// 行の数。
        /// </summary>
        public int Rows { get; private set; }

        /// <summary>
        /// 今の位置。止まれる行が無ければ (-1, -1)。
        /// </summary>
        public Vector2Int Position { get; private set; } = new(-1, -1);

        private readonly Subject<Vector2Int> _onMoved = new();
        private readonly Func<int, int> _columnsOf;

        /// <param name="rows">行の数</param>
        /// <param name="columnsOf">行の番号からその行の列の数を返す。移動のたびに評価される</param>
        /// <param name="skipsBlocked">列の無い行を飛び越えてその先へ進むなら true、手前で止まるなら false</param>
        public JaggedGridCursor(int rows, Func<int, int> columnsOf, bool skipsBlocked = true)
            : base(skipsBlocked)
        {
            _columnsOf = columnsOf ?? throw new ArgumentNullException(nameof(columnsOf));
            SetRows(rows);
        }

        /// <summary>
        /// 指定方向の端での挙動を宣言する。宣言しない方向は <see cref="EdgePolicy.Stop"/>。
        /// </summary>
        public JaggedGridCursor OnEdge(NavigationDirection direction, EdgePolicy policy)
        {
            Edges.Set(direction, policy);
            return this;
        }

        /// <summary>
        /// 行の数を変える。今の位置は範囲内へ収め、収まる行が無ければ先頭の止まれる行へ移す。
        /// </summary>
        public void SetRows(int rows)
        {
            Rows = Mathf.Max(0, rows);
            SetPosition(Position.y < 0 ? new Vector2Int(0, FirstStoppableRow()) : Position);
        }

        /// <summary>
        /// 位置をコードから決める。行と列は範囲内へ収める（止まれない行かどうかは見ない）。
        /// </summary>
        public void SetPosition(Vector2Int position)
        {
            var row = Rows > 0 ? Mathf.Clamp(position.y, 0, Rows - 1) : -1;
            var columns = row >= 0 ? _columnsOf(row) : 0;
            var next = columns > 0 ? new Vector2Int(Mathf.Clamp(position.x, 0, columns - 1), row) : new Vector2Int(-1, -1);
            if (next == Position) return;
            Position = next;
            _onMoved.OnNext(next);
        }

        public override bool TryMove(NavigationDirection direction)
        {
            if (Position.y < 0) return false;

            var horizontal = direction is NavigationDirection.Left or NavigationDirection.Right;
            var step = direction is NavigationDirection.Right or NavigationDirection.Down ? 1 : -1;
            int next;
            if (horizontal)
            {
                var columns = _columnsOf(Position.y);
                next = FindAlongLine(Position.x, step, columns, _ => true);
                if (next < 0 && GetEdge(direction).Kind == EdgePolicyKind.WrapRow) next = FindWrap(Position.x, step, columns, _ => true);
                if (next < 0) return false;
                Position = new Vector2Int(next, Position.y);
            }
            else
            {
                next = FindAlongLine(Position.y, step, Rows, IsStoppableRow);
                if (next < 0 && GetEdge(direction).Kind == EdgePolicyKind.WrapRow) next = FindWrap(Position.y, step, Rows, IsStoppableRow);
                if (next < 0) return false;
                Position = new Vector2Int(Mathf.Min(Position.x, _columnsOf(next) - 1), next);
            }
            _onMoved.OnNext(Position);
            return true;
        }

        private bool IsStoppableRow(int row) => _columnsOf(row) > 0;

        private int FirstStoppableRow()
        {
            for (var row = 0; row < Rows; row++)
            {
                if (IsStoppableRow(row)) return row;
            }
            return -1;
        }

        public override void Dispose()
        {
            base.Dispose();
            _onMoved.Dispose();
        }
    }
}
