using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer.Unity;

namespace Void2610.Arinn
{
    /// <summary>
    /// 今のスコープ（最前面のウィンドウ、なければ基底画面）に登録された <see cref="NavigationScope"/> に従って、
    /// 方向入力から選択を動かす。スコープの外の候補は返さないので、ウィンドウの背面の UI へは飛ばない。
    /// スコープは画面が <see cref="INavigationScopeSource"/> で宣言する（<see cref="WindowBase"/> は既定で自分の transform を根にする）。
    /// <see cref="Register"/> で登録したスコープは宣言より優先する。
    /// スコープを持たない画面や、入力（<see cref="SetInput"/>）が無いときは Unity の移動に任せる。
    /// VContainer では <see cref="ArinnContainerBuilderExtensions.RegisterArinnNavigation"/> で登録し、毎フレームの <see cref="Tick"/> を回す。
    /// </summary>
    public sealed class NavigationController : ITickable, IDisposable
    {
        /// <summary>
        /// 最後に生成されたインスタンス。DI を通さないコードから引くためのロケータ。
        /// </summary>
        public static NavigationController Instance { get; private set; }

        /// <summary>
        /// 選択が変わった通知。きっかけ（方向入力・ホバー・それ以外）を区別できる。
        /// </summary>
        public Observable<NavigationSelectionChange> SelectionChanged => _selectionChanged;

        /// <summary>
        /// 直前の <see cref="Tick"/> で有効だったスコープ。無ければ null。
        /// </summary>
        public NavigationScope ActiveScope { get; private set; }

        internal IFrameClock Clock { get; set; } = UnityFrameClock.Instance;

        // EditMode では GraphicRaycaster が画面座標で当たらないため、テストで当たり判定を差し替える
        internal Action<EventSystem, PointerEventData, List<RaycastResult>> Raycaster { get; set; } =
            static (eventSystem, pointerData, results) => eventSystem.RaycastAll(pointerData, results);

        private readonly UIFocusManager _focusManager;
        private readonly Dictionary<IFocusSource, NavigationScope> _scopes = new();
        // 画面が宣言したスコープ。null（スコープを持たない）も覚えて、毎フレーム作り直さない
        private readonly Dictionary<IFocusSource, NavigationScope> _declaredScopes = new();
        private readonly List<IFocusSource> _deadOwners = new();
        private readonly DirectionRepeater _repeater = new();
        private readonly Subject<NavigationSelectionChange> _selectionChanged = new();
        private readonly List<RaycastResult> _raycastResults = new();

        private INavigationInput _input;
        private Func<bool> _moveBlocker;
        private IPointerPositionSource _pointer;
        private Func<GameObject, bool> _hoverIgnore;
        private Vector2? _lastPointerPosition;

        private GameObject _lastSelected;
        private GameObject _changedByInput;
        private GameObject _changedByHover;
        private bool _disposed;

        public NavigationController(UIFocusManager focusManager)
        {
            _focusManager = focusManager;
            Instance = this;
        }

        // Domain Reload を切った環境で、前回のプレイのインスタンスを掴んだままにしない
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _input?.RestoreUnityMove();
            if (_input is IDisposable disposable) disposable.Dispose();
            _input = null;
            _scopes.Clear();
            _declaredScopes.Clear();
            _selectionChanged.Dispose();
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 方向入力を設定する。null で外すと、以後の移動は Unity に任せる。
        /// 渡した入力が IDisposable なら、差し替え時とこのコントローラーの破棄時に Dispose する。
        /// </summary>
        public NavigationController SetInput(INavigationInput input)
        {
            if (_input == input) return this;
            _input?.RestoreUnityMove();
            if (_input is IDisposable disposable) disposable.Dispose();
            _input = input;
            _repeater.Reset();
            return this;
        }

        /// <summary>
        /// 方向入力を移動として扱わない条件を設定する（LB を押しながらの十字キーを別の操作に割り当てる場合など）。
        /// true の間は選択を動かさず、EventSystem の移動も戻さない（条件の側で止めている移動を勝手に戻さないため）。
        /// </summary>
        public NavigationController SetMoveBlocker(Func<bool> isBlocked)
        {
            _moveBlocker = isBlocked;
            _repeater.Reset();
            return this;
        }

        /// <summary>
        /// ポインタが動いたときだけ、ポインタの下の要素を選択する。ポインタが止まっている間はパッドの操作を上書きしない。
        /// スコープがあれば、スコープの外の要素は選択しない。
        /// </summary>
        /// <param name="pointer">ポインタの位置</param>
        /// <param name="ignore">ホバーの対象から外すオブジェクト（Raycast で当たったオブジェクトが渡る）。外したものは貫通して下を見る</param>
        public NavigationController EnableHoverSelection(IPointerPositionSource pointer, Func<GameObject, bool> ignore = null)
        {
            _pointer = pointer;
            _hoverIgnore = ignore;
            _lastPointerPosition = null;
            return this;
        }

        public void DisableHoverSelection()
        {
            _pointer = null;
            _hoverIgnore = null;
            _lastPointerPosition = null;
        }

        /// <summary>
        /// 画面（ウィンドウ・基底画面）にスコープを結び付ける。同じ画面に登録し直すと置き換わる。
        /// 破棄された MonoBehaviour の登録は自動で外れる。それ以外の画面は、返り値の Dispose か <see cref="Unregister"/> で外すこと。
        /// </summary>
        public IDisposable Register(IFocusSource owner, NavigationScope scope)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            _scopes[owner] = scope ?? throw new ArgumentNullException(nameof(scope));
            return Disposable.Create(() =>
            {
                // 登録し直した後の古い Dispose で、新しいスコープを外さない
                if (_scopes.TryGetValue(owner, out var current) && current == scope) _scopes.Remove(owner);
            });
        }

        public void Unregister(IFocusSource owner)
        {
            if (owner != null) _scopes.Remove(owner);
        }

        /// <summary>
        /// <see cref="Register"/> で画面に結び付けたスコープ。無ければ null（画面が宣言したスコープは含まない）。
        /// </summary>
        public NavigationScope GetScope(IFocusSource owner) => owner != null && _scopes.TryGetValue(owner, out var scope) ? scope : null;

        public void Tick()
        {
            if (_disposed) return;

            PruneDeadOwners();
            var eventSystem = EventSystem.current;
            var owner = FindActiveOwner(eventSystem);
            var scope = GetScope(owner) ?? GetDeclaredScope(owner);
            if (scope != ActiveScope) _repeater.Reset();
            ActiveScope = scope;

            if (eventSystem)
            {
                UpdateMove(eventSystem, owner, scope);
                UpdateHover(eventSystem, scope);
                FollowSelection(eventSystem, scope);
            }
            else
            {
                _repeater.Reset();
                _input?.RestoreUnityMove();
            }
        }

        private bool IsMoveBlocked => _moveBlocker?.Invoke() == true;

        /// <summary>
        /// 最前面のウィンドウ、なければ基底画面。常時表示 UI を借りている間は、今の選択を含むスコープの画面。
        /// </summary>
        private IFocusSource FindActiveOwner(EventSystem eventSystem)
        {
            if (_focusManager == null) return null;
            if (!_focusManager.IsInPersistentUIMode)
            {
                var top = _focusManager.TopWindow;
                return top ? top : _focusManager.BaseFocusSource;
            }

            var selected = eventSystem ? eventSystem.currentSelectedGameObject : null;
            if (!selected) return null;
            foreach (var (owner, scope) in _scopes)
            {
                if (scope.Contains(selected)) return owner;
            }
            foreach (var (owner, scope) in _declaredScopes)
            {
                if (scope != null && scope.Contains(selected) && !_scopes.ContainsKey(owner)) return owner;
            }
            return null;
        }

        private void UpdateMove(EventSystem eventSystem, IFocusSource owner, NavigationScope scope)
        {
            var blocked = IsMoveBlocked;
            if (_input == null || scope is not { ResolvesMove: true })
            {
                _repeater.Reset();
                if (!blocked) _input?.RestoreUnityMove();
                return;
            }

            _input.SuppressUnityMove();
            if (blocked)
            {
                _repeater.Reset();
                return;
            }

            var direction = _repeater.Advance(_input.ReadMove(), Clock.UnscaledTime, _input.RepeatDelay, _input.RepeatRate);
            if (direction == null) return;

            var selected = eventSystem.currentSelectedGameObject;
            var current = selected ? selected.GetComponent<Selectable>() : null;
            if (!current || !scope.Contains(current))
            {
                // スコープの外（背面の UI）や未選択のときは、動かす代わりにスコープの中へ戻す
                var fallback = UnityObjects.GetDefaultFocusElement(owner);
                if (fallback && fallback.activeInHierarchy && scope.Contains(fallback)) SelectBy(eventSystem, fallback, SelectionChangeSource.Input);
                return;
            }

            var next = scope.Resolve(current, direction.Value);
            // 解決器が範囲外や操作できない要素を返しても、封じ込めは破らない
            if (!next || next == current || !scope.Contains(next) || !NavigationScope.IsNavigable(next)) return;
            SelectBy(eventSystem, next.gameObject, SelectionChangeSource.Input);
        }

        private void UpdateHover(EventSystem eventSystem, NavigationScope scope)
        {
            if (_pointer == null) return;
            if (!_pointer.TryGetPosition(out var position))
            {
                _lastPointerPosition = null;
                return;
            }

            // 最初の 1 回とポインタが止まっている間は選択を変えない（パッドの操作をポインタの位置で上書きしない）
            var moved = _lastPointerPosition.HasValue && _lastPointerPosition.Value != position;
            _lastPointerPosition = position;
            if (!moved) return;

            var target = FindHoverTarget(eventSystem, position, scope);
            if (target && target != eventSystem.currentSelectedGameObject) SelectBy(eventSystem, target, SelectionChangeSource.Hover);
        }

        private GameObject FindHoverTarget(EventSystem eventSystem, Vector2 position, NavigationScope scope)
        {
            var pointerData = new PointerEventData(eventSystem) { position = position };
            _raycastResults.Clear();
            Raycaster(eventSystem, pointerData, _raycastResults);

            foreach (var result in _raycastResults)
            {
                var hit = result.gameObject;
                if (_hoverIgnore != null && _hoverIgnore(hit)) continue;

                // 最前面に当たったものだけを見る。操作できない要素に当たっても、その下へは貫通させない
                var selectable = hit.GetComponentInParent<Selectable>();
                if (!selectable || !NavigationScope.IsNavigable(selectable)) return null;
                if (scope != null && !scope.Contains(selectable)) return null;
                return selectable.gameObject;
            }
            return null;
        }

        private void FollowSelection(EventSystem eventSystem, NavigationScope scope)
        {
            var selected = eventSystem.currentSelectedGameObject;
            if (selected == _lastSelected) return;

            _lastSelected = selected;
            var source = selected && selected == _changedByInput ? SelectionChangeSource.Input
                : selected && selected == _changedByHover ? SelectionChangeSource.Hover
                : SelectionChangeSource.Program;
            _changedByInput = null;
            _changedByHover = null;
            if (!selected) return;

            _selectionChanged.OnNext(new NavigationSelectionChange(selected, source));
            // ポインタを乗せただけでコンテンツが跳ねないよう、ホバーでは追従しない
            if (source != SelectionChangeSource.Hover && scope?.ScrollRect && selected.transform is RectTransform rect)
                ScrollIntoView.EnsureVisible(scope.ScrollRect, rect);
        }

        private void SelectBy(EventSystem eventSystem, GameObject target, SelectionChangeSource source)
        {
            if (source == SelectionChangeSource.Input) _changedByInput = target;
            else if (source == SelectionChangeSource.Hover) _changedByHover = target;
            eventSystem.SetSelectedGameObject(target);
        }

        /// <summary>
        /// 画面が <see cref="INavigationScopeSource"/> で宣言したスコープ。最初に今の画面になったときに一度だけ作る。
        /// </summary>
        private NavigationScope GetDeclaredScope(IFocusSource owner)
        {
            if (owner is not INavigationScopeSource source || !UnityObjects.IsAlive(owner)) return null;
            if (_declaredScopes.TryGetValue(owner, out var scope)) return scope;
            scope = source.CreateNavigationScope();
            _declaredScopes[owner] = scope;
            return scope;
        }

        private void PruneDeadOwners()
        {
            PruneDeadOwners(_scopes);
            PruneDeadOwners(_declaredScopes);
        }

        private void PruneDeadOwners(Dictionary<IFocusSource, NavigationScope> scopes)
        {
            foreach (var owner in scopes.Keys)
            {
                if (!UnityObjects.IsAlive(owner)) _deadOwners.Add(owner);
            }
            if (_deadOwners.Count == 0) return;
            foreach (var owner in _deadOwners) scopes.Remove(owner);
            _deadOwners.Clear();
        }
    }
}
