namespace Void2610.Arinn
{
    /// <summary>
    /// 移動入力の Vector2 を方向へ丸める規則。<see cref="NavigationScope.WithInputMode"/> でスコープごとに決める。
    /// </summary>
    public enum NavigationInputMode
    {
        /// <summary> 絶対値の大きい軸の 1 方向。同じなら水平（既定） </summary>
        FourWay,

        /// <summary> 絶対値の大きい軸の 1 方向。同じなら垂直 </summary>
        FourWayPreferVertical,

        /// <summary> 水平の成分だけを見る（垂直の成分があっても左右として扱う） </summary>
        HorizontalOnly,

        /// <summary> 垂直の成分だけを見る </summary>
        VerticalOnly,

        /// <summary> 水平と垂直を別々に判定する。両方あれば斜めとして、水平、垂直の順に 1 歩ずつ動かす </summary>
        EightWay,
    }
}
