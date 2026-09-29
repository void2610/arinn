using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using VContainer;
using VContainer.Unity;

namespace Void2610.Arinn.Samples
{
    /// <summary>
    /// 空のシーンの GameObject に付けて再生すると、基底画面・ウィンドウ 2 枚・確認ダイアログを組み立てて動かす。
    /// arinn の登録と、View をコンテナへ渡すところまでがここの役割で、画面同士の繋ぎは <see cref="MinimalPresenter"/> が持つ。
    /// </summary>
    public sealed class MinimalLifetimeScope : LifetimeScope
    {
        protected override void Awake()
        {
            // コンテナを組む前に EventSystem が要る（NavigationController が入力モジュールを読む）
            EnsureEventSystem();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterArinn(manager => manager.SetSubmitHoldProbe(new InputSystemSubmitHoldProbe()));
            builder.RegisterArinnNavigation(navigation => navigation.UseInputSystem());

            // 後に作ったものほど手前に描かれるので、ダイアログを最後に作る
            var canvas = SampleUI.CreateCanvas(transform).transform;
            var menu = MenuScreen.Create(SampleUI.CreateStretch("Menu", canvas));
            var windowRoot = SampleUI.CreateStretch("Windows", canvas);
            var settings = SettingsWindow.Create(windowRoot);
            var inventory = InventoryWindow.Create(windowRoot);
            var dialog = ConfirmDialog.Create(SampleUI.CreateStretch("Dialogs", canvas));

            // ウィンドウはコンテナから UIFocusManager を注入され、Open / Close はそれを使う
            builder.RegisterComponent(menu);
            builder.RegisterComponent(settings);
            builder.RegisterComponent(inventory);
            builder.RegisterComponent(dialog);
            builder.RegisterEntryPoint<MinimalPresenter>();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
