using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// アニメーションなしで即座に出し入れする既定の遷移。
    /// </summary>
    public sealed class InstantWindowTransition : IWindowTransition
    {
        public static readonly InstantWindowTransition Instance = new();

        public UniTask ShowAsync(CanvasGroup canvasGroup, CancellationToken cancellationToken)
        {
            canvasGroup.alpha = 1f;
            return UniTask.CompletedTask;
        }

        public UniTask HideAsync(CanvasGroup canvasGroup, CancellationToken cancellationToken)
        {
            canvasGroup.alpha = 0f;
            return UniTask.CompletedTask;
        }
    }
}
