using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Level
{
    /// <summary>
    /// 玩家正对电梯门时显示交互提示，按 E 开门并播放开门后的剧情。
    /// 对话播放期间与菜单打开时都不接受交互。
    /// </summary>
    public sealed class Level1DoorInteract : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        Level1State state;

        [SerializeField, Required, LabelText("玩家")]
        Transform player;

        [SerializeField, Required, LabelText("交互提示")]
        GameObject prompt;

        [SerializeField, Required, LabelText("开门剧情")]
        DialogueTrigger openDialogue;

        [SerializeField, LabelText("交互距离")]
        float maxDistance = 2.6f;

        [SerializeField, LabelText("正对角度")]
        float maxAngle = 40f;

        void Update()
        {
            bool canInteract = IsPlayerFacingDoor();
            if (prompt != null && prompt.activeSelf != canInteract)
            {
                prompt.SetActive(canInteract);
            }

            if (!canInteract)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                state.OpenElevatorDoor();
                if (openDialogue != null)
                {
                    openDialogue.StartDialogue();
                }
            }
        }

        /// <summary>玩家在交互距离内、面朝门，且门未开、没有对话在播、光标已锁定。</summary>
        bool IsPlayerFacingDoor()
        {
            if (state == null || player == null)
            {
                return false;
            }

            if (state.DoorOpened || Cursor.lockState != CursorLockMode.Locked)
            {
                return false;
            }

            if (openDialogue != null && openDialogue.IsRunning)
            {
                return false;
            }

            if (Vector3.Distance(player.position, transform.position) > maxDistance)
            {
                return false;
            }

            Vector3 toDoor = transform.position - player.position;
            toDoor.y = 0f;
            if (toDoor.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            return Vector3.Angle(player.forward, toDoor) <= maxAngle;
        }
    }
}
