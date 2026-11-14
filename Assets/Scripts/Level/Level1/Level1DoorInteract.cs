using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 电梯门的交互：玩家视线正对门时按交互键（鼠标左键 / 手柄 A）开门并播放开门后的剧情。
    /// 看过红字但还没听完风声剧情时门保持锁定，既不显示交互提示也开不了门。
    /// 对话播放期间与菜单打开时都不接受交互。
    /// </summary>
    public sealed class Level1DoorInteract : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        Level1State state;

        [SerializeField, Required, LabelText("视线起点（玩家眼睛）")]
        Transform eye;

        [SerializeField, Required, LabelText("开门剧情")]
        DialogueTrigger openDialogue;

        [SerializeField, LabelText("交互距离")]
        float maxDistance = 3f;

        void Update()
        {
            bool aimed = IsAimingAtDoor();
            if (aimed)
            {
                InteractFocus.Set(this);
            }
            else
            {
                InteractFocus.Clear(this);
            }

            if (!aimed)
            {
                return;
            }

            var input = PlayerInputRouter.Instance;
            if (input != null && input.Interact.WasPressedThisFrame() && state.OpenElevatorDoor())
            {
                if (openDialogue != null)
                {
                    openDialogue.StartDialogue();
                }
            }
        }

        void OnDisable() => InteractFocus.Clear(this);

        /// <summary>视线正对门、视线没被挡住，且门未开、允许开门、没有对话在播、光标已锁定。</summary>
        bool IsAimingAtDoor()
        {
            if (state == null || eye == null || state.DoorOpened || !state.CanOpenDoor)
            {
                return false;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return false;
            }

            if (openDialogue != null && openDialogue.IsRunning)
            {
                return false;
            }

            // 视线从眼睛沿着朝向打一条射线，射线起点在玩家胶囊体内部，不会打到自己。
            if (!Physics.Raycast(eye.position, eye.forward, out var hit, maxDistance, ~0,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return hit.collider.transform.IsChildOf(transform);
        }
    }
}
