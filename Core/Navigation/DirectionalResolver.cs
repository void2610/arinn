using System.Collections.Generic;
using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// 要素の矩形だけから移動先を決める純粋なロジック。Unity のシーンには依存しない。
    /// 矩形はすべて同じ座標系（ワールド座標）で、Y が上向きであること。
    /// </summary>
    public static class DirectionalResolver
    {
        // 進行方向の距離がこの値以下の候補は「その方向にある」とみなさない
        private const float MIN_PRIMARY_DISTANCE = 0.01f;

        // 進行方向に直交する軸のずれを、進行方向の距離より重く見る係数。整列した候補を優先する
        private const float PERPENDICULAR_WEIGHT = 2f;

        /// <summary>
        /// 指定方向で最も近い候補の添字を返す。無ければ -1。
        /// 進行方向の反対側にあるものは対象外。直交軸で重ならない候補は、中心のずれが進行方向の距離より小さいものだけを対象にする
        /// （段の違う端から斜めの別の段へ飛ばないようにするため）。
        /// </summary>
        public static int FindNext(Rect from, IReadOnlyList<Rect> candidates, NavigationDirection direction)
        {
            var best = -1;
            var bestScore = float.MaxValue;
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                var primary = PrimaryDistance(from, candidate, direction);
                if (primary <= MIN_PRIMARY_DISTANCE) continue;

                var perpendicular = PerpendicularOffset(from, candidate, direction);
                if (!OverlapsPerpendicular(from, candidate, direction) && perpendicular >= primary) continue;

                var score = primary + perpendicular * PERPENDICULAR_WEIGHT;
                if (score >= bestScore) continue;
                best = i;
                bestScore = score;
            }
            return best;
        }

        /// <summary>
        /// 指定方向の反対側で、同じ行（上下移動なら列）に並ぶ最も遠い候補の添字を返す。無ければ -1。
        /// </summary>
        public static int FindWrapTarget(Rect from, IReadOnlyList<Rect> candidates, NavigationDirection direction)
        {
            var best = -1;
            var bestPrimary = 0f;
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (!OverlapsPerpendicular(from, candidate, direction)) continue;
                var primary = PrimaryDistance(from, candidate, direction);
                if (primary >= -MIN_PRIMARY_DISTANCE) continue;
                if (best >= 0 && primary >= bestPrimary) continue;
                best = i;
                bestPrimary = primary;
            }
            return best;
        }

        private static bool IsVertical(NavigationDirection direction)
        {
            return direction is NavigationDirection.Up or NavigationDirection.Down;
        }

        // 進行方向に沿った中心間の距離（方向の反対側なら負）
        private static float PrimaryDistance(Rect from, Rect to, NavigationDirection direction)
        {
            var delta = to.center - from.center;
            return direction switch
            {
                NavigationDirection.Up => delta.y,
                NavigationDirection.Down => -delta.y,
                NavigationDirection.Left => -delta.x,
                _ => delta.x,
            };
        }

        // 進行方向と直交する軸での中心のずれ（絶対値）
        private static float PerpendicularOffset(Rect from, Rect to, NavigationDirection direction)
        {
            var delta = to.center - from.center;
            return IsVertical(direction) ? Mathf.Abs(delta.x) : Mathf.Abs(delta.y);
        }

        // 進行方向と直交する軸で範囲が重なっているか（左右移動なら同じ行、上下移動なら同じ列）
        private static bool OverlapsPerpendicular(Rect a, Rect b, NavigationDirection direction)
        {
            var overlap = IsVertical(direction)
                ? Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)
                : Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return overlap > MIN_PRIMARY_DISTANCE;
        }
    }
}
