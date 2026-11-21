using System;
using Cysharp.Threading.Tasks;
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
        [SerializeField, Required, LabelText("铰链节点")]
        Transform hinge;

        [SerializeField, LabelText("开门角度"), Tooltip("绕铰链本地 Y 轴旋转的角度，正负决定开向")]
        float openAngle = 90f;

        [SerializeField, LabelText("开合时长")]
        float duration = 0.45f;

        Collider _panelCollider;
        Quaternion _closedRotation;
        bool _open;
        bool _moving;

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
            _panelCollider = GetComponent<Collider>();
        }

        protected override bool CanInteract()
        {
            return hinge != null && !_moving;
        }

        protected override void OnInteract()
        {
            ToggleAsync().Forget();
        }

        async UniTaskVoid ToggleAsync()
        {
            _moving = true;
            var target = _open ? _closedRotation : _closedRotation * Quaternion.Euler(0f, openAngle, 0f);
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

                _open = !_open;
                _moving = false;
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
