using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Level
{
    /// <summary>
    /// 玩家进入或离开时触发事件的区域，用于剧情与开关。
    /// </summary>
    public sealed class TriggerVolume : MonoBehaviour
    {
        [LabelText("只触发一次"), SerializeField] bool triggerOnce = true;
        [LabelText("玩家进入"), SerializeField] UnityEvent onPlayerEnter = new UnityEvent();
        [LabelText("玩家离开"), SerializeField] UnityEvent onPlayerExit = new UnityEvent();

        bool _hasTriggered;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (triggerOnce && _hasTriggered)
            {
                return;
            }

            _hasTriggered = true;
            onPlayerEnter.Invoke();
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            onPlayerExit.Invoke();
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
