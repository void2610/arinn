namespace Void2610.Arinn
{
    /// <summary>
    /// Selectable で表せない対象（タイルのマス・手札・スロットなど）の上を動くカーソル。
    /// <see cref="CursorResolver"/> に渡すと、EventSystem のフォーカスはライブラリ内部のアンカーが受け、
    /// 方向入力はカーソルへ、決定（Submit）はアンカー経由でカーソルへ届く。Cancel は通常の経路のまま。
    /// </summary>
    public interface IVirtualCursor
    {
        /// <summary>
        /// direction へ 1 つ動かす。動いたら true。端で動けなければ false（WrapRow の回り込みはこの中で行う）。
        /// </summary>
        bool TryMove(NavigationDirection direction);

        /// <summary>
        /// 端で動けなかったときの挙動。<see cref="EdgePolicyKind.Exit"/> ならカーソルを抜けて宣言した要素へ移る。
        /// </summary>
        EdgePolicy GetEdge(NavigationDirection direction);

        /// <summary>
        /// アンカーが決定を受けた。
        /// </summary>
        void Submit();

        /// <summary>
        /// アンカーが選択された（true）・選択が外れた（false）。カーソルの表示の出し入れに使う。
        /// </summary>
        void SetFocused(bool focused);
    }

    /// <summary>
    /// 斜め先のマスへ直接動ける仮想カーソル。<see cref="NavigationInputMode.EightWay"/> の斜めの入力で、横と縦に 1 歩ずつ動かす代わりに使われる。
    /// </summary>
    public interface IDiagonalCursor
    {
        /// <summary>
        /// horizontal と vertical を合わせた斜め先へ 1 つ動かす。斜め先が範囲外か止まれないマスなら動かず false。
        /// </summary>
        bool TryMoveDiagonal(NavigationDirection horizontal, NavigationDirection vertical);
    }
}
