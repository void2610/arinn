using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn.Samples
{
    /// <summary>
    /// ウィンドウの下にある基底画面。ウィンドウではないので <see cref="IFocusSource"/> だけを実装する。
    /// 最後のウィンドウを閉じると、この画面の既定要素へフォーカスが戻る。
    /// </summary>
    public sealed class MenuScreen : IFocusSource
    {
        public GameObject DefaultFocusElement => _first ? _first.gameObject : null;

        private Button _first;

        public static MenuScreen Create(Transform parent, NavigationController navigation, SettingsWindow settings, InventoryWindow inventory, ConfirmDialog dialog)
        {
            var screen = new MenuScreen();
            var root = SampleUI.CreateStretch("MenuScreen", parent);
            SampleUI.CreateText("arinn sample", root, new Vector2(0f, 300f), new Vector2(800f, 100f), 64);

            var manager = UIFocusManager.Instance;
            screen._first = SampleUI.CreateButton("設定", root, new Vector2(0f, 100f), new Vector2(360f, 72f), () => manager.ShowWindow(settings));
            SampleUI.CreateButton("持ち物", root, new Vector2(0f, 0f), new Vector2(360f, 72f), () => manager.ShowWindow(inventory));
            SampleUI.CreateButton("終了", root, new Vector2(0f, -100f), new Vector2(360f, 72f),
                () => dialog.Open("サンプルを終了しますか？", Quit));
            SampleUI.CreateText("十字キー / 矢印キーで移動、決定で押す、Esc / B で閉じる", root, new Vector2(0f, -300f), new Vector2(1200f, 60f), 26);

            // 上下の端は反対側へ回り込む
            navigation.Register(screen, new NavigationScope(root)
                .OnEdge(NavigationDirection.Up, EdgePolicy.WrapRow)
                .OnEdge(NavigationDirection.Down, EdgePolicy.WrapRow));
            return screen;
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
