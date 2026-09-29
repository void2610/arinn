using System;

namespace Void2610.Arinn
{
    /// <summary>
    /// 1 回の移動。4 方向なら <see cref="Primary"/> だけ、斜めなら <see cref="Primary"/> が水平、<see cref="Secondary"/> が垂直。
    /// </summary>
    public readonly struct NavigationStep : IEquatable<NavigationStep>
    {
        public NavigationDirection Primary { get; }

        public NavigationDirection? Secondary { get; }

        public NavigationStep(NavigationDirection primary, NavigationDirection? secondary = null)
        {
            Primary = primary;
            Secondary = secondary;
        }

        public bool Equals(NavigationStep other) => Primary == other.Primary && Secondary == other.Secondary;

        public override bool Equals(object obj) => obj is NavigationStep other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Primary, Secondary);

        public override string ToString() => Secondary.HasValue ? $"{Primary}+{Secondary.Value}" : Primary.ToString();
    }
}
