using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 玩家靠近时向上升起的门；触发区挂在根节点，门扇作为子物体单独移动。
    /// </summary>
    public sealed class AutoDoor : MonoBehaviour
    {
        [LabelText("门扇"), SerializeField] Transform doorPanel;
        [LabelText("升起高度"), SerializeField] float openHeight = 2.5f;
        [LabelText("开关门时长"), SerializeField] float duration = 0.6f;
        [LabelText("自动关门"), SerializeField] bool autoClose;
        [LabelText("关门延迟"), SerializeField] float closeDelay = 2f;

        Vector3 _closedLocalPosition;
        Tween _tween;

        void Awake()
        {
            if (doorPanel == null)
            {
                Debug.LogError("AutoDoor 未指定门扇", this);
                enabled = false;
                return;
            }

            _closedLocalPosition = doorPanel.localPosition;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            Open();
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player") || !autoClose)
            {
                return;
            }

            _tween?.Kill();
            _tween = doorPanel.DOLocalMoveY(_closedLocalPosition.y, duration)
                .SetDelay(closeDelay)
                .SetEase(Ease.InQuad);
        }

        /// <summary>升起门。</summary>
        public void Open()
        {
            _tween?.Kill();
            _tween = doorPanel.DOLocalMoveY(_closedLocalPosition.y + openHeight, duration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>把门放回关闭位置。</summary>
        public void Close()
        {
            _tween?.Kill();
            _tween = doorPanel.DOLocalMoveY(_closedLocalPosition.y, duration)
                .SetEase(Ease.InQuad);
        }
    }
}
