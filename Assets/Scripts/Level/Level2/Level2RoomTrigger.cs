using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡二的房间入口：玩家第一次走进打印室（触发 8）或储藏室（触发 10）时播放一次剧情。
    /// 触发区用房间内部的 BoxCollider 当范围，进入储藏室时还会让红色门关上并锁一分钟。
    /// </summary>
    public sealed class Level2RoomTrigger : MonoBehaviour
    {
        /// <summary>触发区所在的房间。</summary>
        public enum Room
        {
            [LabelText("打印室（触发 8）")] PrintRoom,
            [LabelText("储藏室（触发 10）")] Storage,
        }

        [SerializeField, Required, LabelText("关卡状态")]
        Level2State state;

        [SerializeField, Required, LabelText("剧情")]
        DialogueTrigger dialogue;

        [SerializeField, Required, LabelText("玩家")]
        Transform player;

        [SerializeField, Required, LabelText("触发范围")]
        BoxCollider area;

        [SerializeField, LabelText("房间")]
        Room room = Room.PrintRoom;

        void Update()
        {
            if (state == null || dialogue == null || player == null || area == null)
            {
                return;
            }

            if (room == Room.PrintRoom ? state.EnteredPrintRoom : state.EnteredStorage)
            {
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked || dialogue.IsRunning)
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
            else
            {
                state.EnterStorage();
            }

            dialogue.StartDialogue();
        }
    }
}
