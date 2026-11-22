using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Level
{
    /// <summary>
    /// 大厅中间墙上的安全门：玩家正对门按交互键（鼠标左键 / 手柄 A）。
    /// 没卡时播触发 7；拿临时卡时会失败（触发 19，按有没有开过打印室柜子分两句）；
    /// 拿到真门禁卡后播触发 23、把门升起并调用 <see cref="exitOpened"/>。
    /// </summary>
    public sealed class SafeDoorInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("办公室区域状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("安全门（绕铰链旋转）")]
        AutoDoor door;

        [SerializeField, Required, LabelText("台词播放器")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("没卡时的剧情节点")]
        string noCardNode = "LV1_SafeDoor";

        [SerializeField, LabelText("临时卡失效的剧情节点（没开过打印室柜子）")]
        string tempCardNode = "LV1_SafeDoorExpiredA";

        [SerializeField, LabelText("临时卡失效的剧情节点（开过打印室柜子）")]
        string tempCardOpenedCabinetsNode = "LV1_SafeDoorExpiredB";

        [SerializeField, LabelText("真卡开门后的剧情节点")]
        string openedNode = "LV1_SafeDoorOpen";

        [SerializeField, LabelText("门打开后触发（切换下一节点的接口）")]
        UnityEvent exitOpened = new UnityEvent();

        /// <summary>门在拿到真卡之前一直是锁着的。</summary>
        protected override void Awake()
        {
            base.Awake();

            if (door == null)
            {
                Debug.LogError($"{name} 没有指定安全门，门会一直锁不上。", this);
                return;
            }

            door.Locked = true;
        }

        /// <summary>开过电梯门、还没用真卡开门时才允许交互；真卡那一次不要求台词闲着。</summary>
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
                dialogue.StartDialogue(openedNode);
                exitOpened.Invoke();
                return;
            }

            string node = noCardNode;
            if (state.GotTempCard)
            {
                state.MarkTriedSafeDoorWithTempCard();
                node = state.OpenedPrintCabinet ? tempCardOpenedCabinetsNode : tempCardNode;
            }

            dialogue.StartDialogue(node);
        }
    }
}
