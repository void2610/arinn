using System;
using R3;

namespace Void2610.Arinn
{
    /// <summary>
    /// <see cref="GridCursor"/> と <see cref="ListCursor"/> に共通する、端の挙動・決定・フォーカスの通知。
    /// </summary>
    public abstract class VirtualCursor : IVirtualCursor, IDisposable
    {
        /// <summary>
        /// アンカーが決定を受けたときに発火する。
        /// </summary>
        public Observable<Unit> OnSubmitted => _onSubmitted;

        /// <summary>
        /// アンカーの選択が変わったときに発火する（true で選択された）。
        /// </summary>
        public Observable<bool> OnFocusChanged => _onFocusChanged;

        /// <summary>
        /// アンカーが選択されているか。
        /// </summary>
        public bool IsFocused { get; private set; }

        protected EdgePolicySet Edges { get; } = new();

        private readonly Subject<Unit> _onSubmitted = new();
        private readonly Subject<bool> _onFocusChanged = new();

        public abstract bool TryMove(NavigationDirection direction);

        public EdgePolicy GetEdge(NavigationDirection direction) => Edges.Get(direction);

        public void Submit() => _onSubmitted.OnNext(Unit.Default);

        public void SetFocused(bool focused)
        {
            if (IsFocused == focused) return;
            IsFocused = focused;
            _onFocusChanged.OnNext(focused);
        }

        public virtual void Dispose()
        {
            _onSubmitted.Dispose();
            _onFocusChanged.Dispose();
        }

        /// <summary>
        /// 一直線に並んだ count 個のうち、start から step ずつ進んで最初に止まれる位置を返す。無ければ -1。
        /// start 自身は調べない。
        /// </summary>
        protected static int FindAlongLine(int start, int step, int count, Func<int, bool> canStop)
        {
            for (var i = start + step; i >= 0 && i < count; i += step)
            {
                if (canStop(i)) return i;
            }
            return -1;
        }

        /// <summary>
        /// 回り込み先を返す。進行方向の反対側の端から current に向かって、最初に止まれる位置。無ければ -1。
        /// </summary>
        protected static int FindWrap(int current, int step, int count, Func<int, bool> canStop)
        {
            var from = step > 0 ? -1 : count;
            var found = FindAlongLine(from, step, count, canStop);
            return found == current ? -1 : found;
        }
    }
}
