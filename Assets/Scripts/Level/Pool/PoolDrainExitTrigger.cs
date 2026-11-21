using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Level
{
    /// <summary>竖井底的一次性触发；只发下一关事件，不做加载/淡出。</summary>
    public sealed class PoolDrainExitTrigger : MonoBehaviour
    {
        [SerializeField, Required, LabelText("流程状态")]
        PoolFlowState state;

        [SerializeField, LabelText("下一关事件")]
        UnityEvent onNextLevel;

        bool _fired;

        void OnTriggerEnter(Collider other)
        {
            if (_fired)
            {
                return;
            }

            if (state == null)
            {
                Debug.LogError("PoolDrainExitTrigger 缺少流程状态引用", this);
                return;
            }

            if (!state.CoreDrained || !other.CompareTag("Player") || !GameCursorState.IsPlaying)
            {
                return;
            }

            _fired = true;
            state.MarkNextLevelReached();
            onNextLevel?.Invoke();
        }
    }
}
