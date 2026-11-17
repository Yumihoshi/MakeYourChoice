using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 电梯门的交互：玩家视线正对门时按交互键（鼠标左键 / 手柄 A）开门并播放开门后的剧情。
    /// 看过红字但还没听完风声剧情时门保持锁定，既不显示交互提示也开不了门。
    /// 对话播放期间与菜单打开时都不接受交互。
    /// </summary>
    public sealed class ElevatorDoorInteract : DoorInteract
    {
        [SerializeField, Required, LabelText("关卡状态")]
        HallFlowState state;

        protected override bool CanInteract()
        {
            return base.CanInteract() && state != null && !state.DoorOpened && state.CanOpenDoor;
        }

        /// <summary>先过剧情闸门再开门；闸门没过就不开门、也不播开门剧情。</summary>
        protected override bool TryOpenDoor()
        {
            return state != null && state.OpenElevatorDoor();
        }
    }
}
