using UnityEngine;

namespace Void2610.Arinn.Samples
{
    /// <summary>
    /// 1 枚目のウィンドウ。縦に長い一覧で、選択に追従してスクロールする。
    /// 閉じるボタンは右上にあり、一覧の右端から抜けられる。
    /// </summary>
    public sealed class SettingsWindow : WindowBase
    {
        private const int OPTION_COUNT = 12;

        public static SettingsWindow Create(Transform parent, NavigationController navigation, ConfirmDialog dialog)
        {
            var (window, panel) = SampleUI.CreateWindow<SettingsWindow>("SettingsWindow", parent, new Vector2(760f, 720f), "設定");
            var close = SampleUI.CreateButton("×", panel, new Vector2(320f, 310f), new Vector2(64f, 64f));
            window.SetCloseButton(close);

            var (scrollRect, content) = SampleUI.CreateVerticalScroll(panel, new Vector2(-30f, -30f), new Vector2(620f, 560f));
            for (var i = 1; i <= OPTION_COUNT; i++)
            {
                var label = $"オプション {i}";
                var option = SampleUI.CreateListButton(label, content, 64f, () => Debug.Log($"[arinn sample] {label} を押した"));
                if (i == 1) window.SetDefaultFocusElement(option);
            }
            SampleUI.CreateListButton("初期値に戻す", content, 64f,
                () => dialog.Open("設定を初期値に戻しますか？", () => Debug.Log("[arinn sample] 初期値に戻した")));

            navigation.Register(window, new NavigationScope(window.transform)
                .OnEdge(NavigationDirection.Right, EdgePolicy.Exit(close))
                .WithScrollIntoView(scrollRect));
            return window;
        }
    }
}
