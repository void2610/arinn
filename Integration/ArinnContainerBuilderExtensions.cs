using System;
using VContainer;
using VContainer.Unity;

namespace Void2610.Arinn
{
    public static class ArinnContainerBuilderExtensions
    {
        /// <summary>
        /// <see cref="UIFocusManager"/> をエントリポイントとして登録する（毎フレーム Tick され、スコープの破棄で Dispose される）。
        /// シーンを跨いで使う場合は親の LifetimeScope に登録する。
        /// </summary>
        /// <param name="builder">登録先</param>
        /// <param name="configure">生成直後の設定（既定の遷移など）</param>
        public static void RegisterArinn(this IContainerBuilder builder, Action<UIFocusManager> configure = null)
        {
            builder.RegisterEntryPoint<UIFocusManager>().AsSelf();
            if (configure != null) builder.RegisterBuildCallback(resolver => configure(resolver.Resolve<UIFocusManager>()));
        }

        /// <summary>
        /// <see cref="NavigationController"/> をエントリポイントとして登録する。
        /// <see cref="RegisterArinn"/> と同じ LifetimeScope か、その子に登録する。
        /// </summary>
        /// <param name="builder">登録先</param>
        /// <param name="configure">生成直後の設定（入力・ホバー選択・移動を止める条件など）</param>
        public static void RegisterArinnNavigation(this IContainerBuilder builder, Action<NavigationController> configure = null)
        {
            builder.RegisterEntryPoint<NavigationController>().AsSelf();
            if (configure != null) builder.RegisterBuildCallback(resolver => configure(resolver.Resolve<NavigationController>()));
        }
    }
}
