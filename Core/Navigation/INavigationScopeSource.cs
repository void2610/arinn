namespace Void2610.Arinn
{
    /// <summary>
    /// 自分のナビゲーションのスコープを宣言する画面。<see cref="WindowBase"/> は既定で実装している。
    /// 基底画面（<see cref="IFocusSource"/>）も実装すれば、<see cref="NavigationController.Register"/> を呼ばずに済む。
    /// </summary>
    public interface INavigationScopeSource
    {
        /// <summary>
        /// この画面のスコープ。最初に今の画面になったときに一度だけ呼ばれ、以後は同じスコープを使う。null ならスコープを持たない（Unity の移動に任せる）。
        /// </summary>
        NavigationScope CreateNavigationScope();
    }
}
