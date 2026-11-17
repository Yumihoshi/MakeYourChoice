using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 可交互物的公共部分：玩家视线正对物体时登记准心提示，按交互键触发 <see cref="OnInteract"/>。
    /// 光标未锁定（菜单 / 到站演出中）时一律不响应交互，也不显示提示。
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour
    {
        [SerializeField, Required, LabelText("视线起点（玩家眼睛）")]
        protected Transform eye;

        [SerializeField, LabelText("交互距离")]
        protected float maxDistance = 3f;

        void Update()
        {
            if (!IsAimed())
            {
                InteractFocus.Clear(this);
                return;
            }

            InteractFocus.Set(this);

            var input = PlayerInputRouter.Instance;
            if (input != null && input.Interact.WasPressedThisFrame())
            {
                OnInteract();
            }
        }

        void OnDisable() => InteractFocus.Clear(this);

        /// <summary>当前是否允许交互（剧情状态是否满足、有没有对话正在播）。</summary>
        protected abstract bool CanInteract();

        /// <summary>玩家按交互键时调用；此时已确认视线对准且 <see cref="CanInteract"/> 为真。</summary>
        protected abstract void OnInteract();

        /// <summary>光标锁定、视线对准自己的子物体且没有被遮挡。</summary>
        bool IsAimed()
        {
            if (eye == null || !GameCursorState.IsPlaying || !CanInteract())
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
