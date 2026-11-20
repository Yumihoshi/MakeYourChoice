using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 储藏室的抽屉剧情：任意柜子累计拉开够 <see cref="requiredDrawers"/> 格之后，
    /// 播一次“发现维护记录”（触发 11），之后不再触发。
    /// </summary>
    public sealed class StorageDrawerQuest : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("发现维护记录的剧情")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("需要的抽屉数")]
        int requiredDrawers = 3;

        int _opened;

        /// <summary>累计拉开的抽屉格数（拉开加一，关回去不减）。</summary>
        public int OpenedCount => _opened;

        /// <summary>抽屉被拉开时调用；够数且还没播过维护记录时播一次。</summary>
        public void RegisterOpened()
        {
            if (state == null || dialogue == null || state.OpenedCabinetB)
            {
                return;
            }

            _opened++;
            if (_opened < Mathf.Max(1, requiredDrawers) || !state.EnteredStorage)
            {
                return;
            }

            state.MarkOpenedCabinetB();
            dialogue.StartDialogue();
        }
    }
}
