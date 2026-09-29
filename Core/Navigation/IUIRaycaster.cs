using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Void2610.Arinn
{
    /// <summary>
    /// ホバー選択の当たり判定。EditMode では GraphicRaycaster が画面座標で当たらないため、テストで差し替える。
    /// </summary>
    internal interface IUIRaycaster
    {
        void RaycastAll(EventSystem eventSystem, PointerEventData pointerData, List<RaycastResult> results);
    }

    internal sealed class EventSystemRaycaster : IUIRaycaster
    {
        public static readonly EventSystemRaycaster Instance = new();

        public void RaycastAll(EventSystem eventSystem, PointerEventData pointerData, List<RaycastResult> results) => eventSystem.RaycastAll(pointerData, results);
    }
}
