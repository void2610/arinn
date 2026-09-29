using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// <see cref="NavigationController"/> が読む方向入力と、EventSystem 側の移動を止める手段。
    /// Core は入力の仕組みを知らないので、Input System 用の実装は Void2610.Arinn.InputSystem にある。
    /// </summary>
    public interface INavigationInput
    {
        /// <summary>
        /// 今の移動入力（スティック・十字キー・矢印キーなど）。
        /// EventSystem の移動を止めている間も読めること。
        /// </summary>
        Vector2 ReadMove();

        /// <summary>
        /// 押し続けたときに最初のリピートが始まるまでの秒数。
        /// </summary>
        float RepeatDelay { get; }

        /// <summary>
        /// リピートの間隔（秒）。
        /// </summary>
        float RepeatRate { get; }

        /// <summary>
        /// EventSystem の移動を止める。スコープが移動を解決している間、毎フレーム呼ばれる（他の処理が有効に戻しても止め直すため）。
        /// </summary>
        void SuppressUnityMove();

        /// <summary>
        /// <see cref="SuppressUnityMove"/> で止めた移動を戻す。止めていなければ何もしないこと。
        /// </summary>
        void RestoreUnityMove();
    }
}
