using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡一中间的安全门：电梯门打开后，玩家视线正对门时按交互键（鼠标左键 / 手柄 A），
    /// 播放一次“现在打不开，先去找门禁卡”的剧情。门本身不会打开。
    /// 电梯门还没开、剧情正在播、光标未锁定时不接受交互，也不显示交互提示。
    /// </summary>
    public sealed class Level1SafeDoorInteract : MonoBehaviour
    {
        [SerializeField, Required, LabelText("关卡状态")]
        Level1State state;

        [SerializeField, Required, LabelText("视线起点（玩家眼睛）")]
        Transform eye;

        [SerializeField, Required, LabelText("打不开剧情")]
        DialogueTrigger dialogue;

        [SerializeField, LabelText("交互距离")]
        float maxDistance = 3f;

        bool _fired;

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
            if (input == null || !input.Interact.WasPressedThisFrame())
            {
                return;
            }

            _fired = true;
            InteractFocus.Clear(this);
            dialogue.StartDialogue();
        }

        void OnDisable() => InteractFocus.Clear(this);

        /// <summary>视线正对门、视线没被挡住，且电梯门已经开过、还没播过、没有对话在播、光标已锁定。</summary>
        bool IsAimingAtDoor()
        {
            if (state == null || eye == null || dialogue == null || _fired || !state.DoorOpened)
            {
                return false;
            }

            if (Cursor.lockState != CursorLockMode.Locked || dialogue.IsRunning)
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
