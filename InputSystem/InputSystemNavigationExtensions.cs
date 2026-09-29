using System;
using UnityEngine;

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
        public static NavigationController UseInputSystem(this NavigationController controller, bool hoverSelection = true, Func<GameObject, bool> hoverIgnore = null)
        {
            controller.SetInput(new InputSystemNavigationInput());
            if (hoverSelection) controller.EnableHoverSelection(InputSystemPointerPosition.Instance, hoverIgnore);
            return controller;
        }
    }
}
