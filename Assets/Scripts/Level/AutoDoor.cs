using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>门的打开方式。</summary>
    public enum AutoDoorMode
    {
        [LabelText("整体向上")] Rise,
        [LabelText("两扇向两侧滑开")] SlideApart,
        [LabelText("绕铰链旋转")] Hinge,
    }

    /// <summary>
    /// 玩家靠近时打开的门；触发区挂在根节点，门扇作为子物体单独移动或绕铰链旋转。
    /// 旋转门由 <see cref="HingeJoint"/> 限制门扇只能绕竖直轴转动，电机负责开关。
    /// </summary>
    public sealed class AutoDoor : MonoBehaviour
    {
        [LabelText("开门方式"), SerializeField] AutoDoorMode mode = AutoDoorMode.Rise;

        [LabelText("门扇（整体向上时使用）"), SerializeField] Transform doorPanel;
        [LabelText("升起高度"), SerializeField] float openHeight = 2.5f;
        [LabelText("单侧滑动距离（两扇向两侧时使用）"), SerializeField] float slideDistance = 1.5f;

        [LabelText("左门扇"), SerializeField] Transform leftLeaf;
        [LabelText("右门扇"), SerializeField] Transform rightLeaf;

        [LabelText("铰链关节（旋转门使用）"), SerializeField] HingeJoint hinge;
        [LabelText("开门角度（旋转门使用，负值反向转）"), SerializeField] float hingeOpenAngle = -95f;
        [LabelText("最大角速度（旋转门使用，度/秒）"), SerializeField] float hingeSpeed = 150f;
        [LabelText("角度跟随刚度（旋转门使用）"), SerializeField] float hingeGain = 4f;
        [LabelText("电机力矩上限（旋转门使用）"), SerializeField] float hingeMotorForce = 600f;

        [LabelText("开关门时长"), SerializeField] float duration = 0.6f;
        [LabelText("自动关门"), SerializeField] bool autoClose;
        [LabelText("关门延迟"), SerializeField] float closeDelay = 2f;

        Vector3 _closedPanelPosition;
        Vector3 _closedLeftPosition;
        Vector3 _closedRightPosition;
        Tween _tween;
        Tween _secondTween;

        Rigidbody _hingeBody;
        float _closedAngle;
        float _openAngle;
        float _targetAngle;
        float _closeAt;
        bool _pendingClose;

        /// <summary>锁住时玩家靠近也不开门、调用 <see cref="Open"/> 也无效；由剧情脚本控制。</summary>
        public bool Locked { get; set; }

        /// <summary>门当前是否已经打开（升到顶 / 滑到两侧 / 转到开门角度）；用于交互提示与防重复开门。</summary>
        public bool IsOpen { get; private set; }

        void Awake()
        {
            if (mode == AutoDoorMode.Hinge)
            {
                if (hinge == null)
                {
                    Debug.LogError("AutoDoor 未指定铰链关节", this);
                    enabled = false;
                    return;
                }

                _hingeBody = hinge.GetComponent<Rigidbody>();
                if (_hingeBody == null)
                {
                    Debug.LogError("AutoDoor 的铰链关节缺少刚体", this);
                    enabled = false;
                    return;
                }

                _closedAngle = hinge.angle;
                _openAngle = _closedAngle + hingeOpenAngle;

                var limits = hinge.limits;
                limits.min = Mathf.Min(_closedAngle, _openAngle);
                limits.max = Mathf.Max(_closedAngle, _openAngle);
                hinge.limits = limits;
                hinge.useLimits = true;
                hinge.useMotor = true;

                var motor = hinge.motor;
                motor.force = hingeMotorForce;
                motor.targetVelocity = 0f;
                motor.freeSpin = false;
                hinge.motor = motor;

                _targetAngle = _closedAngle;
                return;
            }

            if (mode == AutoDoorMode.Rise)
            {
                if (doorPanel == null)
                {
                    Debug.LogError("AutoDoor 未指定门扇", this);
                    enabled = false;
                    return;
                }

                _closedPanelPosition = doorPanel.localPosition;
                return;
            }

            if (leftLeaf == null || rightLeaf == null)
            {
                Debug.LogError("AutoDoor 未指定左右门扇", this);
                enabled = false;
                return;
            }

            _closedLeftPosition = leftLeaf.localPosition;
            _closedRightPosition = rightLeaf.localPosition;
        }

        void FixedUpdate()
        {
            if (mode != AutoDoorMode.Hinge)
            {
                return;
            }

            if (_pendingClose && Time.time >= _closeAt)
            {
                _pendingClose = false;
                _targetAngle = _closedAngle;
            }

            // 按剩余角度给电机限速：起步快、接近目标时逐渐减速，停在开门角 / 关门角上。
            float diff = _targetAngle - hinge.angle;
            var motor = hinge.motor;
            motor.targetVelocity = Mathf.Abs(diff) < 0.5f
                ? 0f
                : Mathf.Clamp(diff * hingeGain, -hingeSpeed, hingeSpeed);
            hinge.motor = motor;
        }

        void OnTriggerEnter(Collider other)
        {
            if (Locked || !other.CompareTag("Player"))
            {
                return;
            }

            Open();
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player") || !autoClose)
            {
                return;
            }

            Close();
        }

        /// <summary>打开门；锁住时不响应。</summary>
        public void Open()
        {
            if (Locked)
            {
                return;
            }

            IsOpen = true;
            KillTweens();

            if (mode == AutoDoorMode.Hinge)
            {
                _pendingClose = false;
                _targetAngle = _openAngle;
                _hingeBody.WakeUp();
                return;
            }

            if (mode == AutoDoorMode.Rise)
            {
                _tween = doorPanel.DOLocalMoveY(_closedPanelPosition.y + openHeight, duration)
                    .SetEase(Ease.OutQuad);
                return;
            }

            _tween = leftLeaf.DOLocalMoveX(_closedLeftPosition.x - slideDistance, duration)
                .SetEase(Ease.OutQuad);
            _secondTween = rightLeaf.DOLocalMoveX(_closedRightPosition.x + slideDistance, duration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>把门放回关闭位置。</summary>
        public void Close()
        {
            IsOpen = false;
            KillTweens();

            if (mode == AutoDoorMode.Hinge)
            {
                _pendingClose = true;
                _closeAt = Time.time + (autoClose ? closeDelay : 0f);
                _hingeBody.WakeUp();
                return;
            }

            if (mode == AutoDoorMode.Rise)
            {
                _tween = doorPanel.DOLocalMoveY(_closedPanelPosition.y, duration)
                    .SetDelay(autoClose ? closeDelay : 0f)
                    .SetEase(Ease.InQuad);
                return;
            }

            var delay = autoClose ? closeDelay : 0f;
            _tween = leftLeaf.DOLocalMoveX(_closedLeftPosition.x, duration)
                .SetDelay(delay)
                .SetEase(Ease.InQuad);
            _secondTween = rightLeaf.DOLocalMoveX(_closedRightPosition.x, duration)
                .SetDelay(delay)
                .SetEase(Ease.InQuad);
        }

        void KillTweens()
        {
            _tween?.Kill();
            _secondTween?.Kill();
        }
    }
}
