namespace Void2610.Arinn
{
    /// <summary>
    /// ウィンドウの開閉に合わせてゲームプレイ側の入力を止める・戻す窓口。
    /// </summary>
    public interface IInputScopeGate
    {
        /// <summary>
        /// ウィンドウが 1 枚もない状態から最初のウィンドウが開いた。
        /// </summary>
        void OnFirstWindowOpened();

        /// <summary>
        /// 最後のウィンドウが閉じた。
        /// </summary>
        void OnLastWindowClosed();
    }
}
