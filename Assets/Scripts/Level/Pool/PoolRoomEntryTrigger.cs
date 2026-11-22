using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 玩家第一次进入长池 / 池核所在的泳池房时置位，供更衣室柜门判断是否已允许打开。
    /// </summary>
    public sealed class PoolRoomEntryTrigger : MonoBehaviour
    {
        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        void Awake()
        {
            if (state == null)
            {
                Debug.LogError("PoolRoomEntryTrigger 缺少流程状态引用", this);
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
        }
    }
}
