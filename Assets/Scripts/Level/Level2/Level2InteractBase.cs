using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 关卡二可交互物的公共部分：玩家视线正对物体时登记准心提示，
    /// 按交互键（鼠标左键 / 手柄 A）触发 <see cref="OnInteract"/>。
    /// 子类只用说明“什么时候允许交互”和“交互后做什么”。
    /// </summary>
    public abstract class Level2InteractBase : MonoBehaviour
    {
        [SerializeField, Required, LabelText("视线起点（玩家眼睛）")]
        protected Transform eye;

        [SerializeField, LabelText("交互距离")]
        protected float maxDistance = 3f;

        void Update()
        {
            bool aimed = IsAimed();
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
            if (input != null && input.Interact.WasPressedThisFrame())
            {
                OnInteract();
            }
        }

        void OnDisable() => InteractFocus.Clear(this);

        /// <summary>玩家按交互键时调用；此时已经确认视线对准、且<see cref="CanInteract"/>为真。</summary>
        protected abstract void OnInteract();

        /// <summary>当前是否允许交互（剧情状态是否满足、有没有对话正在播）。</summary>
        protected abstract bool CanInteract();

        /// <summary>视线正对物体、视线没被挡住，且当前允许交互、光标已锁定。</summary>
        bool IsAimed()
        {
            if (eye == null || Cursor.lockState != CursorLockMode.Locked || !CanInteract())
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
