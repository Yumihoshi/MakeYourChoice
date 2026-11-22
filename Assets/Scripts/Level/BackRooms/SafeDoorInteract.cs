using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Level
{
    /// <summary>
    /// 关卡二中间墙上的安全门：玩家从行政办公室层正对门按交互键（鼠标左键 / 手柄 A）。
    /// 没卡时重复关卡一的“现在打不开”；有临时卡时只会失败（触发 13）；
    /// 拿到真门禁卡后把门升起、并触发下一节点的接口（触发 16）。
    /// </summary>
    public sealed class SafeDoorInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("关卡二状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("安全门（绕铰链旋转）")]
        AutoDoor door;

        [SerializeField, Required, LabelText("台词播放器")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("没卡时的剧情节点")]
        string noCardNode = "LV1_SafeDoor";

        [SerializeField, LabelText("临时卡失效的剧情节点")]
        string tempCardNode = "LV1_SafeDoorExpiredA";

        [SerializeField, LabelText("临时卡失效的剧情节点（已经看过柜子 A）")]
        string tempCardCheckedNode = "LV1_SafeDoorExpiredB";

        [SerializeField, LabelText("门打开后触发（切换下一节点的接口）")]
        UnityEvent exitOpened = new UnityEvent();

        /// <summary>门在拿到真卡之前一直是锁着的，靠得太近也不会自己升起。</summary>
        void Awake()
        {
            if (door == null)
            {
                Debug.LogError($"{name} 没有指定安全门，门会一直锁不上。", this);
                return;
            }

            door.Locked = true;
        }

        /// <summary>开过电梯门（触发 2）、门还没打开时才允许交互；开真卡那一次不需要台词在闲着。</summary>
        protected override bool CanInteract()
        {
            if (state == null || door == null || dialogue == null)
            {
                return false;
            }

            if (!GameProgress.ElevatorDoorOpened.Value || state.SafeDoorOpened)
            {
                return false;
            }

            return state.GotRealCard || !dialogue.IsRunning;
        }

        protected override void OnInteract()
        {
            if (state.GotRealCard)
            {
                state.MarkSafeDoorOpened();
                door.Locked = false;
                door.Open();
                exitOpened.Invoke();
                return;
            }

            if (state.GotTempCard)
            {
                state.MarkTriedSafeDoorWithTempCard();
            }

            dialogue.StartDialogue(NodeForCurrentCard());
        }

        /// <summary>按玩家手里是哪张卡、以及有没有被“仅仅反光”骗过，选这次要播的台词。</summary>
        string NodeForCurrentCard()
        {
            if (!state.GotTempCard)
            {
                return noCardNode;
            }

            return state.CheckedCabinetA ? tempCardCheckedNode : tempCardNode;
        }
    }
}
