using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn
{
    /// <summary>
    /// 選択中の要素が ScrollRect のビューポートに収まるようにスクロールする。
    /// <see cref="NavigationScope.WithScrollIntoView"/> で登録した ScrollRect を <see cref="NavigationController"/> が選択の変化に合わせて動かす。
    /// 仮想カーソルのように Selectable でない対象は、利用側から直接呼んでよい。
    /// </summary>
    public static class ScrollIntoView
    {
        private const float SNAP_EPSILON = 0.5f;

        /// <summary>
        /// target が見える位置までコンテンツを動かす。content の直下でなくても、content の子孫であれば追従する。
        /// center が true なら、はみ出していなくても target をビューポートの中央へ寄せる。
        /// </summary>
        public static void EnsureVisible(ScrollRect scrollRect, RectTransform target, bool center = false)
        {
            if (!scrollRect || !scrollRect.content || !target || !target.IsChildOf(scrollRect.content)) return;

            Canvas.ForceUpdateCanvases();
            var viewport = scrollRect.viewport ? scrollRect.viewport : (RectTransform)scrollRect.transform;
            var viewportRect = viewport.rect;
            var corners = new Vector3[4];
            var (targetMin, targetMax) = ToViewportSpace(viewport, target, corners);
            var (contentMin, contentMax) = ToViewportSpace(viewport, scrollRect.content, corners);

            var delta = new Vector2(
                scrollRect.horizontal
                    ? CalculateAxisDelta(targetMin.x, targetMax.x, viewportRect.xMin, viewportRect.xMax, contentMin.x, contentMax.x, center)
                    : 0f,
                scrollRect.vertical
                    ? CalculateAxisDelta(targetMin.y, targetMax.y, viewportRect.yMin, viewportRect.yMax, contentMin.y, contentMax.y, center)
                    : 0f);
            if (delta == Vector2.zero) return;

            // 慣性で動き続けると、揃えた位置からすぐにずれる
            scrollRect.StopMovement();
            scrollRect.content.anchoredPosition += delta;
        }

        /// <summary>
        /// 1 軸ぶんの、コンテンツの移動量を返す。すべてビューポートのローカル座標で、コンテンツを動かすと各値も同じだけ動く。
        /// 見えている範囲内なら 0。はみ出した側の端へ揃え、コンテンツの端がビューポートの内側へ入らないよう制限する。
        /// </summary>
        public static float CalculateAxisDelta(
            float targetMin,
            float targetMax,
            float viewportMin,
            float viewportMax,
            float contentMin,
            float contentMax,
            bool center = false)
        {
            // ビューポートに収まるコンテンツはスクロールしない
            if (contentMax - contentMin <= viewportMax - viewportMin) return 0f;

            float delta;
            if (center) delta = (viewportMin + viewportMax - targetMin - targetMax) / 2f;
            else if (targetMax > viewportMax + SNAP_EPSILON) delta = viewportMax - targetMax;
            else if (targetMin < viewportMin - SNAP_EPSILON) delta = viewportMin - targetMin;
            else return 0f;

            return Mathf.Clamp(delta, viewportMax - contentMax, viewportMin - contentMin);
        }

        private static (Vector2 Min, Vector2 Max) ToViewportSpace(RectTransform viewport, RectTransform rect, Vector3[] corners)
        {
            rect.GetWorldCorners(corners);
            var min = (Vector2)viewport.InverseTransformPoint(corners[0]);
            var max = (Vector2)viewport.InverseTransformPoint(corners[2]);
            return (min, max);
        }
    }
}
