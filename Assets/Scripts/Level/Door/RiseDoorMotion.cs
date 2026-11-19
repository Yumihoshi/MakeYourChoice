using DG.Tweening;
using UnityEngine;

namespace Game.Level
{
    /// <summary>整扇门向上抬起的开合方式（电梯门、安全门都是这个）。</summary>
    public sealed class RiseDoorMotion : IDoorMotion
    {
        readonly AutoDoor _owner;
        readonly Transform _doorPanel;
        readonly float _openHeight;
        readonly float _duration;
        readonly bool _autoClose;
        readonly float _closeDelay;

        Vector3 _closedPosition;
        Tween _tween;

        public RiseDoorMotion(AutoDoor owner, Transform doorPanel, float openHeight,
            float duration, bool autoClose, float closeDelay)
        {
            _owner = owner;
            _doorPanel = doorPanel;
            _openHeight = openHeight;
            _duration = duration;
            _autoClose = autoClose;
            _closeDelay = closeDelay;
        }

        public bool TryConfigure()
        {
            if (_doorPanel == null)
            {
                Debug.LogError("AutoDoor 未指定门扇", _owner);
                return false;
            }

            _closedPosition = _doorPanel.localPosition;
            return true;
        }

        public void Open()
        {
            _tween?.Kill();
            _tween = _doorPanel.DOLocalMoveY(_closedPosition.y + _openHeight, _duration)
                .SetEase(Ease.OutQuad);
        }

        public void Close()
        {
            _tween?.Kill();
            _tween = _doorPanel.DOLocalMoveY(_closedPosition.y, _duration)
                .SetDelay(_autoClose ? _closeDelay : 0f)
                .SetEase(Ease.InQuad);
        }

        public void FixedTick()
        {
        }

        public float Progress
        {
            get
            {
                if (_doorPanel == null || _openHeight <= 0f)
                {
                    return 0f;
                }

                return Mathf.Clamp01(
                    (_doorPanel.localPosition.y - _closedPosition.y) / _openHeight);
            }
        }

        public void Dispose() => _tween?.Kill();
    }
}
