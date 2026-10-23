using System;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.Arinn
{
    public static class InputSystemNavigationExtensions
    {
        /// <summary>
        /// Input System の InputSystemUIInputModule を方向入力に使い、マウスのホバーで選択する。
        /// </summary>
        /// <param name="controller">設定先</param>
        /// <param name="hoverSelection">マウスのホバーで選択するか</param>
        /// <param name="hoverIgnore">ホバーの対象から外すオブジェクト</param>
        /// <param name="hoverBlock">ホバーでは選ばず、下へも貫通させない Selectable</param>
        public static NavigationController UseInputSystem(this NavigationController controller, bool hoverSelection = true, Func<GameObject, bool> hoverIgnore = null, Func<Selectable, bool> hoverBlock = null)
        {
            controller.SetInput(new InputSystemNavigationInput());
            if (hoverSelection) controller.EnableHoverSelection(InputSystemPointerPosition.Instance, hoverIgnore, hoverBlock);
            return controller;
        }
    }
}
