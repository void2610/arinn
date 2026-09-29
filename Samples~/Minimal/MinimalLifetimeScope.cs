using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using VContainer;
using VContainer.Unity;

namespace Void2610.Arinn.Samples
{
    /// <summary>
    /// 空のシーンの GameObject に付けて再生すると、基底画面・ウィンドウ 2 枚・確認ダイアログを組み立てて動かす。
    /// 登録するのは arinn と Presenter だけで、View は <see cref="MinimalPresenter"/> がシーンから取得する。
    /// </summary>
    public sealed class MinimalLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterArinn(manager => manager.SetSubmitHoldProbe(new InputSystemSubmitHoldProbe()));
            builder.RegisterArinnNavigation(navigation => navigation.UseInputSystem());
            builder.RegisterEntryPoint<MinimalPresenter>();
        }

        private void BuildUI()
        {
            // 後に作ったものほど手前に描かれるので、ダイアログを最後に作る
            var canvas = SampleUI.CreateCanvas(transform).transform;
            MenuScreen.Create(SampleUI.CreateStretch("Menu", canvas));
            var windowRoot = SampleUI.CreateStretch("Windows", canvas);
            SettingsWindow.Create(windowRoot);
            InventoryWindow.Create(windowRoot);
            ConfirmDialog.Create(SampleUI.CreateStretch("Dialogs", canvas));
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        protected override void Awake()
        {
            // prefab-view の規約の例外。Import するだけで動くよう、普通はシーンに置く EventSystem と UI をコンテナを組む前にコードで作る
            EnsureEventSystem();
            BuildUI();
            base.Awake();
        }
    }
}
