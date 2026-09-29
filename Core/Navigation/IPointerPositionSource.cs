using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// ホバーで選択するときに読むポインタの位置（スクリーン座標）。Input System 用の実装は Void2610.Arinn.InputSystem にある。
    /// </summary>
    public interface IPointerPositionSource
    {
        /// <summary>
        /// ポインタの位置を返す。ポインタが無ければ false。
        /// </summary>
        bool TryGetPosition(out Vector2 position);
    }
}
