using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 对准门板按交互键开门；可配置一段开门剧情（关卡一的电梯门用这个）。
    /// </summary>
    public class DoorInteract : InteractableBase
    {
        [SerializeField, Required, LabelText("门")]
        protected AutoDoor door;

        [SerializeField, LabelText("开门剧情")]
        protected DialogueTrigger openDialogue;

        /// <summary>开门剧情正在播放。</summary>
        protected bool DialogueRunning => openDialogue != null && openDialogue.IsRunning;

        protected override bool CanInteract()
        {
            return door != null && !door.IsOpen && !door.Locked && !DialogueRunning;
        }

        protected override void OnInteract()
        {
            if (!TryOpenDoor())
            {
                return;
            }

            if (openDialogue != null)
            {
                openDialogue.StartDialogue();
            }
        }

        /// <summary>把门打开，返回是否真的打开了；关卡一的电梯门要先过剧情闸门，所以留成虚方法。</summary>
        protected virtual bool TryOpenDoor()
        {
            door.Open();
            return true;
        }
    }
}
