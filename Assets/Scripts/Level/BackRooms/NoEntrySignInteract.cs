using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>触发 9：玩家走过触发点 B 之后、还没进打印室之前，交互“禁止入内”招牌。</summary>
    public sealed class NoEntrySignInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("剧情")]
        DialogueTrigger dialogue;

        [SerializeField, Required, LabelText("大厅状态（是否已经走过触发点 B）")]
        HallFlowState hallState;

        /// <summary>走过触发点 B、还没交互过招牌时才允许交互。</summary>
        protected override bool CanInteract()
        {
            if (state == null || dialogue == null || hallState == null)
            {
                return false;
            }

            return hallState.EnteredRoom && !state.OpenedNoEntrySign && !dialogue.IsRunning;
        }

        protected override void OnInteract()
        {
            state.MarkOpenedNoEntrySign();
            dialogue.StartDialogue();
        }
    }
}
