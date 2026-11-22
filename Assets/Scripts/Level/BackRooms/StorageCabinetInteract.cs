using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 储藏室里的柜子：进过储藏室之后交互一次，播放“发现维护记录”的剧情（触发 24）。
    /// 默认由 <see cref="StorageDrawerQuest"/> 按累计拉开抽屉数触发，这个组件保留给单个柜子单独播报的场合。
    /// </summary>
    public sealed class StorageCabinetInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("打开柜子的剧情")]
        DialogueTrigger dialogue;

        /// <summary>进过储藏室、还没看过维护记录时才允许交互。</summary>
        protected override bool CanInteract()
        {
            if (state == null || dialogue == null)
            {
                return false;
            }

            return state.EnteredStorage && !state.OpenedCabinetB && !dialogue.IsRunning;
        }

        protected override void OnInteract()
        {
            state.MarkOpenedCabinetB();
            dialogue.StartDialogue();
        }
    }
}
