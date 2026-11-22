using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 文件柜的一格抽屉：对准抽屉正面按交互键切换开/关，只滑动抽屉本身、不播台词。
    /// 拉开时向抽屉剧情报数：储藏室用 <see cref="quest"/>（维护记录 / 关门这一分钟有没有开柜子），
    /// 打印室用 <see cref="printQuest"/>（触发 20）。
    /// </summary>
    public sealed class EmptyCabinetDrawer : InteractableBase
    {
        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        [SerializeField, LabelText("储藏室抽屉剧情计数")]
        StorageDrawerQuest quest;

        [SerializeField, LabelText("打印室抽屉剧情")]
        PrintRoomCabinetQuest printQuest;

        [SerializeField, LabelText("允许交互")]
        bool interactable = true;

        [SerializeField, LabelText("拉出行程（米）")]
        float openDistance = 0.35f;

        [SerializeField, LabelText("开合时长（秒）")]
        float duration = 0.25f;

        Vector3 _closedLocalPosition;
        bool _open;
        bool _counted;

        protected override void Awake()
        {
            base.Awake();
            _closedLocalPosition = transform.localPosition;
        }

        protected override bool CanInteract()
        {
            return interactable;
        }

        protected override void OnInteract()
        {
            _open = !_open;
            if (_open)
            {
                if (!_counted && quest != null)
                {
                    _counted = true;
                    quest.RegisterOpened();
                }

                if (printQuest != null)
                {
                    printQuest.RegisterOpened();
                }

                if (state != null)
                {
                    state.MarkCabinetOpenedDuringLockdown();
                }
            }

            transform.DOKill();
            transform.DOLocalMove(
                _open ? _closedLocalPosition + Vector3.forward * openDistance : _closedLocalPosition,
                duration).SetEase(Ease.OutCubic);
        }
    }
}
