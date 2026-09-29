using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
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
        private UniTaskCompletionSource<bool> _result;
        private bool _confirmed;

        public override NavigationScope CreateNavigationScope() => base.CreateNavigationScope().OnEdge(NavigationDirection.Left, EdgePolicy.WrapRow).OnEdge(NavigationDirection.Right, EdgePolicy.WrapRow);

        public static ConfirmDialog Create(Transform parent)
        {
            var (dialog, panel) = SampleUI.CreateWindow<ConfirmDialog>("ConfirmDialog", parent, new Vector2(640f, 300f), "確認");
            dialog._message = SampleUI.CreateText("", panel, new Vector2(0f, 20f), new Vector2(600f, 80f));

            SampleUI.CreateButton("はい", panel, new Vector2(-130f, -90f), new Vector2(220f, 64f), dialog.Confirm);
            var no = SampleUI.CreateButton("いいえ", panel, new Vector2(130f, -90f), new Vector2(220f, 64f));
            dialog.SetCloseButton(no);
            // 誤って決定しても何も起きない側を既定にする
            dialog.SetDefaultFocusElement(no);
            // いいえ、Cancel、「はい」のどれで閉じても、閉じてフォーカスが戻った後に結果を返す
            dialog.OnWindowClosed.Subscribe(_ => dialog._result?.TrySetResult(dialog._confirmed)).AddTo(dialog);
            return dialog;
        }

        /// <summary>
        /// メッセージを出して開き、「はい」で閉じたら true、それ以外で閉じたら false を返す。
        /// </summary>
        public UniTask<bool> ConfirmAsync(string message, CancellationToken cancellationToken)
        {
            _message.text = message;
            _confirmed = false;
            _result = new UniTaskCompletionSource<bool>();
            Open();
            return _result.Task.AttachExternalCancellation(cancellationToken);
        }

        private void Confirm()
        {
            _confirmed = true;
            Close();
        }
    }
}
