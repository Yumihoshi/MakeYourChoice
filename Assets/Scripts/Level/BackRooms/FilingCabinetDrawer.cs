using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 文件柜 A 的一格抽屉：对准抽屉正面按交互键切换开/关，沿柜体正面方向滑出固定行程。
    /// 拉开第三层时通知 <see cref="GlowCabinetInteract"/> 播放触发 21 / 22。
    /// </summary>
    public sealed class FilingCabinetDrawer : InteractableBase
    {
        [SerializeField, Required, LabelText("柜子剧情控制器")]
        GlowCabinetInteract cabinet;

        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, LabelText("是不是第三层")]
        bool thirdLayer;

        [SerializeField, LabelText("允许交互")]
        bool interactable = true;

        [SerializeField, LabelText("拉出行程（米）")]
        float openDistance = 0.4f;

        [SerializeField, LabelText("开合时长（秒）")]
        float duration = 0.3f;

        Vector3 _closedLocalPosition;
        bool _open;

        /// <summary>目标状态是否为打开；开始打开时为 true，开始关闭时为 false。</summary>
        public bool IsOpen => _open;

        /// <summary>抽屉是否已经完全停在关闭位；打开后、补间进行中以及推回途中都为 false。</summary>
        public bool IsClosedAtRest
        {
            get
            {
                return !_open
                       && (transform.localPosition - _closedLocalPosition).sqrMagnitude < 1e-6f;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _closedLocalPosition = transform.localPosition;
        }

        /// <summary>进过打印室、柜子剧情没在播时允许开合。</summary>
        protected override bool CanInteract()
        {
            if (!interactable || cabinet == null || state == null)
            {
                return false;
            }

            return state.EnteredPrintRoom && cabinet.CanUseDrawers;
        }

        protected override void OnInteract()
        {
            _open = !_open;
            if (_open)
            {
                cabinet.OnDrawerOpened(thirdLayer);
            }

            transform.DOKill();
            transform.DOLocalMove(
                _open ? _closedLocalPosition + Vector3.forward * openDistance : _closedLocalPosition,
                duration).SetEase(Ease.OutCubic);
        }
    }
}
