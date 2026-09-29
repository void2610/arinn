using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// 既定でフォーカスする要素を返す画面。
    /// ウィンドウ（<see cref="WindowBase"/>）と、ウィンドウの下にある基底画面の両方が実装する。
    /// </summary>
    public interface IFocusSource
    {
        /// <summary>
        /// フォーカスを置く要素。フォーカスする時点で評価されるため、動的に生成した要素も返せる。
        /// </summary>
        GameObject DefaultFocusElement { get; }
    }
}
