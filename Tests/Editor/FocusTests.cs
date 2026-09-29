using NUnit.Framework;
using UnityEngine;

namespace Void2610.Arinn.Tests
{
    public sealed class FocusTests : ArinnTestFixture
    {
        [Test]
        public void ShowWindow_既定要素へのフォーカスは次のフレームで当たる()
        {
            var outside = Create("Outside");
            EventSystem.SetSelectedGameObject(outside);
            var window = CreateWindow("A");

            Manager.ShowWindow(window);
            Manager.Tick();
            Assert.That(Selected, Is.EqualTo(outside), "開いたフレームではまだ動かさない");

            NextFrame();
            Assert.That(Selected, Is.EqualTo(window.Default));
        }

        [Test]
        public void HideWindow_上のウィンドウを閉じると開く前のフォーカスへ戻る()
        {
            var a = CreateWindow("A");
            Manager.ShowWindow(a);
            NextFrame();
            var b = CreateWindow("B");
            Manager.ShowWindow(b);
            NextFrame();

            Manager.HideWindow(b);

            Assert.That(Selected, Is.EqualTo(a.Default));
        }

        [Test]
        public void HideWindow_最後のウィンドウを閉じると基底画面の既定要素へ戻る()
        {
            var baseScreen = CreateBase("Base");
            Manager.SetBaseFocusSource(baseScreen);
            EventSystem.SetSelectedGameObject(Create("Other"));
            var window = CreateWindow("A");
            Manager.ShowWindow(window);
            NextFrame();

            Manager.HideWindow(window);

            Assert.That(Selected, Is.EqualTo(baseScreen.Default));
        }

        [Test]
        public void HideWindow_基底画面の既定要素が非アクティブならアクティブになってから戻す()
        {
            var baseScreen = CreateBase("Base");
            baseScreen.Default.SetActive(false);
            Manager.SetBaseFocusSource(baseScreen);
            var window = CreateWindow("A");
            Manager.ShowWindow(window);
            NextFrame();

            Manager.HideWindow(window);
            NextFrame();
            Assert.That(Selected, Is.Not.EqualTo(baseScreen.Default));

            baseScreen.Default.SetActive(true);
            NextFrame();
            Assert.That(Selected, Is.EqualTo(baseScreen.Default));
        }

        [Test]
        public void SwitchBase_開いた直後のフォーカス予約を捨てて新しい基底画面へフォーカスする()
        {
            var window = CreateWindow("A");
            Manager.ShowWindow(window);
            var next = CreateBase("Next");

            Manager.SwitchBase(next);
            NextFrame();

            Assert.That(Manager.HasOpenWindows, Is.False);
            Assert.That(Selected, Is.EqualTo(next.Default));
        }

        [Test]
        public void EnterPersistentUIFocus_決定が押されている間はフォーカスを移さない()
        {
            var probe = new FakeSubmitProbe { IsSubmitHeld = true };
            Manager.SetSubmitHoldProbe(probe);
            var previous = Create("Previous");
            EventSystem.SetSelectedGameObject(previous);
            var persistent = Create("Persistent");

            Manager.EnterPersistentUIFocus(persistent);
            NextFrame();
            NextFrame();
            Assert.That(Selected, Is.EqualTo(previous));

            probe.IsSubmitHeld = false;
            NextFrame();
            Assert.That(Selected, Is.EqualTo(persistent));
        }

        [Test]
        public void ExitPersistentUIFocus_移る前に抜けたら予約を捨てて元のフォーカスのまま()
        {
            var previous = Create("Previous");
            EventSystem.SetSelectedGameObject(previous);

            Manager.EnterPersistentUIFocus(Create("Persistent"));
            Manager.ExitPersistentUIFocus();
            NextFrame();

            Assert.That(Selected, Is.EqualTo(previous));
        }

        [Test]
        public void フォーカスが消えたとき直前の要素が生きていればすぐ戻す()
        {
            var target = Create("Target");
            EventSystem.SetSelectedGameObject(target);
            NextFrame();

            EventSystem.SetSelectedGameObject(null);
            NextFrame();

            Assert.That(Selected, Is.EqualTo(target));
        }

        [Test]
        public void フォーカスが消えたとき直前の要素も消えていれば待ってから最前面のウィンドウへ戻す()
        {
            var window = CreateWindow("A");
            Manager.ShowWindow(window);
            NextFrame();
            var target = Create("Target");
            EventSystem.SetSelectedGameObject(target);
            NextFrame();
            target.SetActive(false);

            EventSystem.SetSelectedGameObject(null);
            AdvanceSeconds(0f);
            AdvanceSeconds(0.4f);
            Assert.That(Selected, Is.Null, "待ち時間の間は戻さない");

            AdvanceSeconds(0.2f);
            Assert.That(Selected, Is.EqualTo(window.Default));
        }

        [Test]
        public void フォーカスが消えたときウィンドウがなければ基底画面へ戻す()
        {
            var baseScreen = CreateBase("Base");
            Manager.SetBaseFocusSource(baseScreen);
            var target = Create("Target");
            EventSystem.SetSelectedGameObject(target);
            NextFrame();
            Object.DestroyImmediate(target);

            EventSystem.SetSelectedGameObject(null);
            AdvanceSeconds(0f);
            AdvanceSeconds(0.6f);

            Assert.That(Selected, Is.EqualTo(baseScreen.Default));
        }
    }
}
