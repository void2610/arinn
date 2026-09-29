using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

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

        public override GameObject DefaultFocusElement => Default;

        public override bool IsClosableByCancelInput => ClosableByCancel;

        public CanvasGroup Group => GetComponent<CanvasGroup>();
    }

    public sealed class TestFocusSource : IFocusSource
    {
        public GameObject Default;

        public GameObject DefaultFocusElement => Default;
    }

    public sealed class CountingGate : IInputScopeGate
    {
        public int Opened;
        public int Closed;

        public void OnFirstWindowOpened() => Opened++;

        public void OnLastWindowClosed() => Closed++;
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
