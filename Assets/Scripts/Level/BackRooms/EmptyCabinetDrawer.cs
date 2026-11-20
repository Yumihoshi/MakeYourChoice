using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 空文件柜的一格抽屉：对准抽屉正面按交互键切换开/关，只滑动抽屉本身。
    /// 不带台词、不改变任何流程状态，和 <see cref="FilingCabinetDrawer"/> 的区别就在这里。
    /// </summary>
    public sealed class EmptyCabinetDrawer : InteractableBase
    {
        [SerializeField, LabelText("拉出行程（米）")]
        float openDistance = 0.35f;

        [SerializeField, LabelText("开合时长（秒）")]
        float duration = 0.25f;

        Vector3 _closedLocalPosition;
        bool _open;

        void Awake()
        {
            _closedLocalPosition = transform.localPosition;
        }

        protected override bool CanInteract() => true;

        protected override void OnInteract()
        {
            _open = !_open;

            transform.DOKill();
            transform.DOLocalMove(
                _open ? _closedLocalPosition + Vector3.forward * openDistance : _closedLocalPosition,
                duration).SetEase(Ease.OutCubic);
        }
    }
}
