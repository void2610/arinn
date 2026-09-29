using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// 移動入力の Vector2 を、4 方向の移動（押しっぱなしのリピート付き）に変換する。
    /// EventSystem の move が行っていた処理を、ライブラリ側で行うためのもの。
    /// </summary>
    public sealed class DirectionRepeater
    {
        private const float DEAD_ZONE = 0.5f;

        private NavigationDirection? _held;
        private float _nextRepeatTime;

        /// <summary>
        /// 入力値を渡し、このフレームに発火すべき移動があれば方向を返す。
        /// </summary>
        /// <param name="value">移動入力の値</param>
        /// <param name="time">現在時刻（秒）</param>
        /// <param name="repeatDelay">押し続けたときに最初のリピートが始まるまでの秒数</param>
        /// <param name="repeatRate">リピートの間隔（秒）</param>
        public NavigationDirection? Advance(Vector2 value, float time, float repeatDelay, float repeatRate)
        {
            var direction = Quantize(value);
            if (direction == null)
            {
                _held = null;
                return null;
            }

            if (_held != direction)
            {
                _held = direction;
                _nextRepeatTime = time + repeatDelay;
                return direction;
            }

            if (time < _nextRepeatTime) return null;
            _nextRepeatTime = time + repeatRate;
            return direction;
        }

        /// <summary>
        /// 入力値を 4 方向へ丸める。デッドゾーン内は null。斜めは絶対値の大きい軸を採り、同じなら水平を優先する。
        /// </summary>
        public static NavigationDirection? Quantize(Vector2 value)
        {
            if (value.sqrMagnitude < DEAD_ZONE * DEAD_ZONE) return null;
            if (Mathf.Abs(value.x) >= Mathf.Abs(value.y)) return value.x > 0f ? NavigationDirection.Right : NavigationDirection.Left;
            return value.y > 0f ? NavigationDirection.Up : NavigationDirection.Down;
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
