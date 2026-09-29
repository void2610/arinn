using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace Void2610.Arinn
{
    /// <summary>
    /// CanvasGroup の alpha をフェードさせる遷移。途中でキャンセルされたら、その時点の alpha で止まる。
    /// </summary>
    public sealed class FadeWindowTransition : IWindowTransition
    {
        private readonly float _duration;
        private readonly Ease _ease;

        public FadeWindowTransition(float duration = 0.2f, Ease ease = Ease.OutQuad)
        {
            _duration = duration;
            _ease = ease;
        }

        public UniTask ShowAsync(CanvasGroup canvasGroup, CancellationToken cancellationToken) => FadeAsync(canvasGroup, 1f, cancellationToken);

        public UniTask HideAsync(CanvasGroup canvasGroup, CancellationToken cancellationToken) => FadeAsync(canvasGroup, 0f, cancellationToken);

        private UniTask FadeAsync(CanvasGroup canvasGroup, float to, CancellationToken cancellationToken)
        {
            if (_duration <= 0f)
            {
                canvasGroup.alpha = to;
                return UniTask.CompletedTask;
            }
            return LMotion.Create(canvasGroup.alpha, to, _duration)
                .WithEase(_ease)
                .Bind(canvasGroup, static (alpha, group) => group.alpha = alpha)
                .ToUniTask(cancellationToken);
        }
    }
}
