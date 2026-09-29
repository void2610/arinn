namespace Void2610.Arinn
{
    /// <summary>
    /// 決定入力が押されたままかを返す。
    /// 決定と同時押しのショートカットでフォーカスを移したとき、決定の離しで移動先のボタンが押されるのを防ぐために使う。
    /// </summary>
    public interface ISubmitHoldProbe
    {
        bool IsSubmitHeld { get; }
    }
}
