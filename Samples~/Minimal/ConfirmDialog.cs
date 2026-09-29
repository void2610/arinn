using System;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn.Samples
{
    /// <summary>
    /// はい / いいえ の確認ダイアログ。どのウィンドウの上にも重ねて開ける。
    /// 閉じると、開く前に選択していた要素（開いた元のボタン）へフォーカスが戻る。
    /// </summary>
    public sealed class ConfirmDialog : WindowBase
    {
        private Text _message;
        private Action _onYes;

        public static ConfirmDialog Create(Transform parent, NavigationController navigation)
        {
            var (dialog, panel) = SampleUI.CreateWindow<ConfirmDialog>("ConfirmDialog", parent, new Vector2(640f, 300f), "確認");
            dialog._message = SampleUI.CreateText("", panel, new Vector2(0f, 20f), new Vector2(600f, 80f));

            var yes = SampleUI.CreateButton("はい", panel, new Vector2(-130f, -90f), new Vector2(220f, 64f), dialog.OnYes);
            var no = SampleUI.CreateButton("いいえ", panel, new Vector2(130f, -90f), new Vector2(220f, 64f));
            dialog.SetCloseButton(no);
            // 誤って決定しても何も起きない側を既定にする
            dialog.SetDefaultFocusElement(no);

            navigation.Register(dialog, new NavigationScope(dialog.transform)
                .OnEdge(NavigationDirection.Left, EdgePolicy.WrapRow)
                .OnEdge(NavigationDirection.Right, EdgePolicy.WrapRow));
            return dialog;
        }

        public void Open(string message, Action onYes)
        {
            _message.text = message;
            _onYes = onYes;
            UIFocusManager.Instance.ShowWindow(this);
        }

        private void OnYes()
        {
            var onYes = _onYes;
            _onYes = null;
            // 先に閉じてフォーカスを返してから、結果の処理（別のウィンドウを開くなど）に進む
            UIFocusManager.Instance.HideWindow(this);
            onYes?.Invoke();
        }
    }
}
