using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 绕竖直铰链旋转的开合方式（关卡二两扇北门、安全门）：门扇挂在一个带 Rigidbody + HingeJoint 的枢轴上，
    /// 电机按剩余角度限速转到开门角 / 关门角。
    /// 电机推不动玩家的 CharacterController，玩家站在门前时门扇会顶在玩家身上转不动，所以转动期间临时关掉门扇的碰撞体，
    /// 到位后再恢复；否则门永远开不到目标角度，依赖“门已完全打开”的后续流程（传送门）也不会触发。
    /// </summary>
    public sealed class HingeDoorMotion : IDoorMotion
    {
        const float SettleEpsilon = 0.5f;

        readonly AutoDoor _owner;
        readonly HingeJoint _hinge;
        readonly float _openAngleOffset;
        readonly float _speed;
        readonly float _gain;
        readonly float _motorForce;
        readonly bool _autoClose;
        readonly float _closeDelay;
        readonly Collider[] _panelColliders;
        readonly Renderer[] _panelRenderers;

        Rigidbody _body;
        CharacterController _player;
        bool _playerLookupDone;
        float _closedAngle;
        float _openAngle;
        float _targetAngle;
        float _closeAt;
        bool _pendingClose;
        bool _panelSolid = true;

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
            _panelColliders = hinge != null
                ? hinge.GetComponentsInChildren<Collider>(true)
                : new Collider[0];
            _panelRenderers = hinge != null
                ? hinge.GetComponentsInChildren<Renderer>(true)
                : new Renderer[0];
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
            BeginMove();
        }

        public void Close()
        {
            _pendingClose = true;
            _closeAt = Time.time + (_autoClose ? _closeDelay : 0f);
            _targetAngle = _closedAngle;
            BeginMove();
        }

        public void FixedTick()
        {
            if (_pendingClose && Time.time >= _closeAt)
            {
                _pendingClose = false;
                _targetAngle = _closedAngle;
                BeginMove();
            }

            // 按剩余角度给电机限速：起步快、接近目标时逐渐减速，停在开门角 / 关门角上。
            float diff = _targetAngle - _hinge.angle;
            var motor = _hinge.motor;
            motor.targetVelocity = Mathf.Abs(diff) < SettleEpsilon
                ? 0f
                : Mathf.Clamp(diff * _gain, -_speed, _speed);
            _hinge.motor = motor;

            // 门扇到位、不再压着玩家时才恢复碰撞：刚转到位就把站在门边的玩家卡住会很别扭，
            // 玩家重叠期间门板保持可穿过，等他走开再恢复。电机在剩余角度小于死区时停转，
            // 实际停在离目标不到 1° 的位置，这里用更宽的阈值判定“到位”。
            if (!_panelSolid && Mathf.Abs(diff) < SettleEpsilon * 2f && !PlayerOverlapsPanel())
            {
                SetPanelSolid(true);
            }
        }

        public float Progress
        {
            get
            {
                if (_hinge == null || Mathf.Approximately(_openAngle, _closedAngle))
                {
                    return 0f;
                }

                return Mathf.Clamp01(Mathf.InverseLerp(_closedAngle, _openAngle, _hinge.angle));
            }
        }

        public void Dispose()
        {
            SetPanelSolid(true);
        }

        void BeginMove()
        {
            SetPanelSolid(false);
            _body.WakeUp();
        }

        /// <summary>门扇转动期间关掉碰撞体：玩家用 CharacterController 挡在门板前时，电机的力推不动玩家，门会停转。</summary>
        void SetPanelSolid(bool solid)
        {
            if (_panelSolid == solid)
            {
                return;
            }

            _panelSolid = solid;
            foreach (var collider in _panelColliders)
            {
                if (collider != null)
                {
                    collider.enabled = solid;
                }
            }
        }

        /// <summary>门扇是否还压着玩家：放行期间碰撞体是关掉的（包围盒会退化成 0），所以用渲染器包围盒判断。</summary>
        bool PlayerOverlapsPanel()
        {
            var player = FindPlayer();
            if (player == null || !player.enabled)
            {
                return false;
            }

            var playerBounds = player.bounds;
            foreach (var panelRenderer in _panelRenderers)
            {
                if (panelRenderer != null && panelRenderer.bounds.Intersects(playerBounds))
                {
                    return true;
                }
            }

            foreach (var panelCollider in _panelColliders)
            {
                if (panelCollider != null && panelCollider.enabled && panelCollider.bounds.Intersects(playerBounds))
                {
                    return true;
                }
            }

            return false;
        }

        CharacterController FindPlayer()
        {
            if (_playerLookupDone)
            {
                return _player;
            }

            _playerLookupDone = true;
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                _player = playerObject.GetComponentInChildren<CharacterController>();
            }

            return _player;
        }
    }
}
