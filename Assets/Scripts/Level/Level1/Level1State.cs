using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡一的剧情状态：电梯门是否已打开、玩家是否在开门前看过红字“别开”。
    /// 触发点的先后顺序都以此为准，避免脚本之间互相判断。
    /// </summary>
    public sealed class Level1State : MonoBehaviour
    {
        [SerializeField, Required, LabelText("电梯门")]
        AutoDoor elevatorDoor;

        /// <summary>电梯门是否已经打开；开门后红字剧情与超时结局都不再触发。</summary>
        public bool DoorOpened { get; private set; }

        /// <summary>玩家是否已经回头看过红字“别开”。</summary>
        public bool SawRedText { get; private set; }

        /// <summary>记录玩家看过红字，供“贴门听 3 秒”的剧情解锁。</summary>
        public void MarkRedTextSeen()
        {
            SawRedText = true;
        }

        /// <summary>打开电梯门；重复调用不会重复开门。</summary>
        public void OpenElevatorDoor()
        {
            if (DoorOpened)
            {
                return;
            }

            if (elevatorDoor == null)
            {
                Debug.LogError("Level1State 未指定电梯门", this);
                return;
            }

            DoorOpened = true;
            elevatorDoor.Open();
        }
    }
}
