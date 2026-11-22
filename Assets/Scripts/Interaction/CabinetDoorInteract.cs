using System;
using Cysharp.Threading.Tasks;
using Game.Dialogue;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 柜门的交互：玩家对准门板按交互键，门绕自身铰链在关闭与打开角度之间缓慢旋转。
    /// 只做视觉与阻挡，不参与物理推挤；转动期间把玩家层排除在门板碰撞之外，避免门把玩家顶开。
    /// </summary>
    public sealed class CabinetDoorInteract : InteractableBase
    {
        public enum LockMode
        {
            Unlocked,
            SilentLocked,
            FeedbackLocked
        }

        [SerializeField, Required, LabelText("铰链节点")]
        Transform hinge;

        [SerializeField, LabelText("开门角度"), Tooltip("绕铰链本地 Y 轴旋转的角度，正负决定开向")]
        float openAngle = 90f;

        [SerializeField, LabelText("开合时长")]
        float duration = 0.45f;

        [SerializeField, LabelText("初始锁状态")]
        LockMode lockMode = LockMode.Unlocked;

        [SerializeField, LabelText("泳池房流程状态"), Tooltip("留空时保持原有锁逻辑；绑定后，玩家进入泳池房即可忽略锁并打开")]
        PoolFlowState poolFlowState;

        [SerializeField, ShowIf(nameof(lockMode), LockMode.FeedbackLocked), LabelText("上锁提示剧情")]
        DialogueTrigger lockedDialogue;

        [SerializeField, LabelText("开柜进度")]
        PoolCabinetQuest cabinetQuest;

        [SerializeField, LabelText("首次打开时剧情")]
        DialogueTrigger firstOpenDialogue;

        [SerializeField, LabelText("计入开柜次数")]
        bool countsForQuest = true;

        Collider _panelCollider;
        Quaternion _closedRotation;
        bool _open;
        bool _moving;
        bool _locked;
        bool _reportedOpen;

        /// <summary>锁着时是否仍然显示准心并给短句反馈。</summary>
        bool IsFeedbackLocked => _locked && lockMode == LockMode.FeedbackLocked;

        protected override void Awake()
        {
            base.Awake();
            if (!enabled)
            {
                return;
            }

            if (hinge == null)
            {
                Debug.LogError("CabinetDoorInteract 缺少铰链节点引用", this);
                enabled = false;
                return;
            }

            _closedRotation = hinge.localRotation;
            _panelCollider = GetComponentInChildren<Collider>();
            _locked = lockMode != LockMode.Unlocked;

            if (IsFeedbackLocked && lockedDialogue == null)
            {
                Debug.LogError("CabinetDoorInteract 配置为 FeedbackLocked，但没有上锁提示剧情", this);
            }
        }

        protected override bool CanInteract()
        {
            if (hinge == null || _moving)
            {
                return false;
            }

            if (poolFlowState != null)
            {
                return poolFlowState.EnteredPoolRoom || IsFeedbackLocked;
            }

            return !(_locked && lockMode == LockMode.SilentLocked);
        }

        protected override void OnInteract()
        {
            if (poolFlowState == null || !poolFlowState.EnteredPoolRoom)
            {
                if (_locked)
                {
                    if (IsFeedbackLocked && lockedDialogue != null)
                    {
                        lockedDialogue.StartDialogue();
                    }

                    return;
                }
            }

            SetOpenAsync(!_open).Forget();
        }

        /// <summary>解锁；之后由玩家自己开门，或由剧情调用 <see cref="Open"/>。</summary>
        public void Unlock() => _locked = false;

        /// <summary>解锁后由剧情直接打开；已开或正在转时忽略。</summary>
        public void Open()
        {
            if (_locked || _open || _moving || hinge == null)
            {
                return;
            }

            SetOpenAsync(true).Forget();
        }

        async UniTaskVoid SetOpenAsync(bool open)
        {
            _moving = true;
            var target = open ? _closedRotation * Quaternion.Euler(0f, openAngle, 0f) : _closedRotation;
            var from = hinge.localRotation;
            var originalExclude = _panelCollider != null ? _panelCollider.excludeLayers : default;

            if (_panelCollider != null)
            {
                _panelCollider.excludeLayers = PlayerLayerMask();
            }

            try
            {
                for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
                {
                    var t = Mathf.Clamp01(elapsed / duration);
                    hinge.localRotation = Quaternion.SlerpUnclamped(from, target, t * t * (3f - 2f * t));
                    await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken);
                }

                hinge.localRotation = target;
            }
            catch (OperationCanceledException)
            {
                // 物件在转动途中被销毁 / 禁用，直接停止即可。
            }
            finally
            {
                if (_panelCollider != null)
                {
                    _panelCollider.excludeLayers = originalExclude;
                }

                _open = open;
                _moving = false;

                if (open && !_reportedOpen)
                {
                    _reportedOpen = true;
                    if (firstOpenDialogue != null)
                    {
                        firstOpenDialogue.StartDialogue();
                    }

                    if (countsForQuest && cabinetQuest != null)
                    {
                        cabinetQuest.ReportOpened(this);
                    }
                }
            }
        }

        static LayerMask PlayerLayerMask()
        {
            var layer = LayerMask.NameToLayer("Player");
            if (layer < 0)
            {
                Debug.LogWarning("找不到 Player 层，柜门转动时无法放行玩家");
                return 0;
            }

            return 1 << layer;
        }
    }
}
