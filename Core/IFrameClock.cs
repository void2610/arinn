using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// 遅延フォーカスと監視が使う時刻。テストでフレームと時間を手で進めるために差し替える。
    /// </summary>
    internal interface IFrameClock
    {
        int FrameCount { get; }

        float UnscaledTime { get; }
    }

    internal sealed class UnityFrameClock : IFrameClock
    {
        public int FrameCount => Time.frameCount;

        public float UnscaledTime => Time.unscaledTime;
        public static readonly UnityFrameClock Instance = new();
    }
}
