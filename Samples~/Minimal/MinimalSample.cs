using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Void2610.Arinn.Samples
{
    /// <summary>
    /// 空のシーンの GameObject に付けて再生すると、基底画面・ウィンドウ 2 枚・確認ダイアログを組み立てて動かす。
    /// VContainer を使わない構成で、マネージャーの生成と Tick はこのコンポーネントが行う。
    /// VContainer を使う場合は RegisterArinn / RegisterArinnNavigation で登録すれば Tick も Dispose も任せられる。
    /// </summary>
    public sealed class MinimalSample : MonoBehaviour
    {
        private UIFocusManager _focusManager;
        private NavigationController _navigation;
        private InputAction _cancel;

        private void Awake()
        {
            _focusManager = new UIFocusManager();
            _navigation = new NavigationController(_focusManager).UseInputSystem();
            EnsureEventSystem();

            // 後に作ったものほど手前に描かれるので、ダイアログを最後に作る
            var canvas = SampleUI.CreateCanvas(transform).transform;
            var menuRoot = SampleUI.CreateStretch("Menu", canvas);
            var windowRoot = SampleUI.CreateStretch("Windows", canvas);
            var dialogRoot = SampleUI.CreateStretch("Dialogs", canvas);
            var dialog = ConfirmDialog.Create(dialogRoot, _navigation);
            var settings = SettingsWindow.Create(windowRoot, _navigation, dialog);
            var inventory = InventoryWindow.Create(windowRoot, _navigation, dialog);
            var menu = MenuScreen.Create(menuRoot, _navigation, settings, inventory, dialog);

            _focusManager.SwitchBase(menu);

            // Cancel の順序はアプリの方針。このサンプルは「最前面のウィンドウを閉じる」だけ
            _cancel = new InputAction("Cancel", InputActionType.Button);
            _cancel.AddBinding("<Keyboard>/escape");
            _cancel.AddBinding("<Gamepad>/buttonEast");
            _cancel.performed += _ => _focusManager.TryPopScope();
            _cancel.Enable();
        }

        private void Update()
        {
            _focusManager.Tick();
            _navigation.Tick();
        }

        private void OnDestroy()
        {
            _cancel?.Dispose();
            _navigation?.Dispose();
            _focusManager?.Dispose();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
