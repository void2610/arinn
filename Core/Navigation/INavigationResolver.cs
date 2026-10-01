using UnityEngine.UI;

namespace Void2610.Arinn
{
    /// <summary>
    /// スコープの中で、方向入力から移動先を決める解決器。スコープごとに差し替えられる
    /// （既定は実座標で決める <see cref="SpatialResolver"/>、仮想カーソルは <see cref="CursorResolver"/>）。
    /// </summary>
    public interface INavigationResolver
    {
        /// <summary>
        /// current から direction へ移動した先を返す。選択を動かさない場合（端で止まる・解決器の内部で処理を終えた場合）は null。
        /// 返した要素がスコープ外・操作不可なら <see cref="NavigationController"/> が移動を取り消す。
        /// </summary>
        Selectable Resolve(NavigationScope scope, Selectable current, NavigationDirection direction);
    }

    /// <summary>
    /// 斜めの入力を 1 回で解決できる解決器。解決しなければ <see cref="NavigationController"/> が横、縦の順に 1 歩ずつ動かす。
    /// </summary>
    public interface IDiagonalNavigationResolver
    {
        /// <summary>
        /// current から horizontal と vertical を合わせた斜めへ動かす。この解決器で扱ったら true を返し、選択を移す先を next に入れる（動かさないなら null）。
        /// </summary>
        bool TryResolveDiagonal(NavigationScope scope, Selectable current, NavigationDirection horizontal, NavigationDirection vertical, out Selectable next);
    }
}
