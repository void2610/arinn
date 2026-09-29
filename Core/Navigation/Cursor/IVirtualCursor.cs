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
}
