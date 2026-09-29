namespace Void2610.Arinn
{
    /// <summary>
    /// 4 方向ぶんの <see cref="EdgePolicy"/>。宣言しなかった方向は <see cref="EdgePolicy.Stop"/>。
    /// スコープと仮想カーソルが端の挙動を同じ形で持つための入れ物。
    /// </summary>
    public sealed class EdgePolicySet
    {
        private readonly EdgePolicy[] _policies = new EdgePolicy[4];

        public EdgePolicy Get(NavigationDirection direction) => _policies[(int)direction];

        public void Set(NavigationDirection direction, EdgePolicy policy) => _policies[(int)direction] = policy;
    }
}
