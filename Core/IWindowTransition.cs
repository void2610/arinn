using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// ウィンドウの見た目の出し入れ。入力の受付（interactable / blocksRaycasts）は <see cref="WindowBase"/> が
    /// 遷移の開始時点で切り替えるため、ここでは alpha など見た目だけを扱う。
    /// キャンセルされたら途中の状態のまま止まってよい（次の遷移が現在値から始める）。
    /// </summary>
    public interface IWindowTransition
    {
        UniTask ShowAsync(CanvasGroup canvasGroup, CancellationToken cancellationToken);

        UniTask HideAsync(CanvasGroup canvasGroup, CancellationToken cancellationToken);
    }
}
