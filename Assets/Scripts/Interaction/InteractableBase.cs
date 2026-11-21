using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 可交互物的公共部分：玩家视线正对物体时登记准心提示，按交互键触发 <see cref="OnInteract"/>。
    /// 光标未锁定（菜单 / 到站演出中）或开场剧情还没播完时一律不响应交互，也不显示提示；
    /// 其他剧情播放期间仍允许交互。
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour
    {
        [SerializeField, Required, LabelText("视线起点（玩家眼睛）")]
        protected Transform eye;

        [SerializeField, LabelText("交互距离")]
        protected float maxDistance = 3f;

        /// <summary>
        /// 视线起点留空时取场景里的 Main Camera，让预制体实例（如泳池房）在关卡一、关卡二里都能交互；
        /// 场景里没有 Main Camera 就报错并禁用，而不是静默失效。
        /// </summary>
        protected virtual void Awake()
        {
            if (eye != null)
            {
                return;
            }

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError($"{GetType().Name} 的视线起点为空，且场景里没有 Main Camera", this);
                enabled = false;
                return;
            }

            eye = mainCamera.transform;
        }

        void Update()
        {
            if (!IsAimed())
            {
                InteractFocus.Clear(this);
                return;
            }

            InteractFocus.Set(this);

            var input = PlayerInputRouter.Instance;
            if (input == null)
            {
                return;
            }

            if (IsHoldInteraction)
            {
                if (input.Interact.IsPressed())
                {
                    OnInteractHeld(Time.deltaTime);
                }
            }
            else if (input.Interact.WasPressedThisFrame())
            {
                OnInteract();
            }
        }

        void OnDisable() => InteractFocus.Clear(this);

        /// <summary>当前是否允许交互（剧情状态是否满足、有没有对话正在播）。</summary>
        protected abstract bool CanInteract();

        /// <summary>玩家按交互键时调用；此时已确认视线对准且 <see cref="CanInteract"/> 为真。</summary>
        protected abstract void OnInteract();

        /// <summary>为 true 时走按住分支，由子类在 <see cref="OnInteractHeld"/> 里累计。</summary>
        protected virtual bool IsHoldInteraction => false;

        /// <summary>按住交互键期间每帧调用；只有 <see cref="IsHoldInteraction"/> 为 true 时才会走到。</summary>
        protected virtual void OnInteractHeld(float deltaTime) { }

        /// <summary>光标锁定、视线对准自己的子物体且没有被遮挡。</summary>
        bool IsAimed()
        {
            if (eye == null || !GameCursorState.IsPlaying || PlayerControlLock.IsLocked || !CanInteract())
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
