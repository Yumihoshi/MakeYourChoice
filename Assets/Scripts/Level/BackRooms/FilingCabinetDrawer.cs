using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 文件柜的一格抽屉：对准抽屉正面按交互键切换开/关，沿柜体正面方向滑出固定行程。
    /// 第一次拉开任意一格时通知 <see cref="GlowCabinetInteract"/> 播放触发 14 的剧情。
    /// </summary>
    public sealed class FilingCabinetDrawer : InteractableBase
    {
        [SerializeField, Required, LabelText("柜子剧情控制器")]
        GlowCabinetInteract cabinet;

        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, LabelText("拉出行程（米）")]
        float openDistance = 0.4f;

        [SerializeField, LabelText("开合时长（秒）")]
        float duration = 0.3f;

        Vector3 _closedLocalPosition;
        bool _open;

        /// <summary>目标状态是否为打开；开始打开时为 true，开始关闭时为 false。</summary>
        public bool IsOpen => _open;

        void Awake()
        {
            _closedLocalPosition = transform.localPosition;
        }

        /// <summary>进过打印室、柜子剧情没在播；锁柜期间只能把已经打开的抽屉关回去。</summary>
        protected override bool CanInteract()
        {
            if (cabinet == null || state == null)
            {
                return false;
            }

            if (!state.EnteredPrintRoom || !cabinet.CanUseDrawers)
            {
                return false;
            }

            return _open || !state.CabinetsLocked;
        }

        protected override void OnInteract()
        {
            if (!_open && state.CabinetsLocked)
            {
                return;
            }

            _open = !_open;
            if (_open)
            {
                cabinet.OnDrawerOpened();
            }

            transform.DOKill();
            transform.DOLocalMove(TargetLocalPosition(), duration).SetEase(Ease.OutCubic);
        }

        Vector3 TargetLocalPosition()
        {
            return _open
                ? _closedLocalPosition + Vector3.forward * openDistance
                : _closedLocalPosition;
        }
    }
}
