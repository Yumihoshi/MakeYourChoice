using DG.Tweening;
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 储藏室西墙的假门：触发 11 之后可以交互。
    /// 第一次交互播“门后面什么也没有……”（触发 14），第二次播“我说了——门后面什么也没有……”并把门板绕铰链转开（触发 15）。
    /// </summary>
    public sealed class FakeDoorInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("第一次交互的剧情")]
        DialogueTrigger firstDialogue;

        [SerializeField, Required, LabelText("第二次交互的剧情")]
        DialogueTrigger secondDialogue;

        [SerializeField, Required, LabelText("铰链")]
        Transform hinge;

        [SerializeField, LabelText("开门角度")]
        float openAngle = -100f;

        [SerializeField, LabelText("开门时长（秒）")]
        float duration = 0.8f;

        Vector3 _closedLocalEuler;

        protected override void Awake()
        {
            base.Awake();
            _closedLocalEuler = hinge != null ? hinge.localEulerAngles : Vector3.zero;
        }

        /// <summary>被关进储藏室之后、假门还没打开、剧情没在播时才允许交互。</summary>
        protected override bool CanInteract()
        {
            if (state == null || firstDialogue == null || secondDialogue == null || hinge == null)
            {
                return false;
            }

            return state.EnteredStorage
                   && !state.FakeDoorOpened
                   && !firstDialogue.IsRunning
                   && !secondDialogue.IsRunning;
        }

        protected override void OnInteract()
        {
            if (!state.FakeDoorInteracted)
            {
                state.MarkFakeDoorInteracted();
                firstDialogue.StartDialogue();
                return;
            }

            state.MarkFakeDoorOpened();
            secondDialogue.StartDialogue();
            hinge.DOKill();
            hinge.DOLocalRotate(_closedLocalEuler + Vector3.up * openAngle, duration).SetEase(Ease.OutCubic);
        }
    }
}
