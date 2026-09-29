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

        /// <summary>
        /// 選択した要素をビューポートの中央へ寄せるか。false ならはみ出した側の端へ揃える。
        /// </summary>
        public bool CentersScroll { get; private set; }

        /// <summary>
        /// 移動入力の丸め方。既定は <see cref="NavigationInputMode.FourWay"/>。
        /// </summary>
        public NavigationInputMode InputMode { get; private set; } = NavigationInputMode.FourWay;

        /// <summary>
        /// 押しっぱなしでリピートするか。既定は true。
        /// </summary>
        public bool Repeats { get; private set; } = true;

        /// <summary>
        /// 操作できない（interactable が false の）要素も移動先にするか。既定は false。
        /// </summary>
        public bool IncludesNonInteractable { get; private set; }
        private static readonly List<CanvasGroup> CanvasGroupBuffer = new();

        private readonly EdgePolicySet _edges = new();
        private readonly List<Func<Selectable, bool>> _excludes = new();
        private readonly Dictionary<(Selectable From, NavigationDirection Direction), Selectable> _links = new();
        private readonly List<(Selectable From, NavigationDirection Direction)> _deadLinks = new();
        private readonly List<(Func<Selectable, bool> Predicate, NavigationAxis Axes)> _elementMoves = new();

        public NavigationScope(Transform root, bool resolvesMove = true)
        {
            Root = root;
            ResolvesMove = resolvesMove;
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
        /// 選択先にできる要素か。アクティブで、操作でき、RectTransform を持つこと。
        /// </summary>
        public static bool IsNavigable(Selectable selectable) => selectable && selectable.isActiveAndEnabled && selectable.IsInteractable() && selectable.transform is RectTransform;

        /// <summary>
        /// このスコープで移動先にできる要素か。<see cref="IsNavigable"/> に加え、<see cref="IncludeNonInteractable"/> を宣言していれば操作できない要素も含める。
        /// </summary>
        public bool CanNavigateTo(Selectable selectable)
        {
            if (IsNavigable(selectable)) return true;
            return IncludesNonInteractable && selectable && selectable.isActiveAndEnabled && selectable.transform is RectTransform && GroupsAllowInteraction(selectable.transform);
        }

        /// <summary>
        /// current から direction へ移動した先を返す。移動先が無ければ（端で止まる場合や解決器が内部で処理した場合を含む）null。
        /// </summary>
        public Selectable Resolve(Selectable current, NavigationDirection direction)
        {
            if (!ResolvesMove || Resolver == null) return null;
            var linked = ResolveLink(current, direction);
            return linked ? linked : Resolver.Resolve(this, current, direction);
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
        /// <param name="scrollRect">追従させる ScrollRect</param>
        /// <param name="center">true なら選択した要素をビューポートの中央へ寄せる。false ならはみ出したときだけ端へ揃える</param>
        public NavigationScope WithScrollIntoView(ScrollRect scrollRect, bool center = false)
        {
            ScrollRect = scrollRect;
            CentersScroll = center;
            return this;
        }

        /// <summary>
        /// 移動入力の丸め方を決める。斜めに動かしたい仮想カーソルでは <see cref="NavigationInputMode.EightWay"/> にする。
        /// </summary>
        public NavigationScope WithInputMode(NavigationInputMode mode)
        {
            InputMode = mode;
            return this;
        }

        /// <summary>
        /// 押しっぱなしでもリピートせず、押し直すか向きを変えたときだけ 1 歩動かす。
        /// </summary>
        public NavigationScope WithoutRepeat()
        {
            Repeats = false;
            return this;
        }

        /// <summary>
        /// 操作できない（interactable が false の）要素も移動先にする。買えない商品や空のスロットへ移ってツールチップを見せたい画面で使う。
        /// 閉じたウィンドウのように、親の CanvasGroup で操作を止められている要素は含めない。
        /// </summary>
        public NavigationScope IncludeNonInteractable()
        {
            IncludesNonInteractable = true;
            return this;
        }

        /// <summary>
        /// predicate に当てはまる要素を選んでいる間、axes の方向入力を移動ではなくその要素の Move イベント（OnMove）として渡す。
        /// スライダーの左右で値を変えるときに使う。要素の Navigation を None にしておかないと、Unity の移動で要素から抜けることがある。
        /// </summary>
        public NavigationScope PassMoveToElement(Func<Selectable, bool> predicate, NavigationAxis axes)
        {
            _elementMoves.Add((predicate ?? throw new ArgumentNullException(nameof(predicate)), axes));
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
        /// from を選んでいるときに direction を押したら to へ移る、と明示する。位置からの自動の導出より優先する。
        /// to が非アクティブや操作できないときは自動の導出に戻り、スコープの外なら無視する。同じ from と direction で呼び直すと置き換わる。
        /// </summary>
        public NavigationScope Link(Selectable from, NavigationDirection direction, Selectable to)
        {
            if (!from) throw new ArgumentNullException(nameof(from));
            PruneDeadLinks();
            _links[(from, direction)] = to;
            return this;
        }

        /// <summary>
        /// <see cref="Link"/> で明示した移動先をすべて外す。要素を作り直す画面で、張り直す前に呼ぶ。
        /// </summary>
        public NavigationScope ClearLinks()
        {
            _links.Clear();
            return this;
        }

        /// <summary>
        /// <see cref="Link"/> で明示した移動先を外し、自動の導出に戻す。
        /// </summary>
        public NavigationScope Unlink(Selectable from, NavigationDirection direction)
        {
            _links.Remove((from, direction));
            return this;
        }

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
                if (!CanNavigateTo(selectable)) continue;
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
            return target && target != current && Contains(target) && CanNavigateTo(target) ? target : null;
        }

        /// <summary>
        /// current を選んでいるときの direction の入力を、移動ではなく要素の Move イベントとして渡すか。
        /// </summary>
        internal bool PassesMoveToElement(Selectable current, NavigationDirection direction)
        {
            var axis = direction is NavigationDirection.Left or NavigationDirection.Right ? NavigationAxis.Horizontal : NavigationAxis.Vertical;
            foreach (var (predicate, axes) in _elementMoves)
            {
                if ((axes & axis) != 0 && predicate(current)) return true;
            }
            return false;
        }

        private bool Contains(Transform target)
        {
            return Root && target.IsChildOf(Root);
        }

        // 行き先が今選べないときは、明示した移動先で止めずに自動の導出へ戻す（売り切れの枠などで行き止まりにしない）
        private Selectable ResolveLink(Selectable current, NavigationDirection direction)
        {
            if (!current || !_links.TryGetValue((current, direction), out var target)) return null;
            return target && target != current && Contains(target) && CanNavigateTo(target) ? target : null;
        }

        // 親の CanvasGroup が操作を止めているか。Selectable.IsInteractable の CanvasGroup の判定だけを取り出したもの
        private static bool GroupsAllowInteraction(Transform transform)
        {
            for (var t = transform; t; t = t.parent)
            {
                t.GetComponents(CanvasGroupBuffer);
                foreach (var group in CanvasGroupBuffer)
                {
                    if (!group.enabled) continue;
                    if (!group.interactable) return false;
                    if (group.ignoreParentGroups) return true;
                }
            }
            return true;
        }

        private void PruneDeadLinks()
        {
            foreach (var key in _links.Keys)
            {
                if (!key.From) _deadLinks.Add(key);
            }
            foreach (var key in _deadLinks) _links.Remove(key);
            _deadLinks.Clear();
        }

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
