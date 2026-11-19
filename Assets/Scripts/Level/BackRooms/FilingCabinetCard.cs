using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 文件柜第三个抽屉里的真门禁卡：抽屉拉开后可以交互，
    /// 满足触发 13/14 后拿走并让卡片消失；条件不足时只会重复“只是反光”。
    /// 卡片不作为抽屉子物体，避免抽屉的瞄准判定把卡片也当成自己。
    /// 拿到临时门禁卡之前卡片藏在抽屉底板下不显示，拿到之后抬到抽屉内露出。
    /// </summary>
    public sealed class FilingCabinetCard : InteractableBase
    {
        [SerializeField, Required, LabelText("所属抽屉")]
        FilingCabinetDrawer drawer;

        [SerializeField, Required, LabelText("跟随的抽屉节点")]
        Transform drawerMount;

        [SerializeField, LabelText("抽屉内本地位置（拿到临时卡前）")]
        Vector3 hiddenLocalPositionInDrawer = new Vector3(0f, -0.155f, 0.3f);

        [SerializeField, LabelText("抽屉内本地位置（拿到临时卡后）")]
        Vector3 revealedLocalPositionInDrawer = new Vector3(0f, 0f, 0.3f);

        [SerializeField, LabelText("抽屉内本地旋转")]
        Vector3 localEulerAnglesInDrawer = new Vector3(90f, 180f, 0f);

        [SerializeField, Required, LabelText("柜子剧情控制器")]
        GlowCabinetInteract cabinet;

        [SerializeField, Required, LabelText("关卡状态")]
        BackRoomFlowState state;

        void LateUpdate()
        {
            if (drawerMount == null)
            {
                return;
            }

            Vector3 localPosition = state != null && state.GotTempCard
                ? revealedLocalPositionInDrawer
                : hiddenLocalPositionInDrawer;

            transform.SetPositionAndRotation(
                drawerMount.TransformPoint(localPosition),
                drawerMount.rotation * Quaternion.Euler(localEulerAnglesInDrawer));
        }

        /// <summary>抽屉拉开、卡没被锁住、还没拿走、柜子剧情没在播时才允许交互。</summary>
        protected override bool CanInteract()
        {
            if (drawer == null || cabinet == null || state == null)
            {
                return false;
            }

            return state.EnteredPrintRoom
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
