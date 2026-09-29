using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Void2610.Arinn
{
    /// <summary>
    /// InputSystemUIInputModule の move を方向入力として読み、移動を止めるときは module の move Action を無効にする。
    /// 止めている間も読めるよう、move を複製したライブラリ専用の Action から値を読む。
    /// </summary>
    public sealed class InputSystemNavigationInput : INavigationInput, IDisposable
    {
        private const float DEFAULT_REPEAT_DELAY = 0.5f;
        private const float DEFAULT_REPEAT_RATE = 0.1f;

        // 止めている対象。module が参照している Action そのもの（生成コードの Action は複製なので、そちらを止めても効かない）
        private InputSystemUIInputModule _suppressedModule;
        private InputAction _suppressedAction;

        private InputAction _source;
        private InputAction _clone;

        public float RepeatDelay
        {
            get
            {
                var module = CurrentModule;
                return module ? module.moveRepeatDelay : DEFAULT_REPEAT_DELAY;
            }
        }

        public float RepeatRate
        {
            get
            {
                var module = CurrentModule;
                return module ? module.moveRepeatRate : DEFAULT_REPEAT_RATE;
            }
        }

        private static InputSystemUIInputModule CurrentModule =>
            EventSystem.current ? EventSystem.current.currentInputModule as InputSystemUIInputModule : null;

        public Vector2 ReadMove()
        {
            var action = GetMoveAction(CurrentModule);
            if (action == null) return Vector2.zero;

            if (_source != action)
            {
                _clone?.Dispose();
                _clone = action.Clone();
                _clone.Enable();
                _source = action;
            }
            return _clone.ReadValue<Vector2>();
        }

        public void SuppressUnityMove()
        {
            var module = CurrentModule;
            var action = GetMoveAction(module);
            if (action == null) return;

            // module が差し替わった（シーン遷移など）なら、前の module の移動を戻してから止め直す
            if (_suppressedAction != null && _suppressedAction != action) RestoreUnityMove();
            _suppressedModule = module;
            _suppressedAction = action;
            if (action.enabled) action.Disable();
        }

        public void RestoreUnityMove()
        {
            if (_suppressedAction == null) return;
            // module が破棄済みなら Action も既に使われていないので触らない
            if (_suppressedModule && !_suppressedAction.enabled) _suppressedAction.Enable();
            _suppressedModule = null;
            _suppressedAction = null;
        }

        public void Dispose()
        {
            RestoreUnityMove();
            _clone?.Dispose();
            _clone = null;
            _source = null;
        }

        private static InputAction GetMoveAction(InputSystemUIInputModule module)
        {
            if (!module) return null;
            var reference = module.move;
            return reference ? reference.action : null;
        }
    }
}
