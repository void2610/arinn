using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public void フォーカスを自動で戻している間だけIsRecoveringFocusがtrueになる()
        {
            var target = Create("Target");
            var recorder = target.AddComponent<SelectRecorder>();
            recorder.Manager = Manager;
            EventSystem.SetSelectedGameObject(target);
            Assert.That(recorder.RecoveringOnSelect, Is.EqualTo(new[] { false }), "操作による選択では false");
            NextFrame();

            EventSystem.SetSelectedGameObject(null);
            NextFrame();

            Assert.That(recorder.RecoveringOnSelect, Is.EqualTo(new[] { false, true }), "自動で戻した選択では true");
            Assert.That(Manager.IsRecoveringFocus, Is.False, "戻し終えたら false");
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

        [Test]
        public void HideWindow_下のウィンドウを閉じても最前面のフォーカスは動かない()
        {
            var a = CreateWindow("A");
            Manager.ShowWindow(a);
            NextFrame();
            var b = CreateWindow("B");
            Manager.ShowWindow(b);
            NextFrame();

            Manager.HideWindow(a);

            Assert.That(Selected, Is.EqualTo(b.Default));
            Assert.That(Manager.TopWindow, Is.EqualTo(b));
        }

        [Test]
        public void HideWindow_上を閉じたとき戻り先が消えていれば新しい最前面の既定要素へ戻す()
        {
            var a = CreateWindow("A");
            Manager.ShowWindow(a);
            NextFrame();
            var inner = Create("A/Inner");
            EventSystem.SetSelectedGameObject(inner);
            var b = CreateWindow("B");
            Manager.ShowWindow(b);
            NextFrame();
            inner.SetActive(false);

            Manager.HideWindow(b);

            Assert.That(Selected, Is.EqualTo(a.Default));
        }

        [Test]
        public void 非アクティブな既定要素は上限のフレーム数を過ぎたら待つのをあきらめる()
        {
            var next = CreateBase("Next");
            next.Default.SetActive(false);
            Manager.SwitchBase(next);

            for (var i = 0; i < 121; i++) NextFrame();
            next.Default.SetActive(true);
            NextFrame();

            Assert.That(Selected, Is.Not.EqualTo(next.Default));
        }

        [Test]
        public void 非アクティブな既定要素は上限のフレーム数の内ならアクティブになった時点でフォーカスする()
        {
            var next = CreateBase("Next");
            next.Default.SetActive(false);
            Manager.SwitchBase(next);

            for (var i = 0; i < 119; i++) NextFrame();
            next.Default.SetActive(true);
            NextFrame();

            Assert.That(Selected, Is.EqualTo(next.Default));
        }

        [Test]
        public void シーンが切り替わるとスタックと基底画面と常時表示UIの状態を捨てる()
        {
            var gate = new CountingGate();
            Manager.SetInputScopeGate(gate);
            Manager.SetBaseFocusSource(CreateBase("Base"));
            Manager.EnterPersistentUIFocus(Create("Persistent"));
            Manager.ShowWindow(CreateWindow("A"));

            typeof(UIFocusManager)
                .GetMethod("OnActiveSceneChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Manager, new object[] { default(Scene), default(Scene) });

            Assert.That(Manager.HasOpenWindows, Is.False);
            Assert.That(Manager.BaseFocusSource, Is.Null);
            Assert.That(Manager.IsInPersistentUIMode, Is.False);

            // 入力の窓口も捨てているので、次に開閉しても前のシーンの窓口は呼ばれない
            var window = CreateWindow("B");
            Manager.ShowWindow(window);
            Manager.HideWindow(window);
            Assert.That(gate.Opened, Is.EqualTo(1));
            Assert.That(gate.Closed, Is.Zero);
        }

        [Test]
        public void CloseAll_すべて閉じて基底画面へ戻し入力の窓口の閉じる側を1回呼ぶ()
        {
            var gate = new CountingGate();
            Manager.SetInputScopeGate(gate);
            var baseScreen = CreateBase("Base");
            Manager.SetBaseFocusSource(baseScreen);
            var a = CreateWindow("A");
            var b = CreateWindow("B");
            Manager.ShowWindow(a);
            Manager.ShowWindow(b);
            NextFrame();

            Manager.CloseAll();

            Assert.That(Manager.HasOpenWindows, Is.False);
            Assert.That(a.IsVisible, Is.False);
            Assert.That(b.IsVisible, Is.False);
            Assert.That(Selected, Is.EqualTo(baseScreen.Default));
            Assert.That(gate.Closed, Is.EqualTo(1));
        }

        [Test]
        public void WindowCount_開いているウィンドウの数を返す()
        {
            Manager.ShowWindow(CreateWindow("A"));
            var b = CreateWindow("B");
            Manager.ShowWindow(b);
            Assert.That(Manager.WindowCount, Is.EqualTo(2));

            Manager.HideWindow(b);
            Assert.That(Manager.WindowCount, Is.EqualTo(1));
        }

        [Test]
        public void IsFocusOnDefaultElement_最前面のウィンドウの既定要素にあるときだけtrue()
        {
            var window = CreateWindow("A");
            Manager.ShowWindow(window);
            NextFrame();
            Assert.That(Manager.IsFocusOnDefaultElement, Is.True);

            EventSystem.SetSelectedGameObject(Create("Other"));
            Assert.That(Manager.IsFocusOnDefaultElement, Is.False);
        }

        [Test]
        public void IsFocusOnDefaultElement_ウィンドウが無ければ基底画面の既定要素で判定する()
        {
            var baseScreen = CreateBase("Base");
            Manager.SwitchBase(baseScreen);

            Assert.That(Manager.IsFocusOnDefaultElement, Is.True);
        }

        [Test]
        public void IsFocusOnDefaultElement_何も選択していなければfalse()
        {
            Manager.SetBaseFocusSource(CreateBase("Base"));

            Assert.That(Manager.IsFocusOnDefaultElement, Is.False);
        }

        [Test]
        public void 既定要素の取得が例外を投げてもフォーカスを動かさずに進む()
        {
            var before = Create("Before");
            EventSystem.SetSelectedGameObject(before);
            var window = CreateWindow("A");
            window.ThrowsOnDefault = true;
            Manager.ShowWindow(window);

            Assert.DoesNotThrow(NextFrame);
            Assert.DoesNotThrow(NextFrame);
            Assert.That(Selected, Is.EqualTo(before));
        }

        [Test]
        public void 閉じる途中で既定要素の取得が例外を投げてもウィンドウは閉じきる()
        {
            var a = CreateWindow("A");
            Manager.ShowWindow(a);
            NextFrame();
            var inner = Create("A/Inner");
            EventSystem.SetSelectedGameObject(inner);
            var b = CreateWindow("B");
            Manager.ShowWindow(b);
            NextFrame();
            inner.SetActive(false);
            a.ThrowsOnDefault = true;

            Assert.DoesNotThrow(() => Manager.HideWindow(b));

            Assert.That(b.IsVisible, Is.False);
            Assert.That(Manager.TopWindow, Is.EqualTo(a));
        }

        [Test]
        public void 押せないだけの要素にも閉じたあとでフォーカスを戻す()
        {
            var disabled = CreateButton("Disabled", CreateRoot("Root"), Vector2.zero);
            disabled.interactable = false;
            EventSystem.SetSelectedGameObject(disabled.gameObject);
            var window = CreateWindow("A");
            Manager.ShowWindow(window);
            NextFrame();

            Manager.HideWindow(window);

            Assert.That(Selected, Is.EqualTo(disabled.gameObject));
        }

        [Test]
        public void 常時表示UIから戻るとき借りる前の要素が消えていれば既定要素へ戻す()
        {
            var baseScreen = CreateBase("Base");
            Manager.SetBaseFocusSource(baseScreen);
            var before = Create("Before");
            EventSystem.SetSelectedGameObject(before);
            Manager.EnterPersistentUIFocus(Create("Persistent"));
            NextFrame();
            before.SetActive(false);

            Manager.ExitPersistentUIFocus();

            Assert.That(Selected, Is.EqualTo(baseScreen.Default));
        }

        [Test]
        public void SwitchBaseで常時表示UIのフォーカスを借りている状態も捨てる()
        {
            Manager.EnterPersistentUIFocus(Create("Persistent"));
            NextFrame();

            Manager.SwitchBase(CreateBase("Next"));

            Assert.That(Manager.IsInPersistentUIMode, Is.False);
        }

        [Test]
        public void 常時表示UIへの予約はウィンドウを開いても消えない()
        {
            var persistent = Create("Persistent");
            Manager.EnterPersistentUIFocus(persistent);
            Manager.ShowWindow(CreateWindow("A"));

            NextFrame();

            Assert.That(Selected, Is.EqualTo(persistent));
        }

        [Test]
        public void 常時表示UIの行き先が無ければ選択を外す()
        {
            EventSystem.SetSelectedGameObject(Create("Before"));

            Manager.EnterPersistentUIFocus(null, Create("Owner"));
            NextFrame();

            Assert.That(Selected, Is.Null);
            Assert.That(Manager.IsInPersistentUIMode, Is.True);
        }

        [Test]
        public void TogglePersistentUIFocus_同じownerでもう一度呼ぶと借りる前のフォーカスへ戻る()
        {
            var before = Create("Before");
            EventSystem.SetSelectedGameObject(before);
            var owner = Create("Owner");
            var persistent = Create("Persistent");

            Manager.TogglePersistentUIFocus(persistent, owner);
            NextFrame();
            Assert.That(Selected, Is.EqualTo(persistent));

            Manager.TogglePersistentUIFocus(persistent, owner);

            Assert.That(Selected, Is.EqualTo(before));
            Assert.That(Manager.IsInPersistentUIMode, Is.False);
        }

        [Test]
        public void TogglePersistentUIFocus_別のownerなら借りる前のフォーカスを保ったまま借り直す()
        {
            var before = Create("Before");
            EventSystem.SetSelectedGameObject(before);
            var firstOwner = Create("FirstOwner");
            var secondOwner = Create("SecondOwner");
            var first = Create("First");
            var second = Create("Second");

            Manager.TogglePersistentUIFocus(first, firstOwner);
            NextFrame();
            Manager.TogglePersistentUIFocus(second, secondOwner);
            Assert.That(Selected, Is.EqualTo(second));
            Assert.That(Manager.IsInPersistentUIMode, Is.True);

            Manager.TogglePersistentUIFocus(second, secondOwner);

            Assert.That(Selected, Is.EqualTo(before), "借り直しても、戻り先は最初に借りる前の要素");
        }

        [Test]
        public void 常時表示UIを借りている間はフォーカスが消えても戻さない()
        {
            EventSystem.SetSelectedGameObject(Create("Before"));
            Manager.EnterPersistentUIFocus(Create("Persistent"));
            NextFrame();

            EventSystem.SetSelectedGameObject(null);
            AdvanceSeconds(0f);
            AdvanceSeconds(1f);

            Assert.That(Selected, Is.Null);
        }

        [Test]
        public void フォーカスが消えて待っている間に直前の要素がアクティブに戻ればすぐ戻す()
        {
            var baseScreen = CreateBase("Base");
            Manager.SetBaseFocusSource(baseScreen);
            var target = Create("Target");
            EventSystem.SetSelectedGameObject(target);
            NextFrame();
            target.SetActive(false);
            EventSystem.SetSelectedGameObject(null);
            AdvanceSeconds(0f);
            AdvanceSeconds(0.2f);
            Assert.That(Selected, Is.Null, "待ち時間の途中");

            target.SetActive(true);
            NextFrame();

            Assert.That(Selected, Is.EqualTo(target), "既定要素ではなく直前の要素へ戻す");
        }

        private sealed class SelectRecorder : MonoBehaviour, UnityEngine.EventSystems.ISelectHandler
        {
            public UIFocusManager Manager;
            public readonly System.Collections.Generic.List<bool> RecoveringOnSelect = new();

            public void OnSelect(UnityEngine.EventSystems.BaseEventData eventData) => RecoveringOnSelect.Add(Manager.IsRecoveringFocus);
        }
    }
}
