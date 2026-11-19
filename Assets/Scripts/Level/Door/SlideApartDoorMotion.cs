using DG.Tweening;
using UnityEngine;

namespace Game.Level
{
    /// <summary>两扇门向两侧滑开的开合方式（关卡二的电梯门）。</summary>
    public sealed class SlideApartDoorMotion : IDoorMotion
    {
        readonly AutoDoor _owner;
        readonly Transform _leftLeaf;
        readonly Transform _rightLeaf;
        readonly float _slideDistance;
        readonly float _duration;
        readonly bool _autoClose;
        readonly float _closeDelay;

        Vector3 _closedLeftPosition;
        Vector3 _closedRightPosition;
        Tween _leftTween;
        Tween _rightTween;

        public SlideApartDoorMotion(AutoDoor owner, Transform leftLeaf, Transform rightLeaf,
            float slideDistance, float duration, bool autoClose, float closeDelay)
        {
            _owner = owner;
            _leftLeaf = leftLeaf;
            _rightLeaf = rightLeaf;
            _slideDistance = slideDistance;
            _duration = duration;
            _autoClose = autoClose;
            _closeDelay = closeDelay;
        }

        public bool TryConfigure()
        {
            if (_leftLeaf == null || _rightLeaf == null)
            {
                Debug.LogError("AutoDoor 未指定左右门扇", _owner);
                return false;
            }

            _closedLeftPosition = _leftLeaf.localPosition;
            _closedRightPosition = _rightLeaf.localPosition;
            return true;
        }

        public void Open()
        {
            KillTweens();
            _leftTween = _leftLeaf.DOLocalMoveX(_closedLeftPosition.x - _slideDistance, _duration)
                .SetEase(Ease.OutQuad);
            _rightTween = _rightLeaf.DOLocalMoveX(_closedRightPosition.x + _slideDistance, _duration)
                .SetEase(Ease.OutQuad);
        }

        public void Close()
        {
            KillTweens();
            float delay = _autoClose ? _closeDelay : 0f;
            _leftTween = _leftLeaf.DOLocalMoveX(_closedLeftPosition.x, _duration)
                .SetDelay(delay)
                .SetEase(Ease.InQuad);
            _rightTween = _rightLeaf.DOLocalMoveX(_closedRightPosition.x, _duration)
                .SetDelay(delay)
                .SetEase(Ease.InQuad);
        }

        public void FixedTick()
        {
        }

        public float Progress
        {
            get
            {
                if (_leftLeaf == null || _rightLeaf == null || _slideDistance <= 0f)
                {
                    return 0f;
                }

                float left = (_closedLeftPosition.x - _leftLeaf.localPosition.x) / _slideDistance;
                float right = (_rightLeaf.localPosition.x - _closedRightPosition.x) / _slideDistance;
                return Mathf.Clamp01((left + right) * 0.5f);
            }
        }

        public void Dispose() => KillTweens();

        void KillTweens()
        {
            _leftTween?.Kill();
            _rightTween?.Kill();
        }
    }
}
