using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using Void2610.LiminalPalette;

namespace Void2610.Arinn
{
    /// <summary>
    /// arinn の状態を LiminalPalette のコマンドとして公開する静的ホスト。観測だけで、操作の注入口は持たない。
    /// UIFocusManager / NavigationController はインスタンスなので LiminalPalette の走査対象にならず、属性はここに置いて Instance へ委譲する。
    /// </summary>
    public static class ArinnCommands
    {
        private const string TRUE = "true";
        private const string FALSE = "false";
        private const string NONE = "(none)";
        private const string NO_MANAGER = "(no manager)";
        private const string NOT_FOUND = "(not found)";

        [LiminalCommand("Arinn/TopWindow", Description = "最前面のウィンドウの GameObject 名。開いていなければ (none)")]
        public static string TopWindow()
        {
            var manager = UIFocusManager.Instance;
            if (manager == null) return NO_MANAGER;
            var top = manager.TopWindow;
            return top ? top.name : NONE;
        }

        [LiminalCommand("Arinn/WindowCount", Description = "開いているウィンドウの数")]
        public static string WindowCount()
        {
            var manager = UIFocusManager.Instance;
            return manager == null ? NO_MANAGER : manager.WindowCount.ToString(CultureInfo.InvariantCulture);
        }

        [LiminalCommand("Arinn/IsWindowVisible", Description = "GameObject 名が name のウィンドウの IsVisible (\"true\" / \"false\")。閉じる演出を自前で持つウィンドウが論理的に閉じ切ったかを待つのに使う。見つからなければ (not found)")]
        public static string IsWindowVisible(string name)
        {
            foreach (var window in Object.FindObjectsByType<WindowBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (window.name == name) return window.IsVisible ? TRUE : FALSE;
            }
            return NOT_FOUND;
        }

        [LiminalCommand("Arinn/IsFocusOnDefault", Description = "フォーカスが最前面のウィンドウ（なければ基底画面）の既定要素にあるか (\"true\" / \"false\")")]
        public static string IsFocusOnDefault()
        {
            var manager = UIFocusManager.Instance;
            if (manager == null) return NO_MANAGER;
            return manager.IsFocusOnDefaultElement ? TRUE : FALSE;
        }

        [LiminalCommand("Arinn/IsInPersistentUIMode", Description = "常時表示 UI にフォーカスを借りている最中か (\"true\" / \"false\")")]
        public static string IsInPersistentUIMode()
        {
            var manager = UIFocusManager.Instance;
            if (manager == null) return NO_MANAGER;
            return manager.IsInPersistentUIMode ? TRUE : FALSE;
        }

        [LiminalCommand("Arinn/Selected", Description = "EventSystem で選択中の GameObject 名。無ければ (none)")]
        public static string Selected()
        {
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            return selected ? selected.name : NONE;
        }

        [LiminalCommand("Arinn/ActiveScope", Description = "ナビゲーションが有効なスコープの Root の GameObject 名。無ければ (none)")]
        public static string ActiveScope()
        {
            var controller = NavigationController.Instance;
            if (controller == null) return "(no navigation)";
            var root = controller.ActiveScope?.Root;
            return root ? root.name : NONE;
        }

        [LiminalCommand("Arinn/IsSelectedInScope", Description = "選択中の要素が有効なスコープの中にあるか (\"true\" / \"false\")。スコープが無ければ false")]
        public static string IsSelectedInScope()
        {
            var scope = NavigationController.Instance?.ActiveScope;
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            return scope != null && scope.Contains(selected) ? TRUE : FALSE;
        }
    }
}
