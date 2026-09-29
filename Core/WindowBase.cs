using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Void2610.Arinn
{
    /// <summary>
    /// ウィンドウの基底クラス。CanvasGroup で表示・入力受付をまとめて切り替える。
    /// 開閉は <see cref="Open"/> / <see cref="Close"/>（中身は <see cref="UIFocusManager"/> の ShowWindow / HideWindow）で行う（表示・フォーカス・入力を一緒に変えるため）。
    /// ナビゲーションのスコープは <see cref="CreateNavigationScope"/> で宣言する。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class WindowBase : MonoBehaviour, IFocusSource, INavigationScopeSource
    {
        [SerializeField] protected Button closeButton;
        [SerializeField] protected Selectable defaultFocusElement;

        /// <summary>
        /// 閉じたときに発火する（閉じるボタン・Cancel・コードからの HideWindow のすべて）。
        /// 購読側で別のウィンドウを開いてよい。
        /// </summary>
        public Observable<Unit> OnWindowClosed => _onWindowClosed;

        /// <summary>
        /// 開いているか。フェード中の見た目ではなく、最後に開いたか閉じたかを返す。
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// 既定のフォーカス要素。動的に決める場合はオーバーライドする。
        /// </summary>
        public virtual GameObject DefaultFocusElement => defaultFocusElement ? defaultFocusElement.gameObject : null;

        /// <summary>
        /// Cancel 入力（<see cref="UIFocusManager.TryCloseTopWindow"/>）で閉じてよいか。
        /// 閉じさせたくない強制進行のオーバーレイなどでは false を返す。
        /// </summary>
        public virtual bool IsClosableByCancelInput => true;

        /// <summary>
        /// このウィンドウだけ遷移を変える場合にオーバーライドする。null ならマネージャーの既定を使う。
        /// </summary>
        protected virtual IWindowTransition Transition => null;

        private readonly Subject<Unit> _onWindowClosed = new();
        private UIFocusManager _focusManager;
        private CanvasGroup _canvasGroup;
        private CancellationTokenSource _transitionCts;
        private Button _boundCloseButton;
        private bool _destroyed;

        // EditMode など Awake が走らない状況でも使えるよう、初回アクセスで取得する
        private CanvasGroup Group => _canvasGroup ? _canvasGroup : _canvasGroup = GetComponent<CanvasGroup>();

        /// <summary>
        /// 開く。開く前に選択していた要素を預け、次のフレームで既定要素へフォーカスする。既に開いていれば何もしない。
        /// </summary>
        public void Open() => RequireManager().ShowWindow(this);

        /// <summary>
        /// 閉じて、開く前のフォーカスへ戻す。
        /// </summary>
        public void Close() => RequireManager().HideWindow(this);

        /// <summary>
        /// 開いていれば閉じ、閉じていれば開く。
        /// </summary>
        public void Toggle() => RequireManager().ToggleWindow(this);

        /// <summary>
        /// このウィンドウのナビゲーションのスコープ。最初に最前面になったときに一度だけ呼ばれる。
        /// 既定はこのウィンドウの transform を根とするスコープ。端の挙動・除外・スクロール・解決器を変えるときはオーバーライドする。
        /// </summary>
        public virtual NavigationScope CreateNavigationScope() => new(transform);

        /// <summary>
        /// 表示と入力の受付を開いた状態にする。外からは <see cref="Open"/> を使う。
        /// </summary>
        protected internal virtual void Show()
        {
            if (_destroyed) return;

            IsVisible = true;
            SetInputEnabled(true);
            RunTransition(true);
        }

        /// <summary>
        /// 表示と入力の受付を閉じた状態にする。外からは <see cref="Close"/> を使う。
        /// </summary>
        protected internal virtual void Hide()
        {
            if (_destroyed) return;

            // 見た目の遷移を待たずに入力を切る。遷移が途中で止まっても、見えないウィンドウが入力を遮り続けない
            IsVisible = false;
            SetInputEnabled(false);
            RunTransition(false);
            // 購読側が開き直しても上の状態を上書きしないよう、状態を確定させてから通知する
            _onWindowClosed.OnNext(Unit.Default);
        }

        /// <summary>
        /// 閉じるボタンをコードで指定する。クリックで <see cref="UIFocusManager.HideWindow"/> を呼ぶ。null で外す。
        /// </summary>
        protected void SetCloseButton(Button button)
        {
            closeButton = button;
            BindCloseButton(button);
        }

        /// <summary>
        /// 既定のフォーカス要素をコードで指定する。<see cref="DefaultFocusElement"/> をオーバーライドしていればそちらが優先される。
        /// </summary>
        protected void SetDefaultFocusElement(Selectable selectable) => defaultFocusElement = selectable;

        protected virtual void Awake()
        {
            SetVisibleImmediate(false);
            // 派生の Awake が先に SetCloseButton していても二重に購読しない
            if (!_boundCloseButton) BindCloseButton(closeButton);
        }

        protected virtual void OnDestroy()
        {
            _destroyed = true;
            CancelTransition();
            BindCloseButton(null);
            _onWindowClosed.Dispose();
        }

        // VContainer に登録したウィンドウは、コンテナの UIFocusManager を使う
        [Inject]
        private void InjectFocusManager(UIFocusManager focusManager) => _focusManager = focusManager;

        private UIFocusManager FocusManager => _focusManager ?? UIFocusManager.Instance;

        private void OnCloseButtonClicked() => FocusManager?.HideWindow(this);

        private UIFocusManager RequireManager() =>
            FocusManager ?? throw new InvalidOperationException("UIFocusManager が生成されていない。RegisterArinn で登録するか new UIFocusManager() で生成する");

        private void BindCloseButton(Button button)
        {
            if (_boundCloseButton) _boundCloseButton.onClick.RemoveListener(OnCloseButtonClicked);
            _boundCloseButton = button;
            if (_boundCloseButton) _boundCloseButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void SetVisibleImmediate(bool visible)
        {
            CancelTransition();
            IsVisible = visible;
            Group.alpha = visible ? 1f : 0f;
            SetInputEnabled(visible);
        }

        private void SetInputEnabled(bool enabled)
        {
            Group.interactable = enabled;
            Group.blocksRaycasts = enabled;
        }

        private void RunTransition(bool show)
        {
            CancelTransition();
            _transitionCts = new CancellationTokenSource();
            var transition = Transition ?? FocusManager?.DefaultTransition ?? InstantWindowTransition.Instance;
            RunTransitionAsync(transition, show, _transitionCts.Token).Forget();
        }

        private async UniTaskVoid RunTransitionAsync(IWindowTransition transition, bool show, CancellationToken cancellationToken)
        {
            var task = show ? transition.ShowAsync(Group, cancellationToken) : transition.HideAsync(Group, cancellationToken);
            await task.SuppressCancellationThrow();
        }

        private void CancelTransition()
        {
            if (_transitionCts == null) return;
            _transitionCts.Cancel();
            _transitionCts.Dispose();
            _transitionCts = null;
        }
    }
}
