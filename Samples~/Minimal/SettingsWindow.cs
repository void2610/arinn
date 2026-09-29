using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn.Samples
{
    /// <summary>
    /// 1 枚目のウィンドウ。縦に長い一覧で、選択に追従してスクロールする。
    /// 閉じるボタンは右上にあり、一覧の右端から抜けられる。閉じるボタンと一覧の先頭は、位置では隣にならないので Link で明示する。
    /// </summary>
    public sealed class SettingsWindow : WindowBase
    {
        public Observable<Unit> OnResetClicked => _reset.OnClickAsObservable();

        private const int OPTION_COUNT = 12;

        private Button _close;
        private Button _firstOption;
        private Button _reset;
        private ScrollRect _scrollRect;

        public override NavigationScope CreateNavigationScope() => base.CreateNavigationScope().OnEdge(NavigationDirection.Right, EdgePolicy.Exit(_close)).Link(_close, NavigationDirection.Down, _firstOption).Link(_firstOption, NavigationDirection.Up, _close).WithScrollIntoView(_scrollRect);

        public static SettingsWindow Create(Transform parent)
        {
            var (window, panel) = SampleUI.CreateWindow<SettingsWindow>("SettingsWindow", parent, new Vector2(760f, 720f), "設定");
            window._close = SampleUI.CreateButton("×", panel, new Vector2(320f, 310f), new Vector2(64f, 64f));
            window.SetCloseButton(window._close);

            var (scrollRect, content) = SampleUI.CreateVerticalScroll(panel, new Vector2(-30f, -30f), new Vector2(620f, 560f));
            window._scrollRect = scrollRect;
            for (var i = 1; i <= OPTION_COUNT; i++)
            {
                var label = $"オプション {i}";
                var option = SampleUI.CreateListButton(label, content, 64f, () => Debug.Log($"[arinn sample] {label} を押した"));
                if (i != 1) continue;
                window._firstOption = option;
                window.SetDefaultFocusElement(option);
            }
            window._reset = SampleUI.CreateListButton("初期値に戻す", content, 64f);
            return window;
        }
    }
}
