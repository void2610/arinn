using System;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Void2610.Arinn.Samples
{
    /// <summary>
    /// 画面同士を繋ぐエントリポイント。View は互いを知らず、押されたことを通知するだけで、どのウィンドウを開くかはここで決める。
    /// </summary>
    public sealed class MinimalPresenter : IStartable, IDisposable
    {
        private readonly UIFocusManager _focusManager;
        private readonly MenuScreen _menu;
        private readonly SettingsWindow _settings;
        private readonly InventoryWindow _inventory;
        private readonly ConfirmDialog _dialog;
        private readonly CompositeDisposable _disposables = new();
        private InputAction _cancel;

        public MinimalPresenter(UIFocusManager focusManager)
        {
            _focusManager = focusManager;
            _menu = UnityEngine.Object.FindFirstObjectByType<MenuScreen>();
            _settings = UnityEngine.Object.FindFirstObjectByType<SettingsWindow>();
            _inventory = UnityEngine.Object.FindFirstObjectByType<InventoryWindow>();
            _dialog = UnityEngine.Object.FindFirstObjectByType<ConfirmDialog>();
        }

        public void Start()
        {
            _focusManager.SwitchBase(_menu);

            _menu.OnSettingsClicked.Subscribe(_ => _settings.Open()).AddTo(_disposables);
            _menu.OnInventoryClicked.Subscribe(_ => _inventory.Open()).AddTo(_disposables);
            _menu.OnQuitClicked
                .SubscribeAwait(async (_, ct) =>
                {
                    if (await _dialog.ConfirmAsync("サンプルを終了しますか？", ct)) Quit();
                }, AwaitOperation.Drop)
                .AddTo(_disposables);
            _settings.OnResetClicked
                .SubscribeAwait(async (_, ct) =>
                {
                    if (await _dialog.ConfirmAsync("設定を初期値に戻しますか？", ct)) Debug.Log("[arinn sample] 初期値に戻した");
                }, AwaitOperation.Drop)
                .AddTo(_disposables);
            _inventory.OnSlotSubmitted
                .SubscribeAwait(async (cell, ct) =>
                {
                    if (await _dialog.ConfirmAsync($"マス ({cell.x}, {cell.y}) の道具を捨てますか？", ct)) Debug.Log($"[arinn sample] ({cell.x}, {cell.y}) を捨てた");
                }, AwaitOperation.Drop)
                .AddTo(_disposables);

            // Cancel の順序はアプリの方針。このサンプルは「最前面のウィンドウを閉じる」だけ
            _cancel = new InputAction("Cancel", InputActionType.Button);
            _cancel.AddBinding("<Keyboard>/escape");
            _cancel.AddBinding("<Gamepad>/buttonEast");
            _cancel.performed += _ => _focusManager.TryPopScope();
            _cancel.Enable();
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _cancel?.Dispose();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
