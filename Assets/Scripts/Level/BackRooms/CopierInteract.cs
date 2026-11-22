using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 打印室的复印机：进过打印室之后交互一次，拿到临时门禁卡（触发 18）。
    /// 拿到之后不能再交互。
    /// </summary>
    public sealed class CopierInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("拿卡剧情")]
        DialogueTrigger dialogue;

        /// <summary>进过打印室、还没拿过临时卡时才允许交互。</summary>
        protected override bool CanInteract()
        {
            if (state == null || dialogue == null)
            {
                return false;
            }

            return state.EnteredPrintRoom && !state.GotTempCard && !dialogue.IsRunning;
        }

        protected override void OnInteract()
        {
            state.TakeTempCard();
            dialogue.StartDialogue();
        }
    }
}
