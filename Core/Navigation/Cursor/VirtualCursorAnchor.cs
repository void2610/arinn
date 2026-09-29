using R3;
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
        internal Observable<bool> OnFocusChanged => _onFocusChanged;

        internal Observable<Unit> OnSubmitted => _onSubmitted;

        private readonly Subject<bool> _onFocusChanged = new();
        private readonly Subject<Unit> _onSubmitted = new();

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
            _onFocusChanged.OnNext(true);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            _onFocusChanged.OnNext(false);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (IsInteractable()) _onSubmitted.OnNext(Unit.Default);
        }

        protected override void OnDestroy()
        {
            _onFocusChanged.Dispose();
            _onSubmitted.Dispose();
            base.OnDestroy();
        }
    }
}
