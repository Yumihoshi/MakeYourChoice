using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 储藏室文件柜的一格抽屉：对准抽屉正面按交互键切换开/关，只滑动抽屉本身、不播台词。
    /// 每拉开一格会向 <see cref="StorageDrawerQuest"/> 报数；拿到临时卡、柜子被锁住时只能把打开的抽屉关回去。
    /// </summary>
    public sealed class EmptyCabinetDrawer : InteractableBase
    {
        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, Required, LabelText("抽屉剧情计数")]
        StorageDrawerQuest quest;

        [SerializeField, LabelText("允许交互")]
        bool interactable = true;

        [SerializeField, LabelText("拉出行程（米）")]
        float openDistance = 0.35f;

        [SerializeField, LabelText("开合时长（秒）")]
        float duration = 0.25f;

        Vector3 _closedLocalPosition;
        bool _open;
        bool _counted;

        void Awake()
        {
            _closedLocalPosition = transform.localPosition;
        }

        /// <summary>柜子被锁住时只能把已经打开的抽屉关回去。</summary>
        protected override bool CanInteract()
        {
            return interactable && (_open || state == null || !state.CabinetsLocked);
        }

        protected override void OnInteract()
        {
            if (!_open && state != null && state.CabinetsLocked)
            {
                return;
            }

            _open = !_open;
            if (_open && !_counted && quest != null)
            {
                _counted = true;
                quest.RegisterOpened();
            }

            transform.DOKill();
            transform.DOLocalMove(
                _open ? _closedLocalPosition + Vector3.forward * openDistance : _closedLocalPosition,
                duration).SetEase(Ease.OutCubic);
        }
    }
}
