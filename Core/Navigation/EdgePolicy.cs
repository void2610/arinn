using UnityEngine.UI;

namespace Void2610.Arinn
{
    /// <summary>
    /// 端（その方向に移動先が無い状態）で移動入力を受けたときの挙動の種類。
    /// </summary>
    public enum EdgePolicyKind
    {
        /// <summary> その場に留まる（既定） </summary>
        Stop,

        /// <summary> 宣言した要素へ抜ける </summary>
        Exit,

        /// <summary> 同じ行（上下移動なら列）の反対側の端へ回り込む </summary>
        WrapRow,
    }

    /// <summary>
    /// 端での挙動の宣言。<see cref="NavigationScope.OnEdge"/> や仮想カーソルの OnEdge で方向ごとにコードから指定する。
    /// 候補そのものを対象から外す宣言は <see cref="NavigationScope.Exclude"/> で行う（端の挙動とは別の話のため）。
    /// </summary>
    public readonly struct EdgePolicy
    {
        public EdgePolicyKind Kind { get; }

        /// <summary>
        /// <see cref="EdgePolicyKind.Exit"/> の抜け先。それ以外では null。
        /// </summary>
        public Selectable ExitTarget { get; }

        /// <summary>
        /// 端で止まる。宣言しなかった方向の既定。
        /// </summary>
        public static EdgePolicy Stop => default;

        /// <summary>
        /// 反対側の端へ回り込む。
        /// </summary>
        public static EdgePolicy WrapRow => new(EdgePolicyKind.WrapRow, null);

        private EdgePolicy(EdgePolicyKind kind, Selectable exitTarget)
        {
            Kind = kind;
            ExitTarget = exitTarget;
        }

        /// <summary>
        /// スコープ内の指定した要素へ抜ける。スコープ外・操作不可・非表示の要素や、自分自身が指定されていた場合は止まる。
        /// </summary>
        public static EdgePolicy Exit(Selectable target) => new(EdgePolicyKind.Exit, target);
    }
}
