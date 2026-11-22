using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 玩家第一次进入泳池房时置位并播一段旁白，供更衣室柜门判断是否已允许打开。
    /// </summary>
    public sealed class PoolRoomEntryTrigger : MonoBehaviour
    {
        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, Required, LabelText("进入泳池房剧情")]
        DialogueTrigger dialogue;

        void Awake()
        {
            if (state == null || dialogue == null)
            {
                Debug.LogError("PoolRoomEntryTrigger 缺少流程状态或剧情引用", this);
                enabled = false;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            TryMark(other);
        }

        void OnTriggerStay(Collider other)
        {
            TryMark(other);
        }

        void TryMark(Collider other)
        {
            if (state == null || state.EnteredPoolRoom)
            {
                return;
            }

            if (!other.CompareTag("Player") || !GameCursorState.IsPlaying)
            {
                return;
            }

            state.MarkEnteredPoolRoom();
            dialogue.StartDialogue();
        }
    }
}
