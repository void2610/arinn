using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Void2610.Arinn
{
    /// <summary>
    /// 仮想カーソルの代わりに EventSystem のフォーカスを受ける、見た目を持たない Selectable。
    /// <see cref="CursorResolver"/> が生成して持つ。利用側がシーンやプレハブに置くものではない。
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class VirtualCursorAnchor : Selectable, ISubmitHandler
    {
        internal event Action<bool> FocusChanged;
        internal event Action Submitted;

        protected override void Awake()
        {
            base.Awake();
            // Unity の移動では抜けない。移動は CursorResolver が決める
            navigation = new Navigation { mode = Navigation.Mode.None };
            transition = Transition.None;
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            FocusChanged?.Invoke(true);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            FocusChanged?.Invoke(false);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (IsInteractable()) Submitted?.Invoke();
        }
    }
}
