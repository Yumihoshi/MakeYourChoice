using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 办公室区域的房间入口：玩家第一次走进前室（触发 10）、储藏室（触发 11）或打印室（触发 16）时播放一次剧情。
    /// 走进储藏室时还会让 <see cref="StorageLockdown"/> 把前室与储藏室的门关上并锁一分钟。
    /// </summary>
    public sealed class RoomEntryTrigger : MonoBehaviour
    {
        /// <summary>触发区所在的房间。</summary>
        public enum Room
        {
            [LabelText("打印室（触发 16）")] PrintRoom,
            [LabelText("前室（触发 10）")] PrepRoom,
            [LabelText("储藏室（触发 11）")] Storage,
        }

        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("剧情")]
        DialogueTrigger dialogue;

        [SerializeField, Required, LabelText("玩家")]
        Transform player;

        [SerializeField, Required, LabelText("触发范围")]
        BoxCollider area;

        [SerializeField, LabelText("房间")]
        Room room = Room.PrintRoom;

        [SerializeField, LabelText("储藏室关门倒计时")]
        StorageLockdown lockdown;

        void Update()
        {
            if (state == null || dialogue == null || player == null || area == null)
            {
                return;
            }

            if (HasEntered())
            {
                return;
            }

            if (!GameCursorState.IsPlaying || dialogue.IsRunning)
            {
                return;
            }

            if (!area.bounds.Contains(player.position))
            {
                return;
            }

            if (room == Room.PrintRoom)
            {
                state.MarkEnteredPrintRoom();
            }
            else if (room == Room.PrepRoom)
            {
                state.MarkEnteredPrepRoom();
            }
            else
            {
                state.MarkStorageTrapped();
                if (lockdown != null)
                {
                    lockdown.BeginLockdown();
                }
            }

            dialogue.StartDialogue();
        }

        /// <summary>这个房间的入口剧情是否已经播过。</summary>
        bool HasEntered()
        {
            return room switch
            {
                Room.PrintRoom => state.EnteredPrintRoom,
                Room.PrepRoom => state.EnteredPrepRoom,
                _ => state.StorageTrapped,
            };
        }
    }
}
