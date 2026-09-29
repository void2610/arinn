using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// インターフェイス越しに持った UnityEngine.Object の生死判定。
    /// </summary>
    internal static class UnityObjects
    {
        /// <summary>
        /// 生きているか。破棄済みの UnityEngine.Object は通常の null 判定をすり抜けるため、UnityEngine.Object としても判定する。
        /// </summary>
        public static bool IsAlive(object target) => target is Object obj ? obj : target != null;

        /// <summary>
        /// 画面の既定要素。画面が破棄済み、または既定要素が無ければ null。
        /// </summary>
        public static GameObject GetDefaultFocusElement(IFocusSource source)
        {
            if (!IsAlive(source)) return null;
            var element = source.DefaultFocusElement;
            return element ? element : null;
        }
    }
}
