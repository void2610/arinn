using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace Void2610.Arinn.Tests
{
    /// <summary>
    /// Input System に繋ぐ部分のテスト。EventSystem が入力モジュールを選ぶのは Update の中なので PlayMode で動かす。
    /// </summary>
    public sealed class InputSystemIntegrationTests : InputTestFixture
    {
        private readonly List<EventSystem> _disabledEventSystems = new();
        private GameObject _eventSystemObject;
        private InputActionAsset _actions;
        private InputSystemUIInputModule _module;
        private Gamepad _gamepad;
        private Keyboard _keyboard;

        // 基底の SetUp（入力の隔離）の後に走る
        [SetUp]
        public void SetUpScene()
        {
            // 利用側のプロジェクトで先に走ったテストの EventSystem が残っていると、テストで作ったものが EventSystem.current にならない
            foreach (var eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                if (!eventSystem.enabled) continue;
                eventSystem.enabled = false;
                _disabledEventSystems.Add(eventSystem);
            }
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        // 基底の TearDown（入力の復元）の前に走る
        [TearDown]
        public void TearDownScene()
        {
            if (_eventSystemObject) Object.DestroyImmediate(_eventSystemObject);
            if (_actions) Object.DestroyImmediate(_actions);
            foreach (var eventSystem in _disabledEventSystems)
            {
                if (eventSystem) eventSystem.enabled = true;
            }
            _disabledEventSystems.Clear();
        }

        [UnityTest]
        public IEnumerator SuppressUnityMove_moduleのmoveを止めてRestoreUnityMoveで戻す()
        {
            yield return CreateEventSystem();
            using var input = new InputSystemNavigationInput();
            var move = _module.move.action;
            Assert.That(move.enabled, Is.True, "前提: module の move は有効");

            input.SuppressUnityMove();
            Assert.That(move.enabled, Is.False);

            input.RestoreUnityMove();
            Assert.That(move.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator RestoreUnityMove_止めていなければ触らない()
        {
            yield return CreateEventSystem();
            using var input = new InputSystemNavigationInput();
            var move = _module.move.action;
            move.Disable();

            input.RestoreUnityMove();

            Assert.That(move.enabled, Is.False, "他の処理が止めた move を勝手に有効にしない");
        }

        [UnityTest]
        public IEnumerator ReadMove_moveを止めている間も読める()
        {
            yield return CreateEventSystem();
            using var input = new InputSystemNavigationInput();
            input.ReadMove();
            input.SuppressUnityMove();

            Press(_gamepad.dpad.right);
            yield return null;
            Assert.That(input.ReadMove().x, Is.GreaterThan(0.5f));

            Release(_gamepad.dpad.right);
            yield return null;
            Assert.That(input.ReadMove(), Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator Dispose_止めていたmoveを戻す()
        {
            yield return CreateEventSystem();
            var input = new InputSystemNavigationInput();
            input.SuppressUnityMove();

            input.Dispose();

            Assert.That(_module.move.action.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator RepeatDelayとRepeatRateはmoduleの設定に従う()
        {
            yield return CreateEventSystem();
            _module.moveRepeatDelay = 0.3f;
            _module.moveRepeatRate = 0.05f;
            using var input = new InputSystemNavigationInput();

            Assert.That(input.RepeatDelay, Is.EqualTo(0.3f));
            Assert.That(input.RepeatRate, Is.EqualTo(0.05f));
        }

        [Test]
        public void EventSystemが無ければ何も読まず止めもしない()
        {
            using var input = new InputSystemNavigationInput();

            input.SuppressUnityMove();

            Assert.That(input.ReadMove(), Is.EqualTo(Vector2.zero));
            Assert.That(input.RepeatDelay, Is.EqualTo(0.5f));
        }

        [Test]
        public void SubmitHoldProbe_ゲームパッドのSouthを押している間はtrue()
        {
            var probe = new InputSystemSubmitHoldProbe();

            Press(_gamepad.buttonSouth);
            Assert.That(probe.IsSubmitHeld, Is.True);

            Release(_gamepad.buttonSouth);
            Assert.That(probe.IsSubmitHeld, Is.False);
        }

        [Test]
        public void SubmitHoldProbe_キーボードのSpaceとEnterを見る()
        {
            var probe = new InputSystemSubmitHoldProbe();

            Press(_keyboard.spaceKey);
            Assert.That(probe.IsSubmitHeld, Is.True);
            Release(_keyboard.spaceKey);

            Press(_keyboard.enterKey);
            Assert.That(probe.IsSubmitHeld, Is.True);
            Release(_keyboard.enterKey);
            Assert.That(probe.IsSubmitHeld, Is.False);
        }

        [Test]
        public void SubmitHoldProbe_Actionを渡すとそのActionの押下を見る()
        {
            var action = new InputAction(binding: "<Gamepad>/buttonEast");
            action.Enable();
            var probe = new InputSystemSubmitHoldProbe(action);

            Press(_gamepad.buttonSouth);
            Assert.That(probe.IsSubmitHeld, Is.False, "既定の South は見ない");

            Press(_gamepad.buttonEast);
            Assert.That(probe.IsSubmitHeld, Is.True);
            action.Dispose();
        }

        [Test]
        public void PointerPosition_マウスの位置を返す()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            Set(mouse.position, new Vector2(120f, 80f));

            Assert.That(InputSystemPointerPosition.Instance.TryGetPosition(out var position), Is.True);
            Assert.That(position, Is.EqualTo(new Vector2(120f, 80f)));
        }

        private IEnumerator CreateEventSystem()
        {
            _eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            _module = _eventSystemObject.AddComponent<InputSystemUIInputModule>();
            // 既定のアクションは利用側のプロジェクト全体のアクションに置き換わることがあるので、テスト専用の Navigate を割り当てる
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var navigate = _actions.AddActionMap("UI").AddAction("Navigate", InputActionType.PassThrough, "<Gamepad>/dpad", expectedControlLayout: "Vector2");
            _module.actionsAsset = _actions;
            _module.move = InputActionReference.Create(navigate);
            _actions.Enable();
            // EventSystem が Update で module を選ぶのを待つ
            yield return null;
            Assert.That(EventSystem.current.currentInputModule, Is.EqualTo(_module));
        }
    }
}
