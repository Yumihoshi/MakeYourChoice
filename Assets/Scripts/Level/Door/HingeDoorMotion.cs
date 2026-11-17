using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 绕竖直铰链旋转的开合方式（关卡二两扇北门）：门扇挂在一个带 Rigidbody + HingeJoint 的枢轴上，
    /// 电机按剩余角度限速转到开门角 / 关门角。
    /// </summary>
    public sealed class HingeDoorMotion : IDoorMotion
    {
        readonly AutoDoor _owner;
        readonly HingeJoint _hinge;
        readonly float _openAngleOffset;
        readonly float _speed;
        readonly float _gain;
        readonly float _motorForce;
        readonly bool _autoClose;
        readonly float _closeDelay;

        Rigidbody _body;
        float _closedAngle;
        float _openAngle;
        float _targetAngle;
        float _closeAt;
        bool _pendingClose;

        public HingeDoorMotion(AutoDoor owner, HingeJoint hinge, float openAngleOffset,
            float speed, float gain, float motorForce, bool autoClose, float closeDelay)
        {
            _owner = owner;
            _hinge = hinge;
            _openAngleOffset = openAngleOffset;
            _speed = speed;
            _gain = gain;
            _motorForce = motorForce;
            _autoClose = autoClose;
            _closeDelay = closeDelay;
        }

        public bool TryConfigure()
        {
            if (_hinge == null)
            {
                Debug.LogError("AutoDoor 未指定铰链关节", _owner);
                return false;
            }

            _body = _hinge.GetComponent<Rigidbody>();
            if (_body == null)
            {
                Debug.LogError("AutoDoor 的铰链关节缺少刚体", _owner);
                return false;
            }

            _closedAngle = _hinge.angle;
            _openAngle = _closedAngle + _openAngleOffset;

            var limits = _hinge.limits;
            limits.min = Mathf.Min(_closedAngle, _openAngle);
            limits.max = Mathf.Max(_closedAngle, _openAngle);
            _hinge.limits = limits;
            _hinge.useLimits = true;
            _hinge.useMotor = true;

            var motor = _hinge.motor;
            motor.force = _motorForce;
            motor.targetVelocity = 0f;
            motor.freeSpin = false;
            _hinge.motor = motor;

            _targetAngle = _closedAngle;
            return true;
        }

        public void Open()
        {
            _pendingClose = false;
            _targetAngle = _openAngle;
            _body.WakeUp();
        }

        public void Close()
        {
            _pendingClose = true;
            _closeAt = Time.time + (_autoClose ? _closeDelay : 0f);
            _body.WakeUp();
        }

        public void FixedTick()
        {
            if (_pendingClose && Time.time >= _closeAt)
            {
                _pendingClose = false;
                _targetAngle = _closedAngle;
            }

            // 按剩余角度给电机限速：起步快、接近目标时逐渐减速，停在开门角 / 关门角上。
            float diff = _targetAngle - _hinge.angle;
            var motor = _hinge.motor;
            motor.targetVelocity = Mathf.Abs(diff) < 0.5f
                ? 0f
                : Mathf.Clamp(diff * _gain, -_speed, _speed);
            _hinge.motor = motor;
        }

        public void Dispose()
        {
        }
    }
}
