using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// 第一人称移动：CharacterController + 新版 Input System 的动作输入，水平转身由自身承担，
    /// 上下俯仰作用在 <see cref="lookPivot"/> 上，避免和电梯晃动等相机位移互相覆盖。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [LabelText("移动速度"), SerializeField] float moveSpeed = 4f;
        [LabelText("奔跑倍率"), SerializeField] float sprintMultiplier = 1.8f;
        [LabelText("鼠标灵敏度"), SerializeField] float lookSensitivity = 0.12f;
        [LabelText("重力"), SerializeField] float gravity = -20f;
        [LabelText("俯仰节点"), Tooltip("上下看的旋转作用在这个节点上，通常填子物体「眼睛」")]
        [SerializeField] Transform lookPivot;
        [LabelText("俯仰角度上限"), Range(0f, 89f), SerializeField] float pitchLimit = 85f;

        CharacterController _controller;
        float _yaw;
        float _pitch;
        float _verticalVelocity;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _yaw = transform.eulerAngles.y;

            if (lookPivot == null)
            {
                Debug.LogError("FirstPersonController 缺少俯仰节点引用", this);
                enabled = false;
                return;
            }

            _pitch = Mathf.DeltaAngle(0f, lookPivot.localEulerAngles.x);
        }

        void Update()
        {
            var input = PlayerInputRouter.Instance;
            if (input == null || !GameCursorState.IsPlaying)
            {
                return;
            }

            HandleLook(input);
            HandleMove(input);
        }

        void HandleLook(PlayerInputRouter input)
        {
            // 鼠标是位移量、手柄摇杆是推量，两者换算方式不同。
            var degreesPerFrame = input.CurrentDevice == InputDeviceKind.Gamepad
                ? input.ReadGamepadLookDegreesPerSecond() * Time.deltaTime
                : input.Look.ReadValue<Vector2>() * lookSensitivity;

            _yaw += degreesPerFrame.x;
            _pitch = Mathf.Clamp(_pitch - degreesPerFrame.y, -pitchLimit, pitchLimit);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            lookPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        void HandleMove(PlayerInputRouter input)
        {
            var axis = Vector2.ClampMagnitude(input.Move.ReadValue<Vector2>(), 1f);

            var speed = moveSpeed;
            if (input.Sprint.IsPressed())
            {
                speed *= sprintMultiplier;
            }

            var move = (transform.right * axis.x + transform.forward * axis.y) * speed;
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += gravity * Time.deltaTime;
            move.y = _verticalVelocity;
            _controller.Move(move * Time.deltaTime);
        }
    }
}
