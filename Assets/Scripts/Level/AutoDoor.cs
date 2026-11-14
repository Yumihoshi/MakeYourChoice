using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>门的打开方式。</summary>
    public enum AutoDoorMode
    {
        [LabelText("整体向上")] Rise,
        [LabelText("两扇向两侧滑开")] SlideApart,
    }

    /// <summary>
    /// 玩家靠近时打开的门；触发区挂在根节点，门扇作为子物体单独移动。
    /// </summary>
    public sealed class AutoDoor : MonoBehaviour
    {
        [LabelText("开门方式"), SerializeField] AutoDoorMode mode = AutoDoorMode.Rise;

        [LabelText("门扇（整体向上时使用）"), SerializeField] Transform doorPanel;
        [LabelText("升起高度"), SerializeField] float openHeight = 2.5f;
        [LabelText("单侧滑动距离（两扇向两侧时使用）"), SerializeField] float slideDistance = 1.5f;

        [LabelText("左门扇"), SerializeField] Transform leftLeaf;
        [LabelText("右门扇"), SerializeField] Transform rightLeaf;

        [LabelText("开关门时长"), SerializeField] float duration = 0.6f;
        [LabelText("自动关门"), SerializeField] bool autoClose;
        [LabelText("关门延迟"), SerializeField] float closeDelay = 2f;

        Vector3 _closedPanelPosition;
        Vector3 _closedLeftPosition;
        Vector3 _closedRightPosition;
        Tween _tween;
        Tween _secondTween;

        void Awake()
        {
            if (mode == AutoDoorMode.Rise)
            {
                if (doorPanel == null)
                {
                    Debug.LogError("AutoDoor 未指定门扇", this);
                    enabled = false;
                    return;
                }

                _closedPanelPosition = doorPanel.localPosition;
                return;
            }

            if (leftLeaf == null || rightLeaf == null)
            {
                Debug.LogError("AutoDoor 未指定左右门扇", this);
                enabled = false;
                return;
            }

            _closedLeftPosition = leftLeaf.localPosition;
            _closedRightPosition = rightLeaf.localPosition;
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

            Close();
        }

        /// <summary>打开门。</summary>
        public void Open()
        {
            KillTweens();

            if (mode == AutoDoorMode.Rise)
            {
                _tween = doorPanel.DOLocalMoveY(_closedPanelPosition.y + openHeight, duration)
                    .SetEase(Ease.OutQuad);
                return;
            }

            _tween = leftLeaf.DOLocalMoveX(_closedLeftPosition.x - slideDistance, duration)
                .SetEase(Ease.OutQuad);
            _secondTween = rightLeaf.DOLocalMoveX(_closedRightPosition.x + slideDistance, duration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>把门放回关闭位置。</summary>
        public void Close()
        {
            KillTweens();

            if (mode == AutoDoorMode.Rise)
            {
                _tween = doorPanel.DOLocalMoveY(_closedPanelPosition.y, duration)
                    .SetDelay(autoClose ? closeDelay : 0f)
                    .SetEase(Ease.InQuad);
                return;
            }

            var delay = autoClose ? closeDelay : 0f;
            _tween = leftLeaf.DOLocalMoveX(_closedLeftPosition.x, duration)
                .SetDelay(delay)
                .SetEase(Ease.InQuad);
            _secondTween = rightLeaf.DOLocalMoveX(_closedRightPosition.x, duration)
                .SetDelay(delay)
                .SetEase(Ease.InQuad);
        }

        void KillTweens()
        {
            _tween?.Kill();
            _secondTween?.Kill();
        }
    }
}
