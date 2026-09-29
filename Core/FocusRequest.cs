using System;
using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// 後のフレームで適用するフォーカスの予約。新しい予約は古い予約を置き換える（最後の意図が勝つ）。
    /// </summary>
    internal sealed class FocusRequest
    {
        public Func<GameObject> Resolve { get; private set; }

        public int NotBeforeFrame { get; private set; }

        public bool WaitUntilActive { get; private set; }

        public int GiveUpFrame { get; private set; } = int.MaxValue;

        public Func<bool> HoldWhile { get; private set; }

        public bool IsPersistent { get; private set; }

        /// <summary>
        /// 次のフレームで対象を評価してフォーカスする。対象が非アクティブならあきらめる。
        /// </summary>
        public static FocusRequest NextFrame(Func<GameObject> resolve, int currentFrame, Func<bool> holdWhile = null, bool isPersistent = false) =>
            new()
            {
                Resolve = resolve,
                NotBeforeFrame = currentFrame + 1,
                HoldWhile = holdWhile,
                IsPersistent = isPersistent,
            };

        /// <summary>
        /// 対象がアクティブになるまで待ってフォーカスする。maxFrames を過ぎたらあきらめる。
        /// </summary>
        public static FocusRequest WhenActive(GameObject target, int currentFrame, int maxFrames) =>
            new()
            {
                Resolve = () => target,
                NotBeforeFrame = currentFrame,
                WaitUntilActive = true,
                GiveUpFrame = currentFrame + maxFrames,
            };
    }
}
