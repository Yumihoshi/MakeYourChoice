using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 大厅门边的蓝色箭头（“↑”“←”“→”招牌）：玩家走过触发点 B 之后、还没进打印室之前，
    /// 交互一次会说明蓝箭头通向隔间区（触发 17）。
    /// </summary>
    public sealed class BlueArrowInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("关卡二状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("大厅状态")]
        HallFlowState hallState;

        [SerializeField, Required, LabelText("箭头台词")]
        DialogueTrigger dialogue;

        /// <summary>走过触发点 B、还没进打印室时才允许交互（触发 17 的前置是触发 8、未触发 16）。</summary>
        protected override bool CanInteract()
        {
            if (state == null || hallState == null || dialogue == null)
            {
                return false;
            }

            return hallState.EnteredRoom && !state.EnteredPrintRoom && !dialogue.IsRunning;
        }

        protected override void OnInteract() => dialogue.StartDialogue();
    }
}
