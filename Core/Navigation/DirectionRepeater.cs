using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// 移動入力の Vector2 を方向へ丸め、押しっぱなしのときにリピートする。
    /// EventSystem の move が行っていた処理を、ライブラリ側で行うためのもの。
    /// </summary>
    public sealed class DirectionRepeater
    {
        private const float DEAD_ZONE = 0.5f;

        private NavigationStep? _held;
        private float _nextRepeatTime;

        /// <summary>
        /// 入力値を渡し、このフレームに発火すべき移動があれば返す。
        /// </summary>
        /// <param name="value">移動入力の値</param>
        /// <param name="time">現在時刻（秒）</param>
        /// <param name="repeatDelay">押し続けたときに最初のリピートが始まるまでの秒数</param>
        /// <param name="repeatRate">リピートの間隔（秒）</param>
        /// <param name="mode">方向への丸め方</param>
        /// <param name="repeats">false なら押しっぱなしでもリピートせず、押し直すか向きを変えたときだけ発火する</param>
        public NavigationStep? Advance(Vector2 value, float time, float repeatDelay, float repeatRate, NavigationInputMode mode = NavigationInputMode.FourWay, bool repeats = true)
        {
            var step = Quantize(value, mode);
            if (step == null)
            {
                _held = null;
                return null;
            }

            if (!_held.Equals(step))
            {
                _held = step;
                _nextRepeatTime = time + repeatDelay;
                return step;
            }

            if (!repeats || time < _nextRepeatTime) return null;
            _nextRepeatTime = time + repeatRate;
            return step;
        }

        /// <summary>
        /// 入力値を丸める。デッドゾーン内は null。
        /// </summary>
        public static NavigationStep? Quantize(Vector2 value, NavigationInputMode mode = NavigationInputMode.FourWay)
        {
            var horizontal = Mathf.Abs(value.x) >= DEAD_ZONE ? value.x > 0f ? NavigationDirection.Right : NavigationDirection.Left : (NavigationDirection?)null;
            var vertical = Mathf.Abs(value.y) >= DEAD_ZONE ? value.y > 0f ? NavigationDirection.Up : NavigationDirection.Down : (NavigationDirection?)null;
            switch (mode)
            {
                case NavigationInputMode.HorizontalOnly:
                    return horizontal.HasValue ? new NavigationStep(horizontal.Value) : null;
                case NavigationInputMode.VerticalOnly:
                    return vertical.HasValue ? new NavigationStep(vertical.Value) : null;
                case NavigationInputMode.EightWay:
                    if (horizontal.HasValue) return new NavigationStep(horizontal.Value, vertical);
                    return vertical.HasValue ? new NavigationStep(vertical.Value) : null;
                default:
                    return QuantizeDominant(value, mode == NavigationInputMode.FourWayPreferVertical);
            }
        }

        private static NavigationStep? QuantizeDominant(Vector2 value, bool preferVertical)
        {
            if (value.sqrMagnitude < DEAD_ZONE * DEAD_ZONE) return null;
            var absX = Mathf.Abs(value.x);
            var absY = Mathf.Abs(value.y);
            var useHorizontal = preferVertical ? absX > absY : absX >= absY;
            if (useHorizontal) return new NavigationStep(value.x > 0f ? NavigationDirection.Right : NavigationDirection.Left);
            return new NavigationStep(value.y > 0f ? NavigationDirection.Up : NavigationDirection.Down);
        }

        /// <summary>
        /// 押下の状態を捨てる。次に同じ方向が入力されたら、新しい押下として扱う。
        /// </summary>
        public void Reset()
        {
            _held = null;
        }
    }
}
