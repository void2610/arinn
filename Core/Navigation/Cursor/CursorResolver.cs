using System;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn
{
    /// <summary>
    /// 仮想カーソルを持つスコープの解決器。<see cref="NavigationScope.UseResolver"/> で渡す。
    /// area の全面に見えないアンカー（<see cref="Anchor"/>）を置き、アンカーが選択されている間の方向入力をカーソルへ渡す。
    /// アンカー以外が選択されているときは fallback（既定は <see cref="SpatialResolver"/>）で解決するので、
    /// 周りのボタンからは area の位置へ向かってアンカーへ入れる。
    /// </summary>
    /// <example>
    /// <code>
    /// var cursor = new GridCursor(5, 3).OnEdge(NavigationDirection.Down, EdgePolicy.Exit(closeButton));
    /// var resolver = new CursorResolver(cursor, tileArea);
    /// navigation.Register(this, new NavigationScope(transform).UseResolver(resolver));
    /// SetDefaultFocusElement(resolver.Anchor);
    /// </code>
    /// </example>
    public sealed class CursorResolver : INavigationResolver, IDisposable
    {
        /// <summary>
        /// カーソルの代わりにフォーカスを受ける要素。画面の既定要素にすればカーソルから始まる。
        /// </summary>
        public Selectable Anchor => _anchor;

        public IVirtualCursor Cursor { get; }

        private readonly VirtualCursorAnchor _anchor;
        private readonly INavigationResolver _fallback;
        private readonly IDisposable _subscriptions;

        /// <param name="cursor">動かすカーソル</param>
        /// <param name="area">カーソルが動く範囲。スコープの Root の配下であること。アンカーはこの子に全面で置かれる</param>
        /// <param name="fallback">アンカー以外が選択されているときの解決器。null なら <see cref="SpatialResolver"/></param>
        public CursorResolver(IVirtualCursor cursor, RectTransform area, INavigationResolver fallback = null)
        {
            Cursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
            if (!area) throw new ArgumentNullException(nameof(area));
            _fallback = fallback ?? SpatialResolver.Instance;
            _anchor = CreateAnchor(area);
            _subscriptions = Disposable.Combine(
                _anchor.OnFocusChanged.Subscribe(Cursor.SetFocused),
                _anchor.OnSubmitted.Subscribe(_ => Cursor.Submit()));
        }

        public Selectable Resolve(NavigationScope scope, Selectable current, NavigationDirection direction)
        {
            if (current != _anchor) return _fallback.Resolve(scope, current, direction);
            if (Cursor.TryMove(direction)) return null;

            var edge = Cursor.GetEdge(direction);
            return edge.Kind == EdgePolicyKind.Exit ? scope.ResolveExit(edge, current) : null;
        }

        private static VirtualCursorAnchor CreateAnchor(RectTransform area)
        {
            // prefab-view の規約の例外。見た目を持たない内部のオブジェクトで、利用側に置かせるとマーカーコンポーネントになるため
            var go = new GameObject("VirtualCursorAnchor", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(area, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.layer = area.gameObject.layer;
            return go.AddComponent<VirtualCursorAnchor>();
        }

        /// <summary>
        /// アンカーを破棄する。area と一緒に破棄される場合は呼ばなくてよい。
        /// </summary>
        public void Dispose()
        {
            if (!_anchor) return;
            _subscriptions.Dispose();
            if (Application.isPlaying) UnityEngine.Object.Destroy(_anchor.gameObject);
            else UnityEngine.Object.DestroyImmediate(_anchor.gameObject);
        }
    }
}
