using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡二大厅里的蓝色箭头（“→”招牌）：玩家走过触发点 B 之后、还没进打印室之前，
    /// 交互一次会追问“你为什么让我跟着蓝色箭头”（触发 9）。
    /// </summary>
    public sealed class BlueArrowInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("关卡二状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("追问剧情")]
        DialogueTrigger dialogue;

        /// <summary>走过触发点 B、还没进打印室时才允许交互（触发 9 的前置是触发 7、未触发 8）。</summary>
        protected override bool CanInteract()
        {
            if (state == null || dialogue == null)
            {
                return false;
            }

            return GameProgress.EnteredLobby.Value && !state.EnteredPrintRoom && !dialogue.IsRunning;
        }

        protected override void OnInteract() => dialogue.StartDialogue();
    }
}
