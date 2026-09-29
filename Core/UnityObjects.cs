using System;
using System.Collections.Generic;
using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// インターフェイス越しに持った UnityEngine.Object の生死判定。
    /// </summary>
    internal static class UnityObjects
    {
        private static readonly List<CanvasGroup> CanvasGroupBuffer = new();

        /// <summary>
        /// 生きているか。破棄済みの UnityEngine.Object は通常の null 判定をすり抜けるため、UnityEngine.Object としても判定する。
        /// </summary>
        public static bool IsAlive(object target) => target is UnityEngine.Object obj ? obj : target != null;

        /// <summary>
        /// 画面の既定要素。画面が破棄済み、既定要素が無い、または取得が例外を投げたなら null。
        /// ウィンドウを閉じる途中などで例外を外へ出すと、閉じる処理が途中で止まってウィンドウが半端に残るため、ここで止める。
        /// </summary>
        public static GameObject GetDefaultFocusElement(IFocusSource source)
        {
            if (!IsAlive(source)) return null;
            GameObject element;
            try
            {
                element = source.DefaultFocusElement;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[arinn] {source.GetType().Name}.DefaultFocusElement の取得に失敗したので、フォーカスを動かさない: {e.Message}");
                return null;
            }
            return element ? element : null;
        }

        /// <summary>
        /// 親の CanvasGroup が操作を止めていないか。Selectable.IsInteractable のうち CanvasGroup の判定だけを取り出したもの。
        /// </summary>
        public static bool GroupsAllowInteraction(Transform transform)
        {
            for (var t = transform; t; t = t.parent)
            {
                t.GetComponents(CanvasGroupBuffer);
                foreach (var group in CanvasGroupBuffer)
                {
                    if (!group.enabled) continue;
                    if (!group.interactable) return false;
                    if (group.ignoreParentGroups) return true;
                }
            }
            return true;
        }
    }
}
