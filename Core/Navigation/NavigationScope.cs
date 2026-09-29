using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn
{
    /// <summary>
    /// ナビゲーションの範囲（ウィンドウや基底画面の中）と、その中での振る舞いの宣言。
    /// 移動先の候補は Root の配下の Selectable だけで、Inspector の Navigation 設定は参照しない。
    /// 端の挙動・除外・解決器・スクロール追従はすべてコードで宣言する。
    /// 画面が <see cref="INavigationScopeSource.CreateNavigationScope"/> で宣言するか（<see cref="WindowBase"/> はオーバーライドする）、
    /// <see cref="NavigationController.Register"/> で画面と結び付ける。
    /// </summary>
    public sealed class NavigationScope
    {
        /// <summary>
        /// 候補を探す範囲の根。
        /// </summary>
        public Transform Root { get; }

        /// <summary>
        /// true なら Navigate をライブラリが解決する（EventSystem の move は止まる）。
        /// false なら移動は Unity に任せ、選択に追従するスクロールだけを行う。
        /// </summary>
        public bool ResolvesMove { get; }

        /// <summary>
        /// 方向入力から移動先を決める解決器。既定は <see cref="SpatialResolver"/>。
        /// </summary>
        public INavigationResolver Resolver { get; private set; } = SpatialResolver.Instance;

        /// <summary>
        /// 選択に追従してスクロールさせる ScrollRect。無ければ null。
        /// </summary>
        public ScrollRect ScrollRect { get; private set; }

        private readonly EdgePolicySet _edges = new();
        private readonly List<Func<Selectable, bool>> _excludes = new();

        public NavigationScope(Transform root, bool resolvesMove = true)
        {
            Root = root;
            ResolvesMove = resolvesMove;
        }

        /// <summary>
        /// 解決器を差し替える。仮想カーソルを使う画面では <see cref="CursorResolver"/> を渡す。
        /// </summary>
        public NavigationScope UseResolver(INavigationResolver resolver)
        {
            Resolver = resolver ?? SpatialResolver.Instance;
            return this;
        }

        /// <summary>
        /// 選択に追従してスクロールする ScrollRect を登録する。
        /// </summary>
        public NavigationScope WithScrollIntoView(ScrollRect scrollRect)
        {
            ScrollRect = scrollRect;
            return this;
        }

        /// <summary>
        /// 指定方向の端での挙動を宣言する。宣言しない方向は <see cref="EdgePolicy.Stop"/>。
        /// </summary>
        public NavigationScope OnEdge(NavigationDirection direction, EdgePolicy policy)
        {
            _edges.Set(direction, policy);
            return this;
        }

        /// <summary>
        /// 移動先の候補から外す要素の条件を宣言する。複数宣言したときはいずれかに当てはまれば外れる。
        /// </summary>
        public NavigationScope Exclude(Func<Selectable, bool> predicate)
        {
            _excludes.Add(predicate);
            return this;
        }

        /// <summary>
        /// 指定方向の端での挙動。
        /// </summary>
        public EdgePolicy GetEdge(NavigationDirection direction) => _edges.Get(direction);

        /// <summary>
        /// 要素がこのスコープの範囲内か。
        /// </summary>
        public bool Contains(Selectable selectable) => selectable && Contains(selectable.transform);

        /// <summary>
        /// オブジェクトがこのスコープの範囲内か。
        /// </summary>
        public bool Contains(GameObject target) => target && Contains(target.transform);

        /// <summary>
        /// current から direction へ移動した先を返す。移動先が無ければ（端で止まる場合や解決器が内部で処理した場合を含む）null。
        /// </summary>
        public Selectable Resolve(Selectable current, NavigationDirection direction) =>
            ResolvesMove && Resolver != null ? Resolver.Resolve(this, current, direction) : null;

        /// <summary>
        /// 移動先の候補を集める。Root の配下で、操作でき、除外に当てはまらない要素。
        /// Scrollbar はドラッグ操作用でカーソル移動の対象にしない。
        /// </summary>
        public List<Selectable> CollectCandidates(Selectable current)
        {
            var result = new List<Selectable>();
            if (!Root) return result;

            foreach (var selectable in Root.GetComponentsInChildren<Selectable>(false))
            {
                if (selectable == current) continue;
                if (selectable is Scrollbar) continue;
                if (!IsNavigable(selectable)) continue;
                if (IsExcluded(selectable)) continue;
                result.Add(selectable);
            }
            return result;
        }

        /// <summary>
        /// <see cref="EdgePolicy.Exit"/> の抜け先を検証して返す。スコープ内で操作でき、自分自身でなければ抜け先、それ以外は null（止まる）。
        /// </summary>
        public Selectable ResolveExit(EdgePolicy policy, Selectable current)
        {
            var target = policy.ExitTarget;
            return target && target != current && Contains(target) && IsNavigable(target) ? target : null;
        }

        /// <summary>
        /// 選択先にできる要素か。アクティブで、操作でき、RectTransform を持つこと。
        /// </summary>
        public static bool IsNavigable(Selectable selectable) =>
            selectable && selectable.isActiveAndEnabled && selectable.IsInteractable() && selectable.transform is RectTransform;

        private bool Contains(Transform target) => Root && target.IsChildOf(Root);

        private bool IsExcluded(Selectable selectable)
        {
            foreach (var exclude in _excludes)
            {
                if (exclude(selectable)) return true;
            }
            return false;
        }
    }
}
