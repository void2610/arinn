using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer.Unity;

namespace Void2610.Arinn
{
    /// <summary>
    /// フォーカスを一時的に持つもの（ウィンドウと、常時表示 UI の借用）を 1 つのスコープのスタックで管理する。
    /// スコープを積むときに今のフォーカスを預かり、外したときに返す。フォーカスが消えたら自動で戻す。
    /// VContainer では <see cref="ArinnContainerBuilderExtensions.RegisterArinn"/> で登録し、毎フレームの <see cref="Tick"/> を回す。
    /// </summary>
    public sealed class UIFocusManager : ITickable, IDisposable
    {
        /// <summary>
        /// 最後に生成されたインスタンス。DI を通さない View（閉じるボタンなど）から引くためのロケータ。
        /// </summary>
        public static UIFocusManager Instance { get; private set; }

        /// <summary>
        /// 開いているウィンドウがあるか。
        /// </summary>
        public bool HasOpenWindows => _scopes.Exists(scope => scope.IsWindow);

        /// <summary>
        /// 最前面のウィンドウ。開いていなければ null。
        /// </summary>
        public WindowBase TopWindow => _scopes.FindLast(scope => scope.IsWindow)?.Window;

        /// <summary>
        /// 開いているウィンドウの数。
        /// </summary>
        public int WindowCount => _scopes.FindAll(scope => scope.IsWindow).Count;

        /// <summary>
        /// フォーカスが、最前面のウィンドウ（なければ基底画面）の既定要素にあるか。
        /// </summary>
        public bool IsFocusOnDefaultElement
        {
            get
            {
                var selected = CurrentSelected;
                if (!selected) return false;
                var source = TopWindow ? TopWindow : BaseFocusSource;
                return selected == GetDefaultFocusElement(source);
            }
        }

        /// <summary>
        /// 常時表示 UI にフォーカスを借りている最中か。
        /// </summary>
        public bool IsInPersistentUIMode => BorrowScope != null;

        /// <summary>
        /// ウィンドウが 1 枚もないときのフォーカス先（ウィンドウの下にある画面）。
        /// </summary>
        public IFocusSource BaseFocusSource { get; private set; }

        /// <summary>
        /// 遷移を指定していないウィンドウが使う遷移。
        /// </summary>
        public IWindowTransition DefaultTransition { get; set; } = InstantWindowTransition.Instance;

        /// <summary>
        /// フォーカスを失ってから既定要素へ戻すまでの待ち時間（直前の要素が消えていた場合）。
        /// </summary>
        public float FocusRecoveryDelaySeconds { get; set; } = 0.5f;

        internal IFrameClock Clock { get; set; } = UnityFrameClock.Instance;

        // 非アクティブな要素がアクティブになるのを待つ上限。通常は数フレームで済む想定の安全装置
        private const int WAIT_FOR_ACTIVE_MAX_FRAMES = 120;

        // 末尾が最前面
        private readonly List<FocusScope> _scopes = new();
        private IInputScopeGate _inputScopeGate;
        private ISubmitHoldProbe _submitHoldProbe;
        private FocusRequest _pendingFocus;
        // 常時表示 UI へ移る予約。ウィンドウの予約と取り合わないよう別の枠で持つ
        private FocusRequest _pendingPersistentFocus;

        private GameObject _lastSelected;
        private float? _focusLostSince;
        private bool _disposed;

        public UIFocusManager()
        {
            Instance = this;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        /// <summary>
        /// ウィンドウの開閉に合わせてゲームプレイ側の入力を止める窓口を設定する。
        /// </summary>
        public void SetInputScopeGate(IInputScopeGate gate) => _inputScopeGate = gate;

        /// <summary>
        /// 決定入力の押下を調べる手段を設定する。未設定なら常に「押されていない」とみなす。
        /// </summary>
        public void SetSubmitHoldProbe(ISubmitHoldProbe probe) => _submitHoldProbe = probe;

        /// <summary>
        /// 指定ウィンドウがスタックに積まれているか。
        /// </summary>
        public bool IsWindowInStack(WindowBase window) => _scopes.Exists(scope => scope.Window == window);

        /// <summary>
        /// 基底画面だけを差し替える。フォーカスは動かさない。
        /// </summary>
        public void SetBaseFocusSource(IFocusSource source) => BaseFocusSource = source;

        /// <summary>
        /// 常時表示 UI へフォーカスを借りる。借用のスコープとして一番上に積む。
        /// </summary>
        public void EnterPersistentUIFocus(GameObject element) => EnterPersistentUIFocus(element, element);

        /// <summary>
        /// 入力でウィンドウを開閉するショートカットを登録する。他のウィンドウが開いている間は開かない。
        /// </summary>
        public void RegisterToggleAction<T>(Observable<T> input, WindowBase window, CompositeDisposable disposables, Func<bool> canToggle = null) => RegisterToggleAction(input, window, () => ShowWindow(window), disposables, canToggle);

        /// <summary>
        /// 入力でウィンドウを開閉するショートカットを登録する。開くときは onOpen を呼ぶ（ShowWindow を含めること）。
        /// </summary>
        public void RegisterToggleAction<T>(Observable<T> input, WindowBase window, Action onOpen, CompositeDisposable disposables, Func<bool> canToggle = null) =>
            input
                .Where(_ => canToggle == null || canToggle())
                .Where(_ => !HasOpenWindows || window.IsVisible)
                .Subscribe(_ =>
                {
                    if (window.IsVisible) HideWindow(window);
                    else onOpen();
                })
                .AddTo(disposables);

        /// <summary>
        /// 入力でウィンドウの表示内容を切り替えるショートカットを登録する。
        /// 開いていて shouldClose が true なら閉じ、それ以外は（閉じていれば開いてから）onShowContent を呼ぶ。
        /// </summary>
        public void RegisterToggleAction<T>(Observable<T> input, WindowBase window, Action onShowContent, Func<bool> shouldClose, CompositeDisposable disposables, Func<bool> canToggle = null) =>
            input
                .Where(_ => canToggle == null || canToggle())
                .Where(_ => !HasOpenWindows || window.IsVisible)
                .Subscribe(_ =>
                {
                    if (window.IsVisible && shouldClose())
                    {
                        HideWindow(window);
                        return;
                    }
                    if (!window.IsVisible) ShowWindow(window);
                    onShowContent();
                })
                .AddTo(disposables);

        /// <summary>
        /// ウィンドウを開く。既に開いていれば何もしない。
        /// </summary>
        public void ShowWindow(WindowBase window)
        {
            if (IsWindowInStack(window)) return;

            // 先に入力を受け付ける状態にしてから、フォーカスを予約する
            window.Show();
            if (!HasOpenWindows) _inputScopeGate?.OnFirstWindowOpened();
            _scopes.Add(FocusScope.ForWindow(window, CurrentSelected));
            // 同じフレーム内の上書きを避け、動的に作られる既定要素も拾えるよう 1 フレーム後に評価する
            _pendingFocus = FocusRequest.NextFrame(() => GetDefaultFocusElement(window), Clock.FrameCount);
        }

        /// <summary>
        /// ウィンドウを閉じ、開く前のフォーカスへ戻す。
        /// </summary>
        public void HideWindow(WindowBase window)
        {
            PopWindow(window);
            window.Hide();
        }

        /// <summary>
        /// 開いていれば閉じ、閉じていれば開く。
        /// </summary>
        public void ToggleWindow(WindowBase window)
        {
            if (window.IsVisible) HideWindow(window);
            else ShowWindow(window);
        }

        /// <summary>
        /// 最前面のウィンドウを閉じる。Cancel で閉じられないウィンドウなら閉じない。
        /// </summary>
        /// <returns>閉じた場合は true</returns>
        public bool TryCloseTopWindow()
        {
            var top = TopWindow;
            if (!top || !top.IsClosableByCancelInput) return false;
            HideWindow(top);
            return true;
        }

        /// <summary>
        /// 一番上のフォーカススコープを 1 つ閉じる。借用なら借りる前のフォーカスへ戻り、
        /// ウィンドウなら閉じる（Cancel で閉じられないウィンドウは閉じない）。Cancel 入力の既定の処理として使う。
        /// </summary>
        /// <returns>何かを閉じた場合は true</returns>
        public bool TryPopScope()
        {
            if (_scopes.Count == 0) return false;
            if (_scopes[^1].IsWindow) return TryCloseTopWindow();
            ExitPersistentUIFocus();
            return true;
        }

        /// <summary>
        /// すべてのウィンドウを閉じ、基底画面へフォーカスを戻す。
        /// </summary>
        public void CloseAll()
        {
            _pendingFocus = null;
            _pendingPersistentFocus = null;
            if (!HasOpenWindows) return;

            var bottomPreviousFocus = _scopes.Find(scope => scope.IsWindow).PreviousFocus;
            // Hide の購読側が開き直しても壊れないよう、先にスタックからウィンドウを外してから閉じる
            var windows = _scopes.FindAll(scope => scope.IsWindow).ConvertAll(scope => scope.Window);
            _scopes.RemoveAll(scope => scope.IsWindow);
            foreach (var window in windows)
                window.Hide();

            // 購読側が開き直した場合は、そのウィンドウの入力とフォーカスを奪わない
            if (HasOpenWindows) return;
            _inputScopeGate?.OnLastWindowClosed();
            RestoreFocusAfterLastWindow(bottomPreviousFocus);
        }

        /// <summary>
        /// 基底画面を切り替える。開いているウィンドウをすべて閉じ、新しい基底画面の既定要素へフォーカスする。
        /// 画面遷移（ゲームのステート変更など）で呼ぶ。
        /// </summary>
        public void SwitchBase(IFocusSource source)
        {
            BaseFocusSource = source;
            CloseAll();
            // 閉じたウィンドウの購読側が開き直した場合は、そのウィンドウのフォーカスを奪わない
            if (HasOpenWindows) return;
            _pendingFocus = null;
            // 前の画面で借りていた常時表示 UI のフォーカスは、新しい画面では意味を持たない
            ClearPersistentUIFocus();
            FocusWhenActive(GetDefaultFocusElement(source));
        }

        /// <summary>
        /// 常時表示 UI へフォーカスを借りる。owner は同じ UI かどうかの判定に使う。
        /// </summary>
        public void EnterPersistentUIFocus(GameObject element, GameObject owner)
        {
            var borrow = BorrowScope;
            if (borrow != null && borrow == TopScope)
            {
                borrow.Owner = owner;
                SetSelected(element);
                return;
            }

            // 借用の上に開いたウィンドウから借り直したときは、古い借用を外して一番上に積み直す
            if (borrow != null) _scopes.Remove(borrow);
            _scopes.Add(FocusScope.ForBorrow(CurrentSelected, owner));
            // 決定との同時押しで移った直後に、決定の離しで移動先が押されないよう、決定が離れるまで待ってから移す
            _pendingPersistentFocus = FocusRequest.NextFrame(() => element, Clock.FrameCount, holdWhile: IsSubmitHeld, isPersistent: true);
        }

        /// <summary>
        /// 同じ UI なら元のフォーカスへ戻り、別の UI ならそちらへ借り直す。
        /// </summary>
        public void TogglePersistentUIFocus(GameObject element, GameObject owner)
        {
            if (BorrowScope?.Owner == owner)
            {
                ExitPersistentUIFocus();
                return;
            }
            EnterPersistentUIFocus(element, owner);
        }

        /// <summary>
        /// 常時表示 UI から借りる前のフォーカスへ戻る。
        /// </summary>
        public void ExitPersistentUIFocus()
        {
            var borrow = BorrowScope;
            if (borrow == null) return;

            var wasTop = borrow == TopScope;
            var previous = borrow.PreviousFocus;
            ClearPersistentUIFocus();
            // 借用の上にウィンドウが開いていれば、そのウィンドウのフォーカスはそのまま
            if (!wasTop) return;
            // 借りる前の要素が消えていたら、最前面のウィンドウ（なければ基底画面）の既定要素へ戻す
            if (IsFocusable(previous)) SetSelected(previous);
            else SetSelected(GetDefaultFocusElement(TopWindow ? TopWindow : BaseFocusSource));
        }

        /// <summary>
        /// 予約したフォーカスの適用と、フォーカス喪失からの復帰を 1 フレーム分進める。
        /// </summary>
        public void Tick()
        {
            if (_disposed) return;

            if (_pendingFocus == null && _pendingPersistentFocus == null)
            {
                RecoverLostFocus();
                return;
            }

            // 予約の適用を待っている間は、復帰処理と取り合わない
            _focusLostSince = null;
            if (_pendingFocus != null)
            {
                var request = _pendingFocus;
                // 評価で例外が出ても毎フレーム投げ直さないよう、先に枠から外す
                _pendingFocus = null;
                var remaining = ProcessPendingFocus(request);
                // 選択の通知の中で新しい予約が入っていれば、そちらを残す
                _pendingFocus ??= remaining;
            }
            if (_pendingPersistentFocus != null)
            {
                var request = _pendingPersistentFocus;
                _pendingPersistentFocus = null;
                var remaining = ProcessPendingFocus(request);
                _pendingPersistentFocus ??= remaining;
            }
        }

        // Domain Reload を切った環境で、前回のプレイのインスタンスを掴んだままにしない
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private bool IsSubmitHeld()
        {
            return _submitHoldProbe?.IsSubmitHeld == true;
        }

        private FocusScope TopScope => _scopes.Count > 0 ? _scopes[^1] : null;

        private FocusScope BorrowScope => _scopes.Find(scope => !scope.IsWindow);

        private bool IsBorrowOnTop => TopScope is { IsWindow: false };

        private static GameObject CurrentSelected => EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;

        private static GameObject GetDefaultFocusElement(IFocusSource source)
        {
            return UnityObjects.GetDefaultFocusElement(source);
        }

        private void PopWindow(WindowBase window)
        {
            var index = _scopes.FindIndex(scope => scope.Window == window);
            if (index < 0) return;

            var previousFocus = _scopes[index].PreviousFocus;
            var wasTop = index == _scopes.Count - 1;
            _scopes.RemoveAt(index);

            if (!HasOpenWindows)
            {
                _pendingFocus = null;
                _inputScopeGate?.OnLastWindowClosed();
                // 借りている常時表示 UI から開いたウィンドウなら、借りている要素へ戻る
                if (wasTop && IsBorrowOnTop && IsFocusable(previousFocus)) SetSelected(previousFocus);
                else RestoreFocusAfterLastWindow(previousFocus);
                return;
            }

            // 下のウィンドウを閉じた場合、最前面のフォーカスはそのまま
            if (!wasTop) return;
            _pendingFocus = null;
            if (IsFocusable(previousFocus)) SetSelected(previousFocus);
            else SetSelected(GetDefaultFocusElement(TopWindow));
        }

        private void RestoreFocusAfterLastWindow(GameObject previousFocus)
        {
            // 基底画面があれば、開く前のフォーカスではなく基底画面の既定要素へ戻す（戻り先を 1 つに決めるため）
            if (BaseFocusSource != null)
            {
                FocusWhenActive(GetDefaultFocusElement(BaseFocusSource));
                return;
            }
            if (IsFocusable(previousFocus)) SetSelected(previousFocus);
        }

        /// <summary>
        /// 対象が今は非アクティブでも、アクティブになった時点でフォーカスする（画面遷移の直後など）。
        /// </summary>
        private void FocusWhenActive(GameObject target)
        {
            if (!target) return;
            if (target.activeInHierarchy)
            {
                SetSelected(target);
                return;
            }
            _pendingFocus = FocusRequest.WhenActive(target, Clock.FrameCount, WAIT_FOR_ACTIVE_MAX_FRAMES);
        }

        /// <summary>
        /// 予約を 1 フレーム分進める。まだ待つなら予約を、済んだかあきらめたなら null を返す。
        /// </summary>
        private FocusRequest ProcessPendingFocus(FocusRequest request)
        {
            var frame = Clock.FrameCount;
            if (frame < request.NotBeforeFrame) return request;
            if (request.HoldWhile?.Invoke() == true) return request;

            var target = request.Resolve();
            if (!target)
            {
                // 常時表示 UI の行き先が無ければ、借りる前の要素に選択を残さない
                if (request.IsPersistent && EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                return null;
            }
            if (!target.activeInHierarchy) return request.WaitUntilActive && frame < request.GiveUpFrame ? request : null;

            SetSelected(target);
            return null;
        }

        /// <summary>
        /// フォーカスが消えたら、直前の要素がまだ操作できればすぐ戻し、
        /// 消えていれば一定時間待ってから最前面のウィンドウ（なければ基底画面）の既定要素へ戻す。
        /// </summary>
        private void RecoverLostFocus()
        {
            var eventSystem = EventSystem.current;
            // 常時表示 UI を借りている間は、借りた側の制御に任せる
            if (!eventSystem || IsBorrowOnTop)
            {
                _focusLostSince = null;
                return;
            }

            var selected = eventSystem.currentSelectedGameObject;
            if (selected)
            {
                _lastSelected = selected;
                _focusLostSince = null;
                return;
            }

            if (IsFocusable(_lastSelected))
            {
                _focusLostSince = null;
                SetSelected(_lastSelected);
                return;
            }

            var now = Clock.UnscaledTime;
            _focusLostSince ??= now;
            if (now - _focusLostSince.Value < FocusRecoveryDelaySeconds) return;

            _focusLostSince = null;
            var source = TopWindow ? TopWindow : BaseFocusSource;
            SetSelected(GetDefaultFocusElement(source));
        }

        private void ClearPersistentUIFocus()
        {
            _scopes.RemoveAll(scope => !scope.IsWindow);
            _pendingPersistentFocus = null;
        }

        private void OnActiveSceneChanged(Scene current, Scene next)
        {
            // 前のシーンの UI への参照を捨て、新しいシーンで登録し直してもらう
            _scopes.Clear();
            _pendingFocus = null;
            _pendingPersistentFocus = null;
            _inputScopeGate = null;
            BaseFocusSource = null;
            _lastSelected = null;
            _focusLostSince = null;
        }

        /// <summary>
        /// 生きていて、アクティブで、操作を受け付ける要素か。閉じたウィンドウの中の要素（CanvasGroup で操作不可）は除く。
        /// </summary>
        private static bool IsFocusable(GameObject target)
        {
            if (!target || !target.activeInHierarchy) return false;
            // 売り切れの商品のように押せないだけの要素へは戻す（候補に含める画面があるため）。閉じたウィンドウの中は CanvasGroup で弾く
            return !target.TryGetComponent<Selectable>(out _) || UnityObjects.GroupsAllowInteraction(target.transform);
        }

        private static void SetSelected(GameObject target)
        {
            var eventSystem = EventSystem.current;
            // 行き先がないときは今のフォーカスを消さない（消えたフォーカスは監視側が戻す）
            if (!eventSystem || !target || !target.activeInHierarchy) return;
            eventSystem.SetSelectedGameObject(target);
        }

        /// <summary>
        /// フォーカスを一時的に持つもの 1 つ分。ウィンドウか、常時表示 UI の借用のどちらか。
        /// </summary>
        private sealed class FocusScope
        {
            public WindowBase Window { get; private set; }
            public GameObject PreviousFocus { get; private set; }
            public GameObject Owner { get; set; }
            // 破棄されたウィンドウを借用と取り違えないよう、種類は参照の有無ではなく作ったときに決める
            public bool IsWindow { get; private set; }

            public static FocusScope ForWindow(WindowBase window, GameObject previousFocus) => new() { Window = window, PreviousFocus = previousFocus, IsWindow = true };

            public static FocusScope ForBorrow(GameObject previousFocus, GameObject owner) => new() { PreviousFocus = previousFocus, Owner = owner };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            _pendingFocus = null;
            _pendingPersistentFocus = null;
            if (Instance == this) Instance = null;
        }
    }
}
