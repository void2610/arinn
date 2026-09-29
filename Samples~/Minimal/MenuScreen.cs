using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn.Samples
{
    /// <summary>
    /// ウィンドウの下にある基底画面。ウィンドウではないので <see cref="IFocusSource"/> を実装し、
    /// ナビゲーションのスコープは <see cref="INavigationScopeSource"/> で宣言する。
    /// 最後のウィンドウを閉じると、この画面の既定要素へフォーカスが戻る。
    /// </summary>
    public sealed class MenuScreen : MonoBehaviour, IFocusSource, INavigationScopeSource
    {
        public Observable<Unit> OnSettingsClicked => _settings.OnClickAsObservable();

        public Observable<Unit> OnInventoryClicked => _inventory.OnClickAsObservable();

        public Observable<Unit> OnQuitClicked => _quit.OnClickAsObservable();

        public GameObject DefaultFocusElement => _settings ? _settings.gameObject : null;

        private Button _settings;
        private Button _inventory;
        private Button _quit;

        public static MenuScreen Create(Transform parent)
        {
            var root = SampleUI.CreateStretch("MenuScreen", parent);
            var screen = root.gameObject.AddComponent<MenuScreen>();
            SampleUI.CreateText("arinn sample", root, new Vector2(0f, 300f), new Vector2(800f, 100f), 64);
            screen._settings = SampleUI.CreateButton("設定", root, new Vector2(0f, 100f), new Vector2(360f, 72f));
            screen._inventory = SampleUI.CreateButton("持ち物", root, new Vector2(0f, 0f), new Vector2(360f, 72f));
            screen._quit = SampleUI.CreateButton("終了", root, new Vector2(0f, -100f), new Vector2(360f, 72f));
            SampleUI.CreateText("十字キー / 矢印キーで移動、決定で押す、Esc / B で閉じる", root, new Vector2(0f, -300f), new Vector2(1200f, 60f), 26);
            return screen;
        }

        // 上下の端は反対側へ回り込む
        public NavigationScope CreateNavigationScope() => new NavigationScope(transform)
            .OnEdge(NavigationDirection.Up, EdgePolicy.WrapRow)
            .OnEdge(NavigationDirection.Down, EdgePolicy.WrapRow);
    }
}
