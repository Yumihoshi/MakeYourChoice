using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡一的剧情状态：电梯门是否已打开、玩家是否在开门前看过红字“别开”、是否已经听完门外的风声。
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

        /// <summary>风声剧情是否已经开始播放。</summary>
        public bool ListenStarted { get; private set; }

        /// <summary>风声剧情是否已经播完。</summary>
        public bool ListenFinished { get; private set; }

        /// <summary>
        /// 当前是否允许打开电梯门：没看过红字随时可以开；看过红字必须先听完门外的风声剧情。
        /// </summary>
        public bool CanOpenDoor => !SawRedText || ListenFinished;

        /// <summary>记录玩家看过红字，供“贴门听 3 秒”的剧情解锁。</summary>
        public void MarkRedTextSeen()
        {
            SawRedText = true;
        }

        /// <summary>记录风声剧情已开始；播放期间电梯门保持锁定。</summary>
        public void MarkListenStarted()
        {
            ListenStarted = true;
        }

        /// <summary>记录风声剧情已播完，此时才允许开门。</summary>
        public void MarkListenFinished()
        {
            ListenFinished = true;
        }

        /// <summary>打开电梯门；门已经打开或仍在锁定中时什么都不做。返回是否真的打开了门。</summary>
        public bool OpenElevatorDoor()
        {
            if (DoorOpened || !CanOpenDoor)
            {
                return false;
            }

            if (elevatorDoor == null)
            {
                Debug.LogError("Level1State 未指定电梯门", this);
                return false;
            }

            DoorOpened = true;
            elevatorDoor.Open();
            return true;
        }
    }
}
