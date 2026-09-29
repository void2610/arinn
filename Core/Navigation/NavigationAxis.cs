using System;

namespace Void2610.Arinn
{
    /// <summary>
    /// 方向入力の軸。
    /// </summary>
    [Flags]
    public enum NavigationAxis
    {
        /// <summary> どちらでもない </summary>
        None = 0,

        /// <summary> 左右 </summary>
        Horizontal = 1,

        /// <summary> 上下 </summary>
        Vertical = 2,

        /// <summary> 左右と上下 </summary>
        Both = Horizontal | Vertical,
    }
}
