using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Void2610.Arinn.Tests
{
    /// <summary>
    /// EventSystem・時計・マネージャーを用意し、フレームを手で進めるテストの土台。
    /// </summary>
    public abstract class ArinnTestFixture
    {
        protected UIFocusManager Manager { get; private set; }
        protected EventSystem EventSystem { get; private set; }
        protected GameObject Selected => EventSystem.currentSelectedGameObject;

        private readonly List<GameObject> _created = new();
        private FakeClock _clock;
        private NavigationController _navigation;

        [SetUp]
        public void SetUpFixture()
        {
            var eventSystemObject = Create("EventSystem");
            EventSystem = eventSystemObject.AddComponent<EventSystem>();
            // EditMode では OnEnable が走らず EventSystem.current に登録されないため、明示的に呼ぶ
            InvokeEventSystem("OnEnable");
            _clock = new FakeClock();
            Manager = new UIFocusManager { Clock = _clock };
        }

        [TearDown]
        public void TearDownFixture()
        {
            _navigation?.Dispose();
            _navigation = null;
            Manager.Dispose();
            InvokeEventSystem("OnDisable");
            foreach (var go in _created)
                if (go) Object.DestroyImmediate(go);
            _created.Clear();
        }

        /// <summary>
        /// 1 フレーム進めて Tick する。
        /// </summary>
        protected void NextFrame()
        {
            _clock.FrameCount++;
            Manager.Tick();
        }

        /// <summary>
        /// 同じフレームのまま時間だけ進めて Tick する。
        /// </summary>
        protected void AdvanceSeconds(float seconds)
        {
            _clock.UnscaledTime += seconds;
            _clock.FrameCount++;
            Manager.Tick();
        }

        /// <summary>
        /// マネージャーと同じ時計で動くナビゲーションを作る。テストの終わりに破棄される。
        /// </summary>
        protected NavigationController CreateNavigation(INavigationInput input = null)
        {
            _navigation = new NavigationController(Manager) { Clock = _clock };
            if (input != null) _navigation.SetInput(input);
            return _navigation;
        }

        /// <summary>
        /// 1 フレーム進めて、マネージャーとナビゲーションを Tick する。
        /// </summary>
        protected void NextNavigationFrame(float seconds = 0f)
        {
            _clock.UnscaledTime += seconds;
            _clock.FrameCount++;
            Manager.Tick();
            _navigation?.Tick();
        }

        /// <summary>
        /// 親の下に、指定した位置と大きさのボタンを作る。
        /// </summary>
        protected Button CreateButton(string name, Transform parent, Vector2 position, Vector2? size = null)
        {
            var go = Create(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size ?? new Vector2(100f, 50f);
            return go.AddComponent<Button>();
        }

        /// <summary>
        /// RectTransform を持つ空の根を作る。
        /// </summary>
        protected RectTransform CreateRoot(string name)
        {
            var go = Create(name);
            return go.AddComponent<RectTransform>();
        }

        /// <summary>
        /// 親の下に RectTransform を持つ空のオブジェクトを作る（fixture の外の補助から使う）。
        /// </summary>
        internal RectTransform CreateRootFor(string name, Transform parent)
        {
            var rect = CreateRoot(name);
            rect.SetParent(parent, false);
            return rect;
        }

        internal Button CreateButtonFor(string name, Transform parent, Vector2 position) => CreateButton(name, parent, position);

        protected GameObject Create(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }

        protected TestWindow CreateWindow(string name)
        {
            var window = Create(name).AddComponent<TestWindow>();
            window.Default = Create(name + "/Default");
            return window;
        }

        protected TestFocusSource CreateBase(string name) => new() { Default = Create(name + "/Default") };

        private void InvokeEventSystem(string method) =>
            typeof(EventSystem).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(EventSystem, null);

        private sealed class FakeClock : IFrameClock
        {
            public int FrameCount { get; set; }

            public float UnscaledTime { get; set; }
        }
    }

    public sealed class TestWindow : WindowBase
    {
        public GameObject Default;
        public bool ClosableByCancel = true;
        public IWindowTransition TransitionOverride;

        public override GameObject DefaultFocusElement => Default;

        public override bool IsClosableByCancelInput => ClosableByCancel;

        protected override IWindowTransition Transition => TransitionOverride;

        public System.Func<TestWindow, NavigationScope> ScopeFactory;
        public int ScopeCreatedCount;

        public override NavigationScope CreateNavigationScope()
        {
            ScopeCreatedCount++;
            return ScopeFactory != null ? ScopeFactory(this) : base.CreateNavigationScope();
        }

        public void BindCloseButton(Button button) => SetCloseButton(button);

        public CanvasGroup Group => GetComponent<CanvasGroup>();
    }

    public sealed class TestFocusSource : IFocusSource
    {
        public GameObject Default;

        public GameObject DefaultFocusElement => Default;
    }

    /// <summary>
    /// スコープを自分で宣言する基底画面。
    /// </summary>
    public sealed class TestScopedFocusSource : IFocusSource, INavigationScopeSource
    {
        public GameObject Default;
        public System.Func<NavigationScope> ScopeFactory;

        public GameObject DefaultFocusElement => Default;

        public NavigationScope CreateNavigationScope() => ScopeFactory?.Invoke();
    }

    public sealed class CountingGate : IInputScopeGate
    {
        public int Opened;
        public int Closed;

        public void OnFirstWindowOpened() => Opened++;

        public void OnLastWindowClosed() => Closed++;
    }

    public sealed class FakeNavigationInput : INavigationInput
    {
        public Vector2 Move;
        public int SuppressCount;
        public int RestoreCount;
        public bool IsSuppressed;

        public float RepeatDelay { get; set; } = 0.5f;

        public float RepeatRate { get; set; } = 0.1f;

        public Vector2 ReadMove() => Move;

        public void SuppressUnityMove()
        {
            SuppressCount++;
            IsSuppressed = true;
        }

        public void RestoreUnityMove()
        {
            if (!IsSuppressed) return;
            RestoreCount++;
            IsSuppressed = false;
        }
    }

    /// <summary>
    /// 破棄されたかを記録する入力。
    /// </summary>
    public sealed class DisposableNavigationInput : INavigationInput, System.IDisposable
    {
        public bool IsSuppressed;
        public bool IsDisposed;

        public float RepeatDelay => 0.5f;

        public float RepeatRate => 0.1f;

        public Vector2 ReadMove() => Vector2.zero;

        public void SuppressUnityMove() => IsSuppressed = true;

        public void RestoreUnityMove() => IsSuppressed = false;

        public void Dispose() => IsDisposed = true;
    }

    public sealed class FakePointer : IPointerPositionSource
    {
        public Vector2? Position = Vector2.zero;

        public bool TryGetPosition(out Vector2 position)
        {
            position = Position ?? default;
            return Position.HasValue;
        }
    }

    /// <summary>
    /// 開閉の回数を数える遷移。
    /// </summary>
    public sealed class CountingTransition : IWindowTransition
    {
        public int Shown;
        public int Hidden;

        public UniTask ShowAsync(CanvasGroup canvasGroup, CancellationToken cancellationToken)
        {
            Shown++;
            return UniTask.CompletedTask;
        }

        public UniTask HideAsync(CanvasGroup canvasGroup, CancellationToken cancellationToken)
        {
            Hidden++;
            return UniTask.CompletedTask;
        }
    }

    public sealed class FakeSubmitProbe : ISubmitHoldProbe
    {
        public bool IsSubmitHeld { get; set; }
    }

    /// <summary>
    /// 閉じる遷移が終わらない（フェードの途中を再現する）遷移。
    /// </summary>
    public sealed class NeverEndingHideTransition : IWindowTransition
    {
        public UniTask ShowAsync(CanvasGroup canvasGroup, CancellationToken cancellationToken)
        {
            canvasGroup.alpha = 1f;
            return UniTask.CompletedTask;
        }

        public UniTask HideAsync(CanvasGroup canvasGroup, CancellationToken cancellationToken) => UniTask.Never(cancellationToken);
    }
}
