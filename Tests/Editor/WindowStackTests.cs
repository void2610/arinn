using NUnit.Framework;
using R3;

namespace Void2610.Arinn.Tests
{
    public sealed class WindowStackTests : ArinnTestFixture
    {
        [Test]
        public void ShowWindow_開くと表示と入力受付が一緒に切り替わる()
        {
            var window = CreateWindow("A");

            Manager.ShowWindow(window);

            Assert.That(window.IsVisible, Is.True);
            Assert.That(window.Group.alpha, Is.EqualTo(1f));
            Assert.That(window.Group.interactable, Is.True);
            Assert.That(window.Group.blocksRaycasts, Is.True);
            Assert.That(Manager.TopWindow, Is.EqualTo(window));
        }

        [Test]
        public void ShowWindow_同じウィンドウを二度開いても一度閉じればスタックは空になる()
        {
            var window = CreateWindow("A");

            Manager.ShowWindow(window);
            Manager.ShowWindow(window);
            Manager.HideWindow(window);

            Assert.That(Manager.HasOpenWindows, Is.False);
        }

        [Test]
        public void HideWindow_閉じる遷移の完了を待たずに入力を切る()
        {
            Manager.DefaultTransition = new NeverEndingHideTransition();
            var window = CreateWindow("A");
            Manager.ShowWindow(window);

            Manager.HideWindow(window);

            Assert.That(window.Group.alpha, Is.EqualTo(1f), "前提: 見た目はまだ消えていない");
            Assert.That(window.IsVisible, Is.False);
            Assert.That(window.Group.interactable, Is.False);
            Assert.That(window.Group.blocksRaycasts, Is.False);
        }

        [Test]
        public void HideWindow_閉じた通知の中で開き直しても入力を受け付ける()
        {
            var window = CreateWindow("A");
            var reopened = false;
            using var subscription = window.OnWindowClosed.Subscribe(_ =>
            {
                if (reopened) return;
                reopened = true;
                Manager.ShowWindow(window);
            });
            Manager.ShowWindow(window);

            Manager.HideWindow(window);

            Assert.That(window.IsVisible, Is.True);
            Assert.That(window.Group.interactable, Is.True);
            Assert.That(Manager.IsWindowInStack(window), Is.True);
        }

        [Test]
        public void 入力の窓口は最初に開いたときと最後に閉じたときだけ呼ばれる()
        {
            var gate = new CountingGate();
            Manager.SetInputScopeGate(gate);
            var a = CreateWindow("A");
            var b = CreateWindow("B");

            Manager.ShowWindow(a);
            Manager.ShowWindow(b);
            Manager.HideWindow(b);
            Assert.That((gate.Opened, gate.Closed), Is.EqualTo((1, 0)));

            Manager.HideWindow(a);
            Assert.That((gate.Opened, gate.Closed), Is.EqualTo((1, 1)));
        }

        [Test]
        public void CloseAll_閉じた通知の中で別のウィンドウを開いたら入力を戻さない()
        {
            var gate = new CountingGate();
            Manager.SetInputScopeGate(gate);
            var a = CreateWindow("A");
            var b = CreateWindow("B");
            using var subscription = a.OnWindowClosed.Subscribe(_ => Manager.ShowWindow(b));
            Manager.ShowWindow(a);

            Manager.CloseAll();

            Assert.That(Manager.TopWindow, Is.EqualTo(b));
            Assert.That(b.Group.interactable, Is.True);
            Assert.That(gate.Closed, Is.EqualTo(0), "開いているウィンドウがあるのにゲームプレイの入力を戻さない");
        }

        [Test]
        public void TryCloseTopWindow_Cancelで閉じないウィンドウは閉じない()
        {
            var window = CreateWindow("A");
            window.ClosableByCancel = false;
            Manager.ShowWindow(window);

            Assert.That(Manager.TryCloseTopWindow(), Is.False);
            Assert.That(Manager.IsWindowInStack(window), Is.True);
        }

        [Test]
        public void TryPopScope_ウィンドウを先に閉じ次に常時表示UIから戻る()
        {
            var previous = Create("Previous");
            EventSystem.SetSelectedGameObject(previous);
            Manager.EnterPersistentUIFocus(Create("Persistent"));
            var window = CreateWindow("A");
            Manager.ShowWindow(window);

            Assert.That(Manager.TryPopScope(), Is.True);
            Assert.That(Manager.HasOpenWindows, Is.False);
            Assert.That(Manager.IsInPersistentUIMode, Is.True);

            Assert.That(Manager.TryPopScope(), Is.True);
            Assert.That(Manager.IsInPersistentUIMode, Is.False);
            Assert.That(Selected, Is.EqualTo(previous));

            Assert.That(Manager.TryPopScope(), Is.False);
        }

        [Test]
        public void Instance_生成で差し替わりDisposeで外れる()
        {
            Assert.That(UIFocusManager.Instance, Is.EqualTo(Manager));

            Manager.Dispose();

            Assert.That(UIFocusManager.Instance, Is.Null);
        }
    }
}
