using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 绕竖直铰链旋转的开合方式（关卡二两扇北门、安全门）：门扇挂在一个带 Rigidbody + HingeJoint 的枢轴上，
    /// 枢轴刚体设为运动学，每帧按固定角速度把门扇转到开门角 / 关门角。
    /// 不用 HingeJoint 的电机：铰链电机的力推不动玩家的 CharacterController，门会被玩家顶住停转；
    /// 运动学刚体不受玩家推挤，转动期间再把玩家层排除在门扇碰撞之外，玩家就不会被门板卡住。
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
        Quaternion _restRotation;
        Vector3 _axis;
        float _angle;

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

            _closedAngle = 0f;
            _openAngle = _openAngleOffset;
            _angle = _closedAngle;
            _restRotation = _body.rotation;
            _axis = _restRotation * _hinge.axis;

            // 改用运动学刚体直接转门扇：电机驱动在玩家挡门、门扇碰撞体被临时放行时都会失效。
            _body.isKinematic = true;
            _hinge.useMotor = false;
            _hinge.useLimits = false;

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

            // 按剩余角度限速：起步快、接近目标时逐渐减速，停在开门角 / 关门角上。
            float diff = _targetAngle - _angle;
            float speed = Mathf.Abs(diff) < SettleEpsilon
                ? 0f
                : Mathf.Clamp(Mathf.Abs(diff) * _gain, 0f, _speed);
            _angle = Mathf.MoveTowards(_angle, _targetAngle, speed * Time.fixedDeltaTime);
            _body.MoveRotation(Quaternion.AngleAxis(_angle, _axis) * _restRotation);

            // 门扇到位、不再压着玩家时才恢复碰撞：刚转到位就把站在门边的玩家卡住会很别扭，
            // 玩家重叠期间门板保持可穿过，等他走开再恢复。
            if (!_panelSolid && Mathf.Abs(_targetAngle - _angle) < SettleEpsilon * 2f && !PlayerOverlapsPanel())
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

                return Mathf.Clamp01(Mathf.InverseLerp(_closedAngle, _openAngle, _angle));
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

        /// <summary>
        /// 门扇转动期间把玩家层排除在碰撞之外：玩家用 CharacterController 挡在门板前时，电机的力推不动玩家，门会停转。
        /// 不能直接关掉门板碰撞体——刚体的形状会全部消失，PhysX 随后不再模拟这个刚体，铰链电机推不动它，门就卡在关着的位置。
        /// </summary>
        void SetPanelSolid(bool solid)
        {
            if (_panelSolid == solid)
            {
                return;
            }

            _panelSolid = solid;
            int excludedLayers = solid ? 0 : PlayerLayerMask();
            foreach (var collider in _panelColliders)
            {
                if (collider != null)
                {
                    collider.excludeLayers = excludedLayers;
                }
            }
        }

        static int PlayerLayerMask()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer < 0)
            {
                Debug.LogWarning("找不到 Player 层，门扇转动时无法放行玩家");
                return 0;
            }

            return 1 << playerLayer;
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
