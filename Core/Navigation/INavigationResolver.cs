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
}
