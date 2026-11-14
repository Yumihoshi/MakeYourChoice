using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 要按交互键才打开的门（北墙通往打印室 / 储藏室的两扇）：
    /// 玩家对准门板按交互键才打开，不做靠近自动开门。
    /// </summary>
    public sealed class Level2DoorInteract : Level2InteractBase
    {
        [SerializeField, Required, LabelText("门")]
        AutoDoor door;

        /// <summary>门还没开、也没有被剧情锁住时才允许交互。</summary>
        protected override bool CanInteract()
        {
            return door != null && !door.IsOpen && !door.Locked;
        }

        protected override void OnInteract()
        {
            GameSfx.Play(GameSfx.ElevatorDoorOpen);
            door.Open();
        }
    }
}
