using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Level
{
    /// <summary>
    /// 玩家进入或离开时触发事件的区域，用于剧情与开关。
    /// 默认只在游玩中（光标锁定）生效，避免开始菜单、暂停面板打开时把剧情播掉。
    /// </summary>
    public sealed class TriggerVolume : MonoBehaviour
    {
        [LabelText("只触发一次"), SerializeField] bool triggerOnce = true;
        [LabelText("仅在游玩中触发"), SerializeField] bool requireCursorLocked = true;
        [LabelText("玩家进入"), SerializeField] UnityEvent onPlayerEnter = new UnityEvent();
        [LabelText("玩家离开"), SerializeField] UnityEvent onPlayerExit = new UnityEvent();

        bool _hasTriggered;

        /// <summary>玩家进入事件的运行时订阅入口，编辑器里挂接时请直接用 Inspector 的事件列表。</summary>
        public UnityEvent OnPlayerEnter => onPlayerEnter;

        void OnTriggerEnter(Collider other)
        {
            TryTrigger(other);
        }

        /// <summary>玩家一开始就在触发区里（例如出生点），光标解锁后补触发一次。</summary>
        void OnTriggerStay(Collider other)
        {
            if (_hasTriggered)
            {
                return;
            }

            TryTrigger(other);
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            onPlayerExit.Invoke();
        }

        void TryTrigger(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (triggerOnce && _hasTriggered)
            {
                return;
            }

            if (requireCursorLocked && Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            _hasTriggered = true;
            onPlayerEnter.Invoke();
        }

        void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
            var previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = previous;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, transform.forward * 0.8f);
        }
    }
}
