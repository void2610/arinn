using UnityEngine;
using UnityEngine.InputSystem;

namespace Void2610.Arinn
{
    /// <summary>
    /// Input System のマウスの位置。タッチはホバーの概念が無いので見ない。
    /// </summary>
    public sealed class InputSystemPointerPosition : IPointerPositionSource
    {
        public static readonly InputSystemPointerPosition Instance = new();

        public bool TryGetPosition(out Vector2 position)
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                position = default;
                return false;
            }
            position = mouse.position.ReadValue();
            return true;
        }
    }
}
