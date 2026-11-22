using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 文件柜第三个抽屉里的真门禁卡：抽屉拉开后可以交互，
    /// 满足触发 13/14 后拿走并让卡片消失；条件不足时交互只会重复“只是反光”。
    /// 卡片未露出时不响应交互；卡片不作为抽屉子物体，避免抽屉的瞄准判定把卡片也当成自己。
    /// 用临时卡试过安全门之后卡片才会出现；如果试卡时抽屉还开着，
    /// 要先把它完全推回、再重新拉开，卡片才从底板下抬到抽屉内露出。
    /// </summary>
    public sealed class FilingCabinetCard : InteractableBase
    {
        [SerializeField, Required, LabelText("所属抽屉")]
        FilingCabinetDrawer drawer;

        [SerializeField, Required, LabelText("跟随的抽屉节点")]
        Transform drawerMount;

        [SerializeField, LabelText("抽屉内本地位置（试安全门前藏起）")]
        Vector3 hiddenLocalPositionInDrawer = new Vector3(0f, -0.155f, 0.3f);

        [SerializeField, LabelText("抽屉内本地位置（露出时）")]
        Vector3 revealedLocalPositionInDrawer = new Vector3(0f, 0f, 0.3f);

        [SerializeField, LabelText("抽屉内本地旋转")]
        Vector3 localEulerAnglesInDrawer = new Vector3(90f, 180f, 0f);

        [SerializeField, Required, LabelText("柜子剧情控制器")]
        GlowCabinetInteract cabinet;

        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        /// <summary>试过安全门后抽屉是否完全关过一次；关过一次后真卡才允许露出。</summary>
        bool _drawerClosedSinceTempCardTried;

        /// <summary>试过安全门之后卡片是否已经露出：抽屉还必须完全关过一次。</summary>
        bool IsRevealed
        {
            get { return state != null && state.TriedSafeDoorWithTempCard && _drawerClosedSinceTempCardTried; }
        }

        void LateUpdate()
        {
            if (drawerMount == null)
            {
                return;
            }

            if (state != null && drawer != null && !_drawerClosedSinceTempCardTried
                && state.TriedSafeDoorWithTempCard && drawer.IsClosedAtRest)
            {
                _drawerClosedSinceTempCardTried = true;
            }

            Vector3 localPosition = IsRevealed
                ? revealedLocalPositionInDrawer
                : hiddenLocalPositionInDrawer;

            transform.SetPositionAndRotation(
                drawerMount.TransformPoint(localPosition),
                drawerMount.rotation * Quaternion.Euler(localEulerAnglesInDrawer));
        }

        /// <summary>卡片已露出、抽屉拉开、卡没被锁住、还没拿走、柜子剧情没在播时才允许交互。</summary>
        protected override bool CanInteract()
        {
            if (drawer == null || cabinet == null || state == null)
            {
                return false;
            }

            return state.EnteredPrintRoom
                   && IsRevealed
                   && drawer.IsOpen
                   && !state.CabinetsLocked
                   && !state.GotRealCard
                   && !cabinet.IsDialogueRunning;
        }

        protected override void OnInteract()
        {
            if (cabinet.TryTakeRealCard())
            {
                gameObject.SetActive(false);
            }
        }
    }
}
