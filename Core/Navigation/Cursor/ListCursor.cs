using System;
using R3;
using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// 一列に並んだ項目の上を動く仮想カーソル（手札の左右選択など）。
    /// 並びと直交する方向の入力は、そのまま端として <see cref="VirtualCursor.GetEdge"/> の挙動に従う。
    /// </summary>
    public sealed class ListCursor : VirtualCursor
    {
        /// <summary>
        /// 位置が変わったときに発火する（方向入力でも <see cref="SetIndex"/> でも）。
        /// </summary>
        public Observable<int> OnMoved => _onMoved;

        public int Count { get; private set; }

        /// <summary>
        /// 今の位置。項目が無ければ -1。
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// true なら左右に並ぶ（右が次）、false なら上下に並ぶ（下が次）。
        /// </summary>
        public bool IsHorizontal { get; }

        private readonly Subject<int> _onMoved = new();
        private readonly Func<int, bool> _isNavigable;

        /// <param name="count">項目の数</param>
        /// <param name="isHorizontal">左右に並ぶなら true、上下に並ぶなら false</param>
        /// <param name="isNavigable">止まれる項目か。null ならすべての項目に止まれる。移動のたびに評価される</param>
        /// <param name="skipsBlocked">止まれない項目を飛び越えてその先へ進むなら true、手前で止まるなら false</param>
        public ListCursor(int count, bool isHorizontal = true, Func<int, bool> isNavigable = null, bool skipsBlocked = true)
            : base(skipsBlocked)
        {
            Count = Mathf.Max(0, count);
            Index = Count > 0 ? 0 : -1;
            IsHorizontal = isHorizontal;
            _isNavigable = isNavigable;
        }

        /// <summary>
        /// 指定方向の端での挙動を宣言する。宣言しない方向は <see cref="EdgePolicy.Stop"/>。
        /// </summary>
        public ListCursor OnEdge(NavigationDirection direction, EdgePolicy policy)
        {
            Edges.Set(direction, policy);
            return this;
        }

        /// <summary>
        /// 項目の数を変える。今の位置は範囲内へ収める。
        /// </summary>
        public void SetCount(int count)
        {
            Count = Mathf.Max(0, count);
            SetIndex(Index);
        }

        /// <summary>
        /// 位置をコードから決める。範囲外は範囲内へ収める。止まれない項目かどうかは見ない。
        /// </summary>
        public void SetIndex(int index)
        {
            var clamped = Count > 0 ? Mathf.Clamp(index, 0, Count - 1) : -1;
            if (clamped == Index) return;
            Index = clamped;
            _onMoved.OnNext(clamped);
        }

        public override bool TryMove(NavigationDirection direction)
        {
            if (Count == 0) return false;

            var step = direction switch
            {
                NavigationDirection.Right when IsHorizontal => 1,
                NavigationDirection.Left when IsHorizontal => -1,
                NavigationDirection.Down when !IsHorizontal => 1,
                NavigationDirection.Up when !IsHorizontal => -1,
                _ => 0,
            };
            if (step == 0) return false;

            var next = FindAlongLine(Index, step, Count, IsNavigable);
            if (next < 0 && GetEdge(direction).Kind == EdgePolicyKind.WrapRow) next = FindWrap(Index, step, Count, IsNavigable);
            if (next < 0) return false;

            Index = next;
            _onMoved.OnNext(next);
            return true;
        }

        private bool IsNavigable(int index)
        {
            return _isNavigable == null || _isNavigable(index);
        }

        public override void Dispose()
        {
            base.Dispose();
            _onMoved.Dispose();
        }
    }
}
