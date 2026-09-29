using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn
{
    /// <summary>
    /// 入力の時点の RectTransform の座標から移動先を決める既定の解決器。
    /// Unity の Selectable.navigation（Automatic / Explicit）は読まない。レイアウトの添字計算を再現せず、見えている位置だけを信じる。
    /// </summary>
    public sealed class SpatialResolver : INavigationResolver
    {
        public static readonly SpatialResolver Instance = new();

        public Selectable Resolve(NavigationScope scope, Selectable current, NavigationDirection direction)
        {
            var candidates = scope.CollectCandidates(current);
            var corners = new Vector3[4];
            var rects = new List<Rect>(candidates.Count);
            foreach (var candidate in candidates) rects.Add(GetWorldRect(candidate, corners));

            var from = GetWorldRect(current, corners);
            var index = DirectionalResolver.FindNext(from, rects, direction);
            if (index >= 0) return candidates[index];

            var edge = scope.GetEdge(direction);
            switch (edge.Kind)
            {
                case EdgePolicyKind.Exit:
                    return scope.ResolveExit(edge, current);
                case EdgePolicyKind.WrapRow:
                    var wrapIndex = DirectionalResolver.FindWrapTarget(from, rects, direction);
                    return wrapIndex >= 0 ? candidates[wrapIndex] : null;
                default:
                    return null;
            }
        }

        private static Rect GetWorldRect(Selectable selectable, Vector3[] corners)
        {
            ((RectTransform)selectable.transform).GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
