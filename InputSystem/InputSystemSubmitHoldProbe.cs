using UnityEngine.InputSystem;

namespace Void2610.Arinn
{
    /// <summary>
    /// Input System で決定入力の押下を調べる。Action を渡せばその押下を、渡さなければゲームパッドの South とキーボードの Space / Enter を見る。
    /// </summary>
    public sealed class InputSystemSubmitHoldProbe : ISubmitHoldProbe
    {
        private readonly InputAction _submitAction;

        public InputSystemSubmitHoldProbe(InputAction submitAction = null)
        {
            _submitAction = submitAction;
        }

        public bool IsSubmitHeld
        {
            get
            {
                if (_submitAction != null) return _submitAction.IsPressed();
                var keyboard = Keyboard.current;
                return Gamepad.current?.buttonSouth.isPressed == true
                    || (keyboard != null && (keyboard.spaceKey.isPressed || keyboard.enterKey.isPressed));
            }
        }
    }
}
