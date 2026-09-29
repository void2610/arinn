using System;
using NUnit.Framework;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn.Tests
{
    public sealed class WindowBehaviourTests : ArinnTestFixture
    {
        private readonly CompositeDisposable _disposables = new();

        [TearDown]
        public void TearDownDisposables() => _disposables.Clear();

        [Test]
        public void Openで開いてCloseで閉じる()
        {
            var window = CreateWindow("A");

            window.Open();
            Assert.That(window.IsVisible, Is.True);
            Assert.That(Manager.TopWindow, Is.EqualTo(window));

            window.Close();
            Assert.That(window.IsVisible, Is.False);
            Assert.That(Manager.HasOpenWindows, Is.False);
        }

        [Test]
        public void Openは開く前のフォーカスを預かりCloseで返す()
        {
            var before = Create("Before");
            EventSystem.SetSelectedGameObject(before);
            var window = CreateWindow("A");

            window.Open();
            NextFrame();
            Assert.That(Selected, Is.EqualTo(window.Default));

            window.Close();
            Assert.That(Selected, Is.EqualTo(before));
        }

        [Test]
        public void Toggleは閉じていれば開き開いていれば閉じる()
        {
            var window = CreateWindow("A");

            window.Toggle();
            Assert.That(window.IsVisible, Is.True);

            window.Toggle();
            Assert.That(window.IsVisible, Is.False);
        }

        [Test]
        public void マネージャーが無いときのOpenは例外で知らせる()
        {
            var window = CreateWindow("A");
            Manager.Dispose();

            Assert.Throws<InvalidOperationException>(() => window.Open());
        }

        [Test]
        public void ToggleWindow_閉じていれば開き開いていれば閉じる()
        {
            var window = CreateWindow("A");

            Manager.ToggleWindow(window);
            Assert.That(window.IsVisible, Is.True);

            Manager.ToggleWindow(window);
            Assert.That(window.IsVisible, Is.False);
            Assert.That(Manager.HasOpenWindows, Is.False);
        }

        [Test]
        public void RegisterToggleAction_入力のたびに開閉する()
        {
            var input = new Subject<Unit>();
            var window = CreateWindow("A");
            Manager.RegisterToggleAction(input, window, _disposables);

            input.OnNext(Unit.Default);
            Assert.That(window.IsVisible, Is.True);

            input.OnNext(Unit.Default);
            Assert.That(window.IsVisible, Is.False);
        }

        [Test]
        public void RegisterToggleAction_他のウィンドウが開いている間は開かない()
        {
            var input = new Subject<Unit>();
            var window = CreateWindow("A");
            Manager.RegisterToggleAction(input, window, _disposables);
            Manager.ShowWindow(CreateWindow("Other"));

            input.OnNext(Unit.Default);

            Assert.That(window.IsVisible, Is.False);
        }

        [Test]
        public void RegisterToggleAction_canToggleがfalseの間は何もしない()
        {
            var input = new Subject<Unit>();
            var window = CreateWindow("A");
            var allowed = false;
            Manager.RegisterToggleAction(input, window, _disposables, () => allowed);

            input.OnNext(Unit.Default);
            Assert.That(window.IsVisible, Is.False);

            allowed = true;
            input.OnNext(Unit.Default);
            Assert.That(window.IsVisible, Is.True);
        }

        [Test]
        public void RegisterToggleAction_onOpenを渡すと開くときはonOpenを呼ぶ()
        {
            var input = new Subject<Unit>();
            var window = CreateWindow("A");
            var opened = 0;
            Manager.RegisterToggleAction(input, window, () =>
            {
                opened++;
                Manager.ShowWindow(window);
            }, _disposables);

            input.OnNext(Unit.Default);
            input.OnNext(Unit.Default);

            Assert.That(opened, Is.EqualTo(1));
            Assert.That(window.IsVisible, Is.False);
        }

        [Test]
        public void RegisterToggleAction_shouldCloseがfalseなら開いたまま表示内容だけを切り替える()
        {
            var input = new Subject<Unit>();
            var window = CreateWindow("A");
            var shown = 0;
            var shouldClose = false;
            Manager.RegisterToggleAction(input, window, () => shown++, () => shouldClose, _disposables);

            input.OnNext(Unit.Default);
            Assert.That(window.IsVisible, Is.True);
            Assert.That(shown, Is.EqualTo(1));

            input.OnNext(Unit.Default);
            Assert.That(window.IsVisible, Is.True);
            Assert.That(shown, Is.EqualTo(2));

            shouldClose = true;
            input.OnNext(Unit.Default);
            Assert.That(window.IsVisible, Is.False);
            Assert.That(shown, Is.EqualTo(2));
        }

        [Test]
        public void RegisterToggleAction_購読を破棄したら反応しない()
        {
            var input = new Subject<Unit>();
            var window = CreateWindow("A");
            Manager.RegisterToggleAction(input, window, _disposables);

            _disposables.Clear();
            input.OnNext(Unit.Default);

            Assert.That(window.IsVisible, Is.False);
        }

        [Test]
        public void 閉じるボタンを押すと閉じる()
        {
            var window = CreateWindow("A");
            var close = CreateButton("Close", window.transform, Vector2.zero);
            window.BindCloseButton(close);
            Manager.ShowWindow(window);

            close.onClick.Invoke();

            Assert.That(window.IsVisible, Is.False);
            Assert.That(Manager.HasOpenWindows, Is.False);
        }

        [Test]
        public void 閉じるボタンを付け替えると前のボタンでは閉じない()
        {
            var window = CreateWindow("A");
            var oldButton = CreateButton("Old", window.transform, Vector2.zero);
            var newButton = CreateButton("New", window.transform, new Vector2(200f, 0f));
            window.BindCloseButton(oldButton);
            window.BindCloseButton(newButton);
            Manager.ShowWindow(window);

            oldButton.onClick.Invoke();
            Assert.That(window.IsVisible, Is.True);

            newButton.onClick.Invoke();
            Assert.That(window.IsVisible, Is.False);
        }

        [Test]
        public void 同じ閉じるボタンを二度指定しても一度しか閉じない()
        {
            var window = CreateWindow("A");
            var close = CreateButton("Close", window.transform, Vector2.zero);
            window.BindCloseButton(close);
            window.BindCloseButton(close);
            Manager.ShowWindow(window);
            var closed = 0;
            using var subscription = window.OnWindowClosed.Subscribe(_ => closed++);

            close.onClick.Invoke();

            Assert.That(closed, Is.EqualTo(1));
        }

        [Test]
        public void 閉じるボタンをnullで外すと押しても閉じない()
        {
            var window = CreateWindow("A");
            var close = CreateButton("Close", window.transform, Vector2.zero);
            window.BindCloseButton(close);
            window.BindCloseButton(null);
            Manager.ShowWindow(window);

            close.onClick.Invoke();

            Assert.That(window.IsVisible, Is.True);
        }

        [Test]
        public void ウィンドウごとの遷移はマネージャーの既定より優先される()
        {
            var fallback = new CountingTransition();
            var own = new CountingTransition();
            Manager.DefaultTransition = fallback;
            var window = CreateWindow("A");
            window.TransitionOverride = own;

            Manager.ShowWindow(window);
            Manager.HideWindow(window);

            Assert.That(own.Shown, Is.EqualTo(1));
            Assert.That(own.Hidden, Is.EqualTo(1));
            Assert.That(fallback.Shown + fallback.Hidden, Is.Zero);
        }

        [Test]
        public void 遷移を指定しないウィンドウはマネージャーの既定を使う()
        {
            var fallback = new CountingTransition();
            Manager.DefaultTransition = fallback;
            var window = CreateWindow("A");

            Manager.ShowWindow(window);

            Assert.That(fallback.Shown, Is.EqualTo(1));
        }
    }
}
